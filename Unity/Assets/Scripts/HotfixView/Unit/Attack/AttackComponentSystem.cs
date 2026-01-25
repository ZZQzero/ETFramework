using System;
using System.Collections.Generic;
using Animancer;
using Cysharp.Threading.Tasks;
using GameUI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ET
{
    [EntitySystemOf(typeof(AttackComponent))]
    [FriendOf(typeof(AttackComponent))]
    public static partial class AttackComponentSystem
    {
        #region 生命周期
        
        [EntitySystem]
        private static void Awake(this AttackComponent self)
        {
            self.ResetState();
            Unit unit = self.GetParent<Unit>();
            self.AnimatorComponent = unit.GetComponent<AnimatorComponent>();
            self.Player = unit.GetComponent<GameObjectComponent>().Transform;
            self.CameraFollow = self.Root().GetComponent<CameraFollowComponent>();
            self.TimerComponent = self.Root().GetComponent<TimerComponent>();
            self.RoleIdentity = unit.GetComponent<RoleIdentityComponent>();
            self.HitStop = unit.GetComponent<CombatFeedbackComponent>();
            self.EffectRoot = new GameObject("EffectRoot");
            if (self.CameraFollow == null)
            {
                Log.Error("没有找到CameraFollowComponent组件");
            }
            self.LoadConfigAsync().NoContext();
        }

        [EntitySystem]
        private static void Destroy(this AttackComponent self)
        {
            self.CleanupTimers();
            self.ResetState();
            self.Config = null;
            self.CurrentAnimState = null;
            self.OnAttackStart = null;
            self.OnAttackEnd = null;
            self.OnHit = null;
            self.OnComboReset = null;
            self.OnComboCountChanged = null;
        }

        [EntitySystem]
        private static void Update(this AttackComponent self)
        {
            if (!self.IsInAttack)
                return;

            self.UpdateBufferedInputTimeout();
            self.UpdateComboTimeout();
            self.UpdateAnimationState();
            self.UpdateAttackLayerFadeOut();
        }

        [EntitySystem]
        private static void FixedUpdate(this AttackComponent self)
        {
            if (!self.IsInAttack)
                return;
            
            // HitStop（顿帧）期间冻结判定推进：跳过 Physics 扫描，避免重复扫与高频 GC/开销。
            // 注意：顿帧期间 Animancer 图是暂停的，HitBox 的激活窗口也不会推进；恢复后会继续按窗口扫，不会丢判定。
            if (self.HitStop != null && self.HitStop.IsHitStopActive)
            {
                return;
            }
            self.UpdateHitDetection();
        }
        
        #endregion

        #region 配置加载
        
        /// <summary>
        /// 异步加载攻击配置
        /// </summary>
        private static async ETTask LoadConfigAsync(this AttackComponent self)
        {
            var id = self.RoleIdentity.RoleSkillSetTable.BasicAttackSkillId;
            var skillTable = SkillConfig.Instance.Get(id);
            var configAsset = await ResourcesLoadManager.Instance.LoadAssetAsync<AttackConfigAsset>(skillTable.SkillAsset);
            if (configAsset == null)
            {
                Log.Error($"AttackComponent: Failed to load config from {skillTable.SkillAsset}  {id}");
                return;
            }

            self.Config = configAsset.Config;
        }
        #endregion

        #region 状态管理
        
        /// <summary>
        /// 重置所有状态
        /// </summary>
        private static void ResetState(this AttackComponent self)
        {
            self.State = AttackState.Idle;
            self.CurrentSegmentIndex = -1;
            self.CurrentSegment = null;
            self.ComboCount = 0;
            self.HasBufferedInput = false;
            self.BufferedInputType = ComboInputType.None;
            self.BufferedInputTime = 0;
            self.HasHitThisSegment = false;
            self.HitTargetsThisSegment.Clear();
            self.TotalHitCount = 0;
            self.IsMovementActive = false;
            self.TrackTarget = null;
            self.CurrentSegmentEnded = false;
            self.IsInputBufferWindowOpen = false;
            self.IsCancelWindowOpen = false;
            self.ComboTimeoutAtCombatMs = 0;
            self.AttackLayerFadeOutAtCombatMs = 0;
        }

        /// <summary>
        /// 清理定时器
        /// </summary>
        private static void CleanupTimers(this AttackComponent self)
        {
            self.ComboTimeoutAtCombatMs = 0;
            self.AttackLayerFadeOutAtCombatMs = 0;
        }
        
        #endregion

        #region 输入处理
        
        /// <summary>
        /// 处理攻击输入
        /// </summary>
        /// <param name="inputType">输入类型</param>
        /// <returns>是否成功处理输入</returns>
        public static bool HandleAttackInput(this AttackComponent self,AnimatorComponent animatorComponent,ComboInputType inputType = ComboInputType.Normal)
        {
            if (self.Config == null || self.Config.Segments.Count == 0)
            {
                Log.Warning("AttackComponent: Config not loaded or no segments");
                return false;
            }

            if (self.AnimatorComponent == null)
            {
                self.AnimatorComponent = animatorComponent;
            }
            // 记录输入时间
            self.LastInputTime = self.GetCombatNowMs();

            // 如果不在攻击状态，开始第一段攻击
            if (!self.IsInAttack)
            {
                return self.StartAttack(0, inputType);
            }

            // 若处于 HitStop（顿帧）中：继续收输入，但不推进攻击段（保证“停顿中也能搓招”的手感）
            if (self.IsInHitStop())
            {
                self.BufferInput(inputType);
                return true;
            }

            // 先判断是否存在可衔接的下一段（无下一段则不缓存，避免脏输入滞留）
            int nextIndexCandidate = self.GetNextSegmentIndex(inputType);
            if (nextIndexCandidate < 0)
            {
                if (self.State == AttackState.Recovery && self.CurrentSegmentEnded)
                {
                    // 这里做一次“软退出”，只清理必要状态，然后立刻起手第一段
                    self.SoftExitForRestart();
                    return self.StartAttack(0, inputType);
                }
                return false;
            }

            // 处于后摇阶段时，Layer0 可能已经恢复到 Move/Jump 等基础动画（不再推进攻击 clip）。
            // 这时不能依赖“攻击动画是否结束”的判定来切段，否则输入会被缓存但永远等不到消费。
            // 进入 Recovery 且已标记本段自然结束（CurrentSegmentEnded），视为可立即衔接。
            if (self.State == AttackState.Recovery && self.CurrentSegmentEnded)
            {
                return self.StartAttack(nextIndexCandidate, inputType);
            }

            // 动画已结束/到达结束阈值：直接切下一段（保证极限手速也能丝滑）
            if (self.IsCurrentAnimationEnded())
            {
                return self.StartAttack(nextIndexCandidate, inputType);
            }

            // 无论是否已到输入窗口，都缓存输入；真正消费发生在动画结束时
            // 配合 InputBufferWindowMs 做过期控制
            self.BufferInput(inputType);
            self.ResetComboTimeout();
            return true;
        }

        /// <summary>
        /// 缓存输入
        /// </summary>
        private static void BufferInput(this AttackComponent self, ComboInputType inputType)
        {
            self.HasBufferedInput = true;
            self.BufferedInputType = inputType;
            self.BufferedInputTime = self.GetCombatNowMs();
        }

        private static void ClearBufferedInput(this AttackComponent self)
        {
            self.HasBufferedInput = false;
            self.BufferedInputType = ComboInputType.None;
            self.BufferedInputTime = 0;
        }

        /// <summary>
        /// 获取下一段攻击索引
        /// </summary>
        private static int GetNextSegmentIndex(this AttackComponent self, ComboInputType inputType)
        {
            if (self.CurrentSegment == null)
            {
                // 没有当前攻击段，返回第一段
                return self.Config.Segments.Count > 0 ? 0 : -1;
            }

            // 检查分支连击
            if (self.CurrentSegment.ComboBranches.TryGetValue(inputType, out int branchId))
            {
                int branchIndex = self.Config.GetSegmentIndexById(branchId);
                if (branchIndex >= 0)
                {
                    return branchIndex;
                }
            }

            // 默认线性连击
            int nextIndex = self.CurrentSegmentIndex + 1;
            if (nextIndex < self.Config.Segments.Count)
            {
                return nextIndex;
            }

            return -1;
        }
        
        #endregion

        #region 攻击执行
        
        /// <summary>
        /// 开始攻击
        /// </summary>
        private static bool StartAttack(this AttackComponent self, int segmentIndex, ComboInputType inputType)
        {
            if (segmentIndex < 0 || segmentIndex >= self.Config.Segments.Count)
            {
                Log.Warning($"AttackComponent: Invalid segment index {segmentIndex}");
                return false;
            }

            var segment = self.Config.Segments[segmentIndex];
            if (segment == null || segment.AnimationClipTrans == null || !segment.AnimationClipTrans.IsValid())
            {
                Log.Warning($"AttackComponent: Segment {segmentIndex} not loaded");
                return false;
            }
            
            // 重置攻击段状态
            self.ResetSegmentState(segment);
            // 播放动画
            var attackLayer = self.AnimatorComponent.AttackLayer;
            if (attackLayer == null)
            {
                self.AnimatorComponent.Animancer.Layers.SetMinCount(2);
                attackLayer = self.AnimatorComponent.Animancer.Layers[1];
                attackLayer.Weight = 0f;
                self.AnimatorComponent.AttackLayer = attackLayer;
            }
            
            var layer0State = self.AnimatorComponent.MoveMixer.State;
            if (layer0State != null)
            {
                layer0State.Speed = 0f;
                self.AnimatorComponent.MoveMixer.State.Parameter = 0;
                layer0State.NormalizedTime = 0f;
            }
            AnimancerState animState;
            
            if (segmentIndex == 0)
            {
                // 0 秒淡入等价于“立刻到 1”，同时也会取消之前的 FadeGroup。
                animState = attackLayer.Play(segment.AnimationClipTrans,0);
                attackLayer.StartFade(1f, 0f);
                attackLayer.Weight = 1f;
            }
            else
            {
                animState = attackLayer.Play(segment.AnimationClipTrans);
                float fadeIn = Mathf.Max(0.05f, segment.AnimationClipTrans.FadeDuration);
                attackLayer.StartFade(1f, fadeIn);
            }
            
            if (animState == null)
            {
                Log.Error($"AttackComponent: Failed to play animation for segment {segmentIndex}");
                return false;
            }
            animState.Time = 0;

            // 更新状态
            self.CurrentAnimState = animState;
            self.CurrentSegmentIndex = segmentIndex;
            self.CurrentSegment = segment;
            self.State = AttackState.Attacking;
            self.CurrentSegmentEnded = false;
            self.IsMovementActive = self.CurrentSegment.Movement.EnableMovement;
            self.ClearBufferedInput();
            self.CancelAttackLayerFadeOutTimer();
            
            self.BindAnimancerEvents(animState, segment);

            // 更新连击计数
            self.ComboCount++;
            self.OnComboCountChanged?.Invoke(self.ComboCount);
            
            // 初始化位移
            self.InitializeMovement(segment);

            // 重置超时定时器
            self.ResetComboTimeout();

            // 播放攻击特效和音效
            self.PlayAttackEffects(segment);

            // 触发攻击开始事件
            self.OnAttackStart?.Invoke(segmentIndex);
            return true;
        }

        /// <summary>
        /// 重置攻击段状态
        /// </summary>
        private static void ResetSegmentState(this AttackComponent self, AttackSegmentData segment)
        {
            // 段切换前：先恢复上一段接管过的 Active（避免残留显隐影响新段/其他系统）
            self.RestoreAttachedActives();

            self.HasHitThisSegment = false;
            self.HitTargetsThisSegment.Clear();
            self.IsMovementActive = false;
            self.IsInputBufferWindowOpen = false;
            self.IsCancelWindowOpen = false;
            // 重置判定框状态
            if (segment.HitBoxes != null)
            {
                foreach (var hitBox in segment.HitBoxes)
                {
                    hitBox.IsActive = false;
                }
            }
        }

        /// <summary>
        /// 初始化攻击位移
        /// </summary>
        private static void InitializeMovement(this AttackComponent self, AttackSegmentData segment)
        {
            if (segment.Movement == null || !segment.Movement.EnableMovement)
                return;
            self.MovementStartPosition = self.Player.position;

            // 如果启用追踪，寻找最近目标
            if (segment.Movement.TrackTarget)
            {
                self.TrackTarget = self.FindNearestTarget(segment.Movement.TrackRange);
                if (self.TrackTarget != null)
                {
                    Vector3 direction = (self.TrackTarget.position - self.Player.position).normalized;
                    direction.y = 0;
                    self.MovementTargetPosition = self.MovementStartPosition + direction * segment.Movement.Distance;
                }
                else
                {
                    self.MovementTargetPosition = self.MovementStartPosition + self.Player.forward * segment.Movement.Distance;
                }
            }
            else
            {
                self.MovementTargetPosition = self.MovementStartPosition + self.Player.forward * segment.Movement.Distance;
            }
            
            if(segment.Movement.Distance > 0f)
            {
                //self.CameraFollow.SetCameraOffest(new Vector3(0f, 0f, -1.5f));
                //self.CameraFollow.SetCameraFov(1.5f);
            }
        }

        /// <summary>
        /// TODO 寻找最近目标
        /// </summary>
        private static Transform FindNearestTarget(this AttackComponent self, float range)
        {
            // 这里需要根据实际项目的目标查找系统实现
            // 示例实现：
            /*var targetComponent = unit.GetComponent<TargetSearchComponent>();
            return targetComponent?.FindNearestEnemy(range);*/
            return null;
        }

        /// <summary>
        /// TODO 播放攻击特效和音效
        /// </summary>
        private static void PlayAttackEffects(this AttackComponent self, AttackSegmentData segment)
        {
            // 视觉/音效统一走 VisualEffects / SoundEffects 轨道（更贴近时间轴编辑/Animancer事件驱动）。
            // 这里暂时保留入口，具体播放系统按项目的 VFX/SFX 管线接入。
        }
        
        #endregion

        #region 动画状态更新
        
        /// <summary>
        /// 更新动画状态
        /// </summary>
        private static void UpdateAnimationState(this AttackComponent self)
        {
            if (self.State != AttackState.Attacking)
                return;

            if (self.CurrentAnimState == null || self.CurrentSegment == null)
            {
                self.ExitAttackState();
                return;
            }
            
            if (self.CurrentAnimState.HasEvents)
            {
                return;
            }

            // 检查动画是否结束
            if (self.IsCurrentAnimationEnded())
            {
                self.OnCurrentAnimationEnd();
            }
        }

        /// <summary>
        /// 检查当前动画是否已结束
        /// </summary>
        private static bool IsCurrentAnimationEnded(this AttackComponent self)
        {
            if (self.CurrentAnimState == null || self.CurrentSegment == null)
                return true;
            
            float endTime = self.CurrentSegment.TimeWindow.AnimationEnd;
            if (endTime <= 0f)
            {
                endTime = 1f;
            }
            endTime = Mathf.Clamp01(endTime);
            return self.CurrentAnimState.NormalizedTime >= endTime;
        }

        /// <summary>
        /// 当前动画结束处理
        /// </summary>
        private static void OnCurrentAnimationEnd(this AttackComponent self)
        {
            int completedIndex = self.CurrentSegmentIndex;

            // 触发攻击结束事件
            if (!self.CurrentSegmentEnded && completedIndex >= 0)
            {
                self.CurrentSegmentEnded = true;
                self.OnAttackEnd?.Invoke(completedIndex);
            }

            // 检查是否有缓冲输入（且未过期）
            if (self.HasBufferedInput && self.IsBufferedInputValid())
            {
                var inputType = self.BufferedInputType;
                self.ClearBufferedInput();

                int nextIndex = self.GetNextSegmentIndex(inputType);
                if (nextIndex >= 0)
                {
                    self.StartAttack(nextIndex, inputType);
                }
                return;
            }

            // 没有后续攻击，进入后摇状态
            self.State = AttackState.Recovery;
            self.ScheduleAttackLayerFadeOutTimer();
            
        }
        
        #endregion

        #region 命中检测
        
        /// <summary>
        /// 更新命中检测
        /// </summary>
        private static void UpdateHitDetection(this AttackComponent self)
        {
            if (self.State != AttackState.Attacking)
                return;

            if (self.CurrentSegment?.HitBoxes == null)
                return;

            foreach (var hitBox in self.CurrentSegment.HitBoxes)
            {
                // 执行命中检测
                if (hitBox.IsActive)
                {
                    self.PerformHitDetection(hitBox);
                }
            }
        }

        #region Animancer Events 绑定（Pro）

        private static void BindAnimancerEvents(this AttackComponent self, AnimancerState animState, AttackSegmentData segment)
        {
            if (animState == null || segment == null)
                return;

            try
            {
                // 通过 owner 绑定，避免事件所有权冲突。
                if (animState.Events(self, out var events))
                {
                    // 首次初始化时，确保没有遗留事件。
                    events.Clear();
                }
                else
                {
                    // 已有事件时也清掉，确保不同 Segment 不会复用到旧事件（同 Clip 复用 state 的情况很常见）。
                    events.Clear();
                }

                // 窗口：输入缓冲 / 取消。由事件驱动置位，避免分散在逻辑里到处比较时间。
                float inputWindow = Mathf.Clamp01(segment.TimeWindow.InputBufferStart);
                if (inputWindow > 0f)
                {
                    events.Add(inputWindow, () =>
                    {
                        if (self.CurrentAnimState != animState || self.CurrentSegment != segment || self.State != AttackState.Attacking)
                            return;
                        self.IsInputBufferWindowOpen = true;
                    });
                }
                else
                {
                    // 0 表示从一开始就可缓冲
                    self.IsInputBufferWindowOpen = true;
                }

                float cancelWindow = Mathf.Clamp01(segment.TimeWindow.CancelableTime);
                if (cancelWindow > 0f)
                {
                    events.Add(cancelWindow, () =>
                    {
                        if (self.CurrentAnimState != animState || self.CurrentSegment != segment || self.State != AttackState.Attacking)
                            return;
                        self.IsCancelWindowOpen = true;
                    });
                }
                else
                {
                    // 0 表示从一开始就可取消
                    self.IsCancelWindowOpen = true;
                }

                // 段结束阈值：使用 TimeWindow.AnimationEnd（可早于 1），实现提前进入后摇/接段。
                float endTime = segment.TimeWindow.AnimationEnd;
                if (endTime > 0f && endTime < 1f)
                {
                    events.NormalizedEndTime = Mathf.Clamp01(endTime);
                }
                // endTime <= 0 或 >= 1 则保持默认（NaN -> 自动取 1 或 0，取决于播放方向）

                // 段结束事件：不要用 End Event（events.OnEnd），因为 End Event 在超过时间后会“每帧触发”，
                // 如果回调没有明确停止该动画（例如立刻播放其他 State），Animancer 会给出 OptionalWarning.EndEventInterrupt。
                // 这里改为普通 Animancer Event（只触发一次），触发点与 AnimationEnd 对齐。
                float endTrigger = endTime;
                if (endTrigger <= 0f)
                {
                    if (animState.IsLooping)
                    {
                        endTrigger = AnimancerEvent.AlmostOne;
                    }
                    else
                    {
                        endTrigger = 1f;
                    }
                }
                else
                {
                    endTrigger = Mathf.Clamp01(endTrigger);
                    if (animState.IsLooping && Mathf.Approximately(endTrigger, 1f))
                    {
                        endTrigger = AnimancerEvent.AlmostOne;
                    }
                }
                events.Add(endTrigger, () =>
                {
                    // 防止旧 state 的事件误触发；且只在 Attacking 阶段响应（进入 Recovery/Idle 后不再触发）
                    if (self.CurrentAnimState == animState && self.State == AttackState.Attacking)
                    {
                        self.OnCurrentAnimationEnd();
                    }
                });

                // HitBox 开关事件：StartTime -> active, EndTime -> inactive
                if (segment.HitBoxes != null)
                {
                    int hint = 0;
                    for (int i = 0; i < segment.HitBoxes.Count; i++)
                    {
                        var hitBox = segment.HitBoxes[i];
                        if (hitBox == null)
                            continue;

                        float start = Mathf.Clamp01(hitBox.NormalizedStart);
                        float end = Mathf.Clamp01(hitBox.NormalizedEnd);
                        if (end < start)
                        {
                            continue;
                        }

                        if (Mathf.Approximately(start, end))
                        {
                            hint = events.Add(hint, start, () =>
                            {
                                if (self.CurrentAnimState != animState || self.CurrentSegment != segment ||
                                    self.State != AttackState.Attacking)
                                    return;
                                hitBox.IsActive = true;
                                self.UpdateHitDetection();
                                hitBox.IsActive = false;
                            });
                        }
                        else
                        {
                            hint = events.Add(hint, start, () =>
                            {
                                if (self.CurrentAnimState != animState || self.CurrentSegment != segment ||
                                    self.State != AttackState.Attacking)
                                    return;
                                hitBox.IsActive = true;
                            });

                            hint = events.Add(hint, end, () =>
                            {
                                if (self.CurrentAnimState != animState || self.CurrentSegment != segment)
                                    return;
                                hitBox.IsActive = false;
                            });
                        }
                    }
                }

                // VisualEffects：一次性触发（使用 StartTime）
                if (segment.VisualEffects != null)
                {
                    int hint = 0;
                    for (int i = 0; i < segment.VisualEffects.Count; i++)
                    {
                        var vfx = segment.VisualEffects[i];
                        if (vfx == null)
                            continue;
                        float t = Mathf.Clamp01(vfx.NormalizedStart);
                        hint = events.Add(hint, t, () =>
                        {
                            if (self.CurrentAnimState != animState || self.CurrentSegment != segment || self.State != AttackState.Attacking)
                                return;
                            self.PlayVisualEffect(vfx);
                        });
                    }
                }

                // SoundEffects：一次性触发（使用 StartTime）
                if (segment.SoundEffects != null)
                {
                    int hint = 0;
                    for (int i = 0; i < segment.SoundEffects.Count; i++)
                    {
                        var sfx = segment.SoundEffects[i];
                        if (sfx == null)
                            continue;
                        float t = Mathf.Clamp01(sfx.NormalizedStart);
                        hint = events.Add(hint, t, () =>
                        {
                            if (self.CurrentAnimState != animState || self.CurrentSegment != segment || self.State != AttackState.Attacking)
                                return;
                            self.PlaySoundEffect(sfx);
                        });
                    }
                }

                // AttachedActives：区间内启用，区间外禁用（支持同一对象多区间重叠：引用计数）
                if (segment.AttachedActives != null)
                {
                    int hint = 0;
                    float endNorm = segment.TimeWindow != null ? segment.TimeWindow.AnimationEnd : 1f;
                    if (endNorm <= 0f) endNorm = 1f;
                    endNorm = Mathf.Clamp01(endNorm);

                    for (int i = 0; i < segment.AttachedActives.Count; i++)
                    {
                        var a = segment.AttachedActives[i];
                        if (a == null)
                            continue;

                        float start = Mathf.Clamp01(a.NormalizedStart);
                        float end = Mathf.Clamp01(a.NormalizedEnd);
                        if (end < start)
                        {
                            (start, end) = (end, start);
                        }

                        // 对齐运行时段结束语义：不允许超过 AnimationEnd
                        start = Mathf.Min(start, endNorm);
                        end = Mathf.Min(end, endNorm);
                        if (end <= start)
                        {
                            continue;
                        }

                        hint = events.Add(hint, start, () =>
                        {
                            if (self.CurrentAnimState != animState || self.CurrentSegment != segment || self.State != AttackState.Attacking)
                                return;
                            self.ApplyAttachedActiveDelta(segment, a, isStart: true);
                        });

                        hint = events.Add(hint, end, () =>
                        {
                            if (self.CurrentAnimState != animState || self.CurrentSegment != segment)
                                return;
                            self.ApplyAttachedActiveDelta(segment, a, isStart: false);
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"AttackComponent: BindAnimancerEvents failed - {e}");
            }
        }

        private static void PlayVisualEffect(this AttackComponent self, VisualEffectData vfx)
        {
            if (vfx == null || vfx.Prefab == null || self.Player == null)
            {
                return;
            }

            var instance = GameObjectPool.Instance.GetObjectSync(vfx.Prefab.name, PoolType.Effect);
            if (instance == null)
            {
                return;
            }
            AnimationEffectSystem effectSystem = instance.GetComponent<AnimationEffectSystem>();
            if (effectSystem == null)
            {
                return;
            }
            if (vfx.FollowTarget)
            {
                instance.transform.SetParent(self.Player, worldPositionStays: false);
                instance.transform.localPosition = vfx.Offset;
                instance.transform.localRotation = Quaternion.Euler(vfx.RotationEuler);
            }
            else
            {
                instance.transform.SetParent(self.EffectRoot.transform, worldPositionStays: false);
                instance.transform.position = self.Player.TransformPoint(vfx.Offset);
                instance.transform.rotation = self.Player.rotation * Quaternion.Euler(vfx.RotationEuler);
            }
            instance.transform.localScale = Vector3.one;
            effectSystem.Play(vfx).Forget();
        }

        private static void PlaySoundEffect(this AttackComponent self, SoundEffectData sfx)
        {
            if (sfx == null || sfx.Clip == null || self.Player == null)
                return;

            try
            {
                float volume = Mathf.Clamp01(sfx.Volume);
                // 商用项目建议替换为统一的音频系统（这里先用 PlayClipAtPoint 作为最小可用实现）
                AudioSource.PlayClipAtPoint(sfx.Clip, self.Player.position, volume);
            }
            catch (Exception e)
            {
                Log.Warning($"AttackComponent: PlaySoundEffect failed - {e.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 执行命中检测
        /// </summary>
        private static void PerformHitDetection(this AttackComponent self, HitBoxData hitBox)
        {
            // 计算判定框世界坐标
            Vector3 worldPosition = self.Player.position + self.Player.rotation * hitBox.Offset;
            Quaternion worldRotation = self.Player.rotation * Quaternion.Euler(hitBox.RotationEuler);

            // 根据形状类型进行检测
            var hitTargets = ListComponent<GameObject>.Create();
            var layer = LayerMask.GetMask("Enemy");
            try
            {
                switch (hitBox.ShapeType)
                {
                    case HitShapeType.Box:
                        PhysicsHelper.OverlapBox(worldPosition, hitBox.Size * 0.5f, worldRotation, hitTargets, layer);
                        break;
                    case HitShapeType.Sphere:
                        PhysicsHelper.OverlapSphere(worldPosition, hitBox.Size.x, hitTargets, layer);
                        break;
                    case HitShapeType.Fan:
                        PhysicsHelper.OverlapFan(worldPosition, worldRotation * Vector3.forward, hitBox.Size.x, hitBox.Size.y, hitTargets, layer, hitBox.Size.z);
                        break;
                    case HitShapeType.Capsule:
                        PhysicsHelper.OverlapCapsule(worldPosition, hitBox.Size.x, hitBox.Size.y, worldRotation, hitTargets, layer);
                        break;
                }

                if (hitTargets.Count == 0)
                {
                    return;
                }

                Log.Error($"命中个数 {hitTargets.Count}");
                // 处理命中目标
                foreach (var target in hitTargets)
                {
                    if (target == null)
                        continue;

                    // 检查是否已命中过该目标
                    if (!self.HitTargetsThisSegment.Add(target))
                        continue;

                    // 记录命中
                    self.HasHitThisSegment = true;
                    self.TotalHitCount++;

                    // 处理命中效果（使用 HitBox 的独立效果配置）
                    self.ProcessHit(target, hitBox);
                }
            }
            finally
            {
                ObjectPool.Recycle(hitTargets);
            }
        }

        /// <summary>
        /// 处理命中效果（使用 HitBox 的独立效果配置）
        /// </summary>
        private static void ProcessHit(this AttackComponent self, GameObject target, HitBoxData hitBox)
        {
            var effect = hitBox.Effect;
            var feedback = hitBox.Feedback;

            // 计算伤害
            float damage = self.CalculateDamage(target, effect);

            //TODO 应用伤害 
            /*var targetHealth = target.GetComponent<HealthComponent>();
            targetHealth?.TakeDamage(damage, attacker);*/

            //应用受击反应
            //self.ApplyHitReaction(target, effect);

            // 播放命中特效和音效
            self.PlayHitEffects(target, hitBox);

            // 申请顿帧：由 CombatFeedbackComponent 统一合并/叠加，避免多目标命中导致重入与恢复错误
            int hitStopMs = feedback.HitStopMs;
            if (hitStopMs <= 0)
            {
                hitStopMs = self.Config?.DefaultHitStopMs ?? 0;
            }
            if (hitStopMs > 0)
            {
                self.HitStop.RequestHitStop(hitStopMs,self.AnimatorComponent.Animancer);
            }

            // 应用屏幕震动
            if (feedback.ScreenShakeIntensity > 0)
            {
                //CameraManager.Instance?.Shake(feedback.ScreenShakeIntensity, feedback.ScreenShakeDuration);
            }

            // 触发命中事件
            self.OnHit?.Invoke(target, self.CurrentSegment);

            //Log.Debug($"AttackComponent: Hit target {target.name}, damage: {damage}");
        }

        /// <summary>
        /// 计算伤害（使用 HitEffectData）
        /// </summary>
        private static float CalculateDamage(this AttackComponent self, GameObject target, HitEffectData effect)
        {
            // 获取攻击者属性
            /*var attackerAttr = attacker.GetComponent<AttributeComponent>();
            float baseAttack = attackerAttr?.GetAttribute(AttributeType.Attack) ?? 100f;

            // 获取目标防御
            var targetAttr = target.GetComponent<AttributeComponent>();
            float defense = targetAttr?.GetAttribute(AttributeType.Defense) ?? 0f;

            // 计算最终伤害
            float damage = baseAttack * effect.DamageMultiplier;
            damage = Mathf.Max(1, damage - defense);*/
            float damage = 100 * effect.DamageMultiplier;
            return damage;
        }

        /// <summary>
        /// 应用受击反应（使用 HitEffectData）
        /// </summary>
        private static void ApplyHitReaction(this AttackComponent self, GameObject target, HitEffectData effect)
        {
            var hitReactionComponent = target.GetComponent<HitReactionComponent>();
            if (hitReactionComponent == null)
                return;

            var attacker = self.GetParent<Unit>();
            Vector3 hitDirection = (target.transform.position - self.Player.position).normalized;
            hitDirection.y = 0;

            switch (effect.HitReaction)
            {
                case HitReactionType.Light:
                    hitReactionComponent.PlayLightHit(hitDirection, effect.HitStunMs);
                    break;
                case HitReactionType.Medium:
                    hitReactionComponent.PlayMediumHit(hitDirection, effect.HitStunMs);
                    break;
                case HitReactionType.Heavy:
                    hitReactionComponent.PlayHeavyHit(hitDirection, effect.HitStunMs);
                    break;
                case HitReactionType.Knockback:
                    hitReactionComponent.PlayKnockback(hitDirection, effect.KnockbackForce, effect.HitStunMs);
                    break;
                case HitReactionType.Knockup:
                    hitReactionComponent.PlayKnockup(effect.KnockupForce, effect.HitStunMs);
                    break;
                case HitReactionType.Knockdown:
                    hitReactionComponent.PlayKnockdown(hitDirection, effect.KnockbackForce, effect.HitStunMs);
                    break;
            }
        }

        /// <summary>
        /// 播放命中特效和音效
        /// </summary>
        private static void PlayHitEffects(this AttackComponent self, GameObject target, HitBoxData hitBox)
        {
            // 命中特效/音效同样建议通过轨道驱动（例如在命中点触发一条 VisualEffectData）。
            // 这里保留扩展点，避免把资源路径硬编码在配置里。
        }
        
        #endregion

        #region 连击超时
        
        /// <summary>
        /// 重置连击超时定时器
        /// </summary>
        private static void ResetComboTimeout(this AttackComponent self)
        {
            int timeoutMs = self.GetCurrentSegmentComboTimeoutMs();
            self.ComboTimeoutAtCombatMs = self.GetCombatNowMs() + timeoutMs;
        }

        /// <summary>
        /// 获取当前段的连击超时（毫秒）。
        /// 规则：段超时 = 段时长(ms) + 段偏移(ComboTimeoutOffsetMs)，用于避免全局超时小于动画时长导致提前退出。
        /// </summary>
        private static int GetCurrentSegmentComboTimeoutMs(this AttackComponent self)
        {
            // 默认兜底
            int fallback = self.Config?.ComboTimeoutMs ?? 800;

            var seg = self.CurrentSegment;
            if (seg == null)
            {
                return Mathf.Max(0, fallback);
            }

            float durSec = Mathf.Max(0f, seg.Duration);
            // Duration 若异常为 0，则回退使用兜底超时，避免一直被判定为立即超时
            if (durSec <= 0f)
            {
                return Mathf.Max(0, fallback);
            }

            int durMs = Mathf.RoundToInt(durSec * seg.TimeWindow.AnimationEnd * 1000f); //转成毫秒
            int offsetMs = Mathf.Max(0, seg.ComboTimeoutOffsetMs);
            int total = durMs + offsetMs;
            return Mathf.Max(0, total);
        }

        /// <summary>
        /// 连击超时处理（由定时器调用）
        /// </summary>
        public static void OnComboTimeout(this AttackComponent self)
        {
            self.ExitAttackState();
        }
        
        #endregion

        #region 攻击取消
        
        /// <summary>
        /// 尝试用技能取消当前攻击
        /// </summary>
        /// <param name="skillId">技能ID</param>
        /// <returns>是否成功取消</returns>
        public static bool TryCancelWithSkill(this AttackComponent self, int skillId)
        {
            if (!self.IsAttacking)
                return true;

            if (!self.CanCancelAttack)
                return false;

            if (self.CurrentSegment?.CancelableSkillIds == null)
                return false;

            if (!self.CurrentSegment.CancelableSkillIds.Contains(skillId))
                return false;

            self.ExitAttackState();
            return true;
        }

        /// <summary>
        /// 强制取消当前攻击
        /// </summary>
        public static void ForceCancel(this AttackComponent self)
        {
            self.ExitAttackState();
        }

        /// <summary>
        /// 退出攻击状态
        /// </summary>
        public static void ExitAttackState(this AttackComponent self)
        {
            if (self.State == AttackState.Idle)
                return;
            // 退出攻击状态时，淡出攻击层
            self.FadeOutAttackLayer();
            self.CancelAttackLayerFadeOutTimer();

            int lastIndex = self.CurrentSegmentIndex;

            // 清理定时器
            self.CleanupTimers();

            // 恢复本次攻击流程接管过的 Active（避免退出后残留显隐）
            self.RestoreAttachedActives();

            // 触发连击重置事件
            if (self.ComboCount > 0)
            {
                self.OnComboReset?.Invoke();
            }

            // 触发攻击结束事件
            if (lastIndex >= 0 && !self.CurrentSegmentEnded)
            {
                self.OnAttackEnd?.Invoke(lastIndex);
            }

            // 重置状态
            self.ResetState();

//            Log.Debug("AttackComponent: Exited attack state");
        }

        /// <summary>
        /// 软退出（用于“最后一段结束后立刻重起手”）。
        /// 目标：不淡出 AttackLayer，不触发退出语义（例如 ComboReset），只清理必要状态，避免闪 Idle。
        /// </summary>
        private static void SoftExitForRestart(this AttackComponent self)
        {
            // 清理定时器：避免旧的连击超时在新起手过程中触发
            self.CleanupTimers();
            self.CancelAttackLayerFadeOutTimer();
            // 软退出也应恢复 Active，避免“立刻重起手”时上一段显隐残留
            self.RestoreAttachedActives();

            // 软重置攻击运行时状态：保持结构与 ResetState 一致，但不做攻击层淡出、不触发事件
            self.State = AttackState.Idle;
            self.CurrentSegmentIndex = -1;
            self.CurrentSegment = null;
            self.HasBufferedInput = false;
            self.BufferedInputType = ComboInputType.None;
            self.BufferedInputTime = 0;
            self.HasHitThisSegment = false;
            self.HitTargetsThisSegment.Clear();
            self.IsMovementActive = false;
            self.TrackTarget = null;
            self.CurrentSegmentEnded = false;
            self.IsInputBufferWindowOpen = false;
            self.IsCancelWindowOpen = false;
            self.ComboTimeoutAtCombatMs = 0;
            self.AttackLayerFadeOutAtCombatMs = 0;
        }

        #region AttachedActives（运行时显隐控制）

        private static void RestoreAttachedActives(this AttackComponent self)
        {
            var dict = self.AttachedActiveOriginalStates;
            if (dict == null || dict.Count == 0)
            {
                // 仍然清掉 refCounts，避免残留计数影响后续段
                self.AttachedActiveRefCounts?.Clear();
                return;
            }

            foreach (var kv in dict)
            {
                var go = kv.Key;
                if (go == null)
                {
                    continue;
                }

                bool original = kv.Value;
                if (go.activeSelf != original)
                {
                    go.SetActive(original);
                }
            }

            dict.Clear();
            self.AttachedActiveRefCounts?.Clear();
        }

        private static void ApplyAttachedActiveDelta(this AttackComponent self, AttackSegmentData segment, AttachedActiveData data, bool isStart)
        {
            if (self == null || self.IsDisposed || segment == null || data == null)
            {
                return;
            }

            var root = self.Player;
            if (root == null)
            {
                return;
            }

            string path = data.RelativePath ?? string.Empty;
            if (string.IsNullOrEmpty(path))
            {
                // 约束：空路径不控制（避免误操作 root）
                return;
            }

            var tf = root.Find(path);
            var go = tf != null ? tf.gameObject : null;
            if (go == null)
            {
                return;
            }

            // 记录原始状态（只记录一次，退出攻击/切段时恢复）
            self.AttachedActiveOriginalStates ??= new Dictionary<GameObject, bool>();
            if (!self.AttachedActiveOriginalStates.ContainsKey(go))
            {
                self.AttachedActiveOriginalStates[go] = go.activeSelf;
            }

            self.AttachedActiveRefCounts ??= new Dictionary<GameObject, int>();
            self.AttachedActiveRefCounts.TryGetValue(go, out int count);
            count += isStart ? 1 : -1;
            if (count < 0) count = 0;

            if (count == 0)
            {
                self.AttachedActiveRefCounts.Remove(go);
                // 轨道语义：区间外隐藏（退出攻击时会恢复原始状态）
                if (go.activeSelf)
                {
                    go.SetActive(false);
                }
            }
            else
            {
                self.AttachedActiveRefCounts[go] = count;
                // 轨道语义：区间内显示
                if (!go.activeSelf)
                {
                    go.SetActive(true);
                }
            }
        }

        #endregion

        #region AttackLayer 延迟淡出（定时器）

        private static void CancelAttackLayerFadeOutTimer(this AttackComponent self)
        {
            self.AttackLayerFadeOutAtCombatMs = 0;
        }

        private static void ScheduleAttackLayerFadeOutTimer(this AttackComponent self)
        {
            self.CancelAttackLayerFadeOutTimer();

            int holdMs = self.Config?.RecoveryHoldMs ?? 200;
            if (holdMs <= 0)
            {
                // 立刻淡出（不额外占用 Timer）
                self.FadeOutAttackLayer();
                return;
            }

            self.AttackLayerFadeOutAtCombatMs = self.GetCombatNowMs() + holdMs;
        }

        public static void OnAttackLayerFadeOutTimer(this AttackComponent self)
        {
            // 兼容旧调用点：新实现不再依赖 TimerInvokeType
            self.AttackLayerFadeOutAtCombatMs = 0;
            if (!self.IsDisposed && self.State == AttackState.Recovery)
            {
                self.FadeOutAttackLayer();
            }
        }

        #endregion
        
        #endregion

        #region 辅助方法

        /// <summary>
        /// 淡出Attack层到指定权重
        /// </summary>
        private static void FadeOutAttackLayer(this AttackComponent self, float targetWeight = 0f, float duration = 0.25f)
        {
            var layer0State = self.AnimatorComponent.MoveMixer.State;
            if (layer0State != null)
            {
                layer0State.Speed = 1f;
                self.AnimatorComponent.MoveMixer.State.Parameter = 0;
                layer0State.NormalizedTime = 0f;
            }
            var attackLayer = self.AnimatorComponent?.AttackLayer;
            if (attackLayer != null)
            {
                attackLayer.StartFade(targetWeight, duration);
            }
        }

        /// <summary>
        /// 获取当前连击数
        /// </summary>
        public static int GetComboCount(this AttackComponent self)
        {
            return self.ComboCount;
        }

        /// <summary>
        /// 检查是否可以开始攻击
        /// </summary>
        public static bool CanStartAttack(this AttackComponent self)
        {
            if (self.Config == null || self.Config.Segments.Count == 0)
                return false;

            var unit = self.GetParent<Unit>();
            if (unit == null)
                return false;

            // TODO 检查角色状态（例如：是否被控制、是否在施法等）
            /*var stateComponent = unit.GetComponent<UnitStateComponent>();
            if (stateComponent != null)
            {
                if (stateComponent.IsStunned || stateComponent.IsCasting)
                    return false;
            }*/

            return true;
        }

        /// <summary>
        /// 获取当前攻击段信息（用于UI显示）
        /// </summary>
        public static (int index, string name, float progress) GetCurrentAttackInfo(this AttackComponent self)
        {
            if (!self.IsAttacking || self.CurrentSegment == null)
                return (-1, string.Empty, 0f);

            return (self.CurrentSegmentIndex, self.CurrentSegment.Name, self.CurrentNormalizedTime);
        }
        
        #endregion

        #region 输入缓冲过期控制

        private static bool IsBufferedInputValid(this AttackComponent self)
        {
            if (!self.HasBufferedInput)
                return false;

            int windowMs = self.Config?.InputBufferWindowMs ?? 200;
            if (windowMs <= 0)
                return true;

            long now = self.GetCombatNowMs();
            return now - self.BufferedInputTime <= windowMs;
        }

        private static void UpdateBufferedInputTimeout(this AttackComponent self)
        {
            if (!self.HasBufferedInput)
                return;

            if (!self.IsBufferedInputValid())
            {
                self.ClearBufferedInput();
            }
        }

        #endregion

        #region 顿帧

        private static bool IsInHitStop(this AttackComponent self)
        {
            var unit = self.GetParent<Unit>();
            var feedback = unit?.GetComponent<CombatFeedbackComponent>();
            return feedback != null && feedback.IsHitStopActive;
        }

        private static long GetCombatNowMs(this AttackComponent self)
        {
            var unit = self.GetParent<Unit>();
            var feedback = unit?.GetComponent<CombatFeedbackComponent>();
            if (feedback != null)
            {
                return feedback.NowCombatMs();
            }

            // 兜底：没有控制器时，退回原有 client frame time
            return TimeInfo.Instance.ClientFrameTime();
        }

        private static void UpdateComboTimeout(this AttackComponent self)
        {
            if (self.ComboTimeoutAtCombatMs <= 0)
            {
                return;
            }

            long now = self.GetCombatNowMs();
            if (now >= self.ComboTimeoutAtCombatMs)
            {
                self.ComboTimeoutAtCombatMs = 0;
                self.OnComboTimeout();
            }
        }

        private static void UpdateAttackLayerFadeOut(this AttackComponent self)
        {
            if (self.State != AttackState.Recovery)
            {
                return;
            }

            if (self.AttackLayerFadeOutAtCombatMs <= 0)
            {
                return;
            }

            long now = self.GetCombatNowMs();
            if (now >= self.AttackLayerFadeOutAtCombatMs)
            {
                self.AttackLayerFadeOutAtCombatMs = 0;
                self.FadeOutAttackLayer();
            }
        }

        #endregion
    }
}