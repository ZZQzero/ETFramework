using System;
using System.Collections.Generic;
using Animancer;
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
        private static void Awake(this AttackComponent self, string configPath)
        {
            self.ConfigPath = configPath;
            self.ResetState();
            Unit unit = self.GetParent<Unit>();
            self.AnimatorComponent = unit.GetComponent<AnimatorComponent>();
            self.CharacterController = unit.GetComponent<CharacterControllerComponent>();
            self.Player = unit.GetComponent<GameObjectComponent>().Transform;
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

            self.UpdateHitStop();
            self.UpdateBufferedInputTimeout();
            self.UpdateAnimationState();
        }

        [EntitySystem]
        private static void FixedUpdate(this AttackComponent self)
        {
            if (!self.IsInAttack)
                return;

            // 物理相关（位移/命中检测）放在 FixedUpdate，避免与 Rigidbody/地面检测打架
            self.UpdateMovement(Time.fixedDeltaTime);
            self.UpdateHitDetection();
        }
        
        #endregion

        #region 配置加载
        
        /// <summary>
        /// 异步加载攻击配置
        /// </summary>
        private static async ETTask LoadConfigAsync(this AttackComponent self)
        {
            if (string.IsNullOrEmpty(self.ConfigPath))
            {
                Log.Error("AttackComponent: ConfigPath is null or empty");
                return;
            }

            var configAsset = await ResourcesLoadManager.Instance.LoadAssetAsync<AttackConfigAsset>(self.ConfigPath);
            if (configAsset == null)
            {
                Log.Error($"AttackComponent: Failed to load config from {self.ConfigPath}");
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
            self.HitStopEndTime = 0;
            self.CurrentSegmentEnded = false;
            self.IsInputBufferWindowOpen = false;
            self.IsCancelWindowOpen = false;

            // 退出时确保外部运动关闭，避免残留
            if (self.CharacterController != null)
            {
                self.CharacterController.ExternalMotorActive = false;
                self.CharacterController.ExternalMotorVelocity = Vector3.zero;
            }

            // 恢复移动锁
            if (self.CharacterController != null && self.MovementLocked)
            {
                self.CharacterController.EnableMovement = self.PrevEnableMovement;
            }
            self.MovementLocked = false;
        }

        /// <summary>
        /// 清理定时器
        /// </summary>
        private static void CleanupTimers(this AttackComponent self)
        {
            var timerComponent = self.Root().GetComponent<TimerComponent>();
            if (timerComponent != null && self.ComboTimeoutTimer != 0)
            {
                timerComponent.Remove(ref self.ComboTimeoutTimer);
            }
        }
        
        #endregion

        #region 输入处理
        
        /// <summary>
        /// 处理攻击输入
        /// </summary>
        /// <param name="inputType">输入类型</param>
        /// <returns>是否成功处理输入</returns>
        public static bool HandleAttackInput(this AttackComponent self, ComboInputType inputType = ComboInputType.Normal)
        {
            if (self.Config == null || self.Config.Segments.Count == 0)
            {
                Log.Warning("AttackComponent: Config not loaded or no segments");
                return false;
            }

            // 记录输入时间
            self.LastInputTime = TimeInfo.Instance.ClientFrameTime();

            // 如果不在攻击状态，开始第一段攻击
            if (!self.IsInAttack)
            {
                return self.StartAttack(0, inputType);
            }

            // 如果在顿帧中，缓存输入
            if (self.IsInHitStop)
            {
                self.BufferInput(inputType);
                return true;
            }

            // 先判断是否存在可衔接的下一段（无下一段则不缓存，避免脏输入滞留）
            int nextIndexCandidate = self.GetNextSegmentIndex(inputType);
            if (nextIndexCandidate < 0)
            {
                // 已经进入后摇且本段自然结束：允许立刻从第一段重新起手（更丝滑）
                if (self.State == AttackState.Recovery && self.CurrentSegmentEnded)
                {
                    self.ExitAttackState();
                    return self.StartAttack(0, inputType);
                }
                return false;
            }

            // 处于后摇阶段时，Layer0 可能已经恢复到 Move/Jump 等基础动画（不再推进攻击 clip）。
            // 这时不能依赖“攻击动画是否结束”的判定来切段，否则输入会被缓存但永远等不到消费。
            // 约定：进入 Recovery 且已标记本段自然结束（CurrentSegmentEnded），视为可立即衔接。
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
            // 配合 InputBufferWindowMs 做过期控制，解决“太早按键被吞”的不流畅问题
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
            self.BufferedInputTime = TimeInfo.Instance.ClientFrameTime();
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

            if (self.AnimatorComponent == null)
            {
                self.AnimatorComponent = self.GetParent<Unit>().GetComponent<AnimatorComponent>();
            }

            // 重置攻击段状态
            self.ResetSegmentState(segment);

            // 播放动画
            var animState = self.AnimatorComponent.Animancer.Play(segment.AnimationClipTrans);
            if (animState == null)
            {
                Log.Error($"AttackComponent: Failed to play animation for segment {segmentIndex}");
                return false;
            }

            // 更新状态
            self.CurrentAnimState = animState;
            self.CurrentSegmentIndex = segmentIndex;
            self.CurrentSegment = segment;
            self.State = AttackState.Attacking;
            self.CurrentSegmentEnded = false;
            self.ClearBufferedInput();

            // 锁定常规移动（避免攻击过程中输入移动与位移/硬直互相覆盖）
            if (self.CharacterController != null && !self.MovementLocked)
            {
                self.PrevEnableMovement = self.CharacterController.EnableMovement;
                self.CharacterController.EnableMovement = false;
                self.MovementLocked = true;
            }
            
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

            Log.Debug($"AttackComponent: Started attack segment {segmentIndex} ({segment.Name})");
            return true;
        }

        /// <summary>
        /// 重置攻击段状态
        /// </summary>
        private static void ResetSegmentState(this AttackComponent self, AttackSegmentData segment)
        {
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
                    hitBox.IsCompleted = false;
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

            Vector3 startPos = self.CharacterController?.Rigidbody != null
                ? self.CharacterController.Rigidbody.position
                : self.Player.position;
            self.MovementStartPosition = startPos;

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
        /// 播放攻击特效和音效
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

            // 混合方案：
            // - AnimancerEvent.Sequence.OnEnd + NormalizedEndTime 驱动段结束（更贴合时间轴/可维护）
            // - 轮询仅作为兜底（例如事件绑定失败、或未初始化事件系统时）
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
            // 统一以配置的 TimeWindow.AnimationEnd（NormalizedTime）为准；未配置则默认 1.0
            float endTime = self.CurrentSegment.TimeWindow.AnimationEnd;
            if (endTime <= 0f)
                endTime = 1f;
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

            // 后摇阶段不应继续外部位移
            if (self.CharacterController != null)
            {
                self.CharacterController.ExternalMotorActive = false;
                self.CharacterController.ExternalMotorVelocity = Vector3.zero;

                // 后摇阶段允许恢复常规移动（避免“打完一段还锁死 800ms”带来的粘滞感）
                if (self.MovementLocked)
                {
                    self.CharacterController.EnableMovement = self.PrevEnableMovement;
                    self.MovementLocked = false;
                }
            }
            
            // 等待超时后退出攻击状态
            // 超时由定时器处理
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
                    endTrigger = 1f;
                }
                endTrigger = Mathf.Clamp01(endTrigger);
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
                        if (end <= start)
                            continue;

                        hint = events.Add(hint, start, () =>
                        {
                            if (self.CurrentAnimState != animState || self.CurrentSegment != segment || self.State != AttackState.Attacking)
                                return;
                            hitBox.IsActive = true;
                            hitBox.IsCompleted = false;
                        });

                        hint = events.Add(hint, end, () =>
                        {
                            if (self.CurrentAnimState != animState || self.CurrentSegment != segment)
                                return;
                            hitBox.IsActive = false;
                            hitBox.IsCompleted = true;
                        });
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
            }
            catch (Exception e)
            {
                Log.Warning($"AttackComponent: BindAnimancerEvents failed - {e}");
            }
        }

        private static void PlayVisualEffect(this AttackComponent self, VisualEffectData vfx)
        {
            if (vfx == null || vfx.Prefab == null || self.Player == null)
                return;

            try
            {
                if (vfx.FollowTarget)
                {
                    // 商用项目建议替换为统一的特效系统/对象池（这里先用原生 Instantiate 作为最小可用实现）
                    var instance = Object.Instantiate(vfx.Prefab, self.Player);
                    instance.transform.localPosition = vfx.Offset;
                    instance.transform.localRotation = Quaternion.identity;
                    if (vfx.Length > 0)
                    {
                        Object.Destroy(instance, vfx.Length);
                    }
                }
                else
                {
                    Vector3 pos = self.Player.position + self.Player.rotation * vfx.Offset;
                    Quaternion rot = self.Player.rotation;
                    // 商用项目建议替换为统一的特效系统/对象池（这里先用原生 Instantiate 作为最小可用实现）
                    var instance = Object.Instantiate(vfx.Prefab, pos, rot);
                    if (vfx.Length > 0)
                    {
                        Object.Destroy(instance, vfx.Length);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"AttackComponent: PlayVisualEffect failed - {e.Message}");
            }
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
            List<GameObject> hitTargets = null;
            switch (hitBox.ShapeType)
            {
                case HitShapeType.Box:
                    hitTargets = PhysicsHelper.OverlapBox(worldPosition, hitBox.Size * 0.5f, worldRotation, LayerMask.GetMask("Enemy"));
                    break;
                case HitShapeType.Sphere:
                    hitTargets = PhysicsHelper.OverlapSphere(worldPosition, hitBox.Size.x, LayerMask.GetMask("Enemy"));
                    break;
                case HitShapeType.Fan:
                    hitTargets = PhysicsHelper.OverlapFan(worldPosition, worldRotation * Vector3.forward, hitBox.Size.x, hitBox.Size.y, LayerMask.GetMask("Enemy"));
                    break;
                case HitShapeType.Capsule:
                    hitTargets = PhysicsHelper.OverlapCapsule(worldPosition, hitBox.Size.x, hitBox.Size.y, worldRotation, LayerMask.GetMask("Enemy"));
                    break;
            }

            if (hitTargets == null || hitTargets.Count == 0)
                return;

            // 处理命中目标
            foreach (var target in hitTargets)
            {
                if (target == null)
                    continue;

                // 检查是否已命中过该目标
                if (self.HitTargetsThisSegment.Contains(target))
                    continue;

                // 记录命中
                self.HitTargetsThisSegment.Add(target);
                self.HasHitThisSegment = true;
                self.TotalHitCount++;

                // 处理命中效果（使用 HitBox 的独立效果配置）
                self.ProcessHit(target, hitBox);
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

            // 应用受击反应
            self.ApplyHitReaction(target, effect);

            // 播放命中特效和音效
            self.PlayHitEffects(target, hitBox);

            // 应用顿帧
            int hitStopMs = feedback.HitStopMs;
            if (hitStopMs <= 0)
            {
                hitStopMs = self.Config?.DefaultHitStopMs ?? 0;
            }
            if (hitStopMs > 0)
            {
                self.ApplyHitStop(hitStopMs);
            }

            // 应用屏幕震动
            if (feedback.ScreenShakeIntensity > 0)
            {
                //CameraManager.Instance?.Shake(feedback.ScreenShakeIntensity, feedback.ScreenShakeDuration);
            }

            // 触发命中事件
            self.OnHit?.Invoke(target, self.CurrentSegment);

            Log.Debug($"AttackComponent: Hit target {target.name}, damage: {damage}");
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

        #region 顿帧系统
        
        /// <summary>
        /// 应用顿帧效果
        /// </summary>
        private static void ApplyHitStop(this AttackComponent self, int durationMs)
        {
            if (self.CurrentAnimState == null)
                return;

            // 保存当前速度
            self.HitStopPreviousSpeed = self.CurrentAnimState.Speed;
            
            // 暂停动画
            self.CurrentAnimState.Speed = 0;
            
            // 设置顿帧状态
            self.State = AttackState.HitStop;
            self.HitStopEndTime = TimeInfo.Instance.ClientFrameTime() + durationMs;

            Log.Debug($"AttackComponent: HitStop applied for {durationMs}ms");
        }

        /// <summary>
        /// 更新顿帧状态
        /// </summary>
        private static void UpdateHitStop(this AttackComponent self)
        {
            if (self.State != AttackState.HitStop)
                return;

            long currentTime = TimeInfo.Instance.ClientFrameTime();
            if (currentTime >= self.HitStopEndTime)
            {
                self.EndHitStop();
            }
        }

        /// <summary>
        /// 结束顿帧
        /// </summary>
        private static void EndHitStop(this AttackComponent self)
        {
            if (self.CurrentAnimState != null)
            {
                // 恢复动画速度
                self.CurrentAnimState.Speed = self.HitStopPreviousSpeed;
            }

            self.State = AttackState.Attacking;
            self.HitStopEndTime = 0;

            Log.Debug("AttackComponent: HitStop ended");
        }
        
        #endregion

        #region 位移系统
        
        /// <summary>
        /// 更新攻击位移
        /// </summary>
        private static void UpdateMovement(this AttackComponent self, float fixedDeltaTime)
        {
            if (self.State != AttackState.Attacking)
                return;

            if (self.CurrentSegment?.Movement == null || !self.CurrentSegment.Movement.EnableMovement)
            {
                // 无攻击位移时，关闭外部运动驱动
                if (self.CharacterController != null)
                {
                    self.CharacterController.ExternalMotorActive = false;
                    self.CharacterController.ExternalMotorVelocity = Vector3.zero;
                }
                return;
            }

            var movement = self.CurrentSegment.Movement;
            float normalizedTime = self.CurrentNormalizedTime;

            // 检查是否在位移时间范围内
            if (normalizedTime < movement.NormalizedStart)
            {
                self.IsMovementActive = false;
                if (self.CharacterController != null)
                {
                    self.CharacterController.ExternalMotorActive = false;
                    self.CharacterController.ExternalMotorVelocity = Vector3.zero;
                }
                return;
            }

            if (normalizedTime > movement.NormalizedEnd)
            {
                self.IsMovementActive = false;
                if (self.CharacterController != null)
                {
                    self.CharacterController.ExternalMotorActive = false;
                    self.CharacterController.ExternalMotorVelocity = Vector3.zero;
                }
                return;
            }

            self.IsMovementActive = true;

            // 计算位移进度
            float moveProgress = (normalizedTime - movement.NormalizedStart) / (movement.NormalizedEnd - movement.NormalizedStart);
            moveProgress = Mathf.Clamp01(moveProgress);

            // 应用曲线
            float curveValue = movement.MoveCurve.Evaluate(moveProgress);

            // 计算目标位置
            Vector3 targetPos = Vector3.Lerp(self.MovementStartPosition, self.MovementTargetPosition, curveValue);

            // 通过 CharacterController 外部运动驱动提供 XZ 速度，避免与常规移动覆盖
            if (self.CharacterController?.Rigidbody != null)
            {
                Vector3 currentPos = self.CharacterController.Rigidbody.position;
                Vector3 delta = targetPos - currentPos;
                float dt = Mathf.Max(fixedDeltaTime, 0.0001f);
                Vector3 externalVel = new Vector3(delta.x / dt, 0f, delta.z / dt);
                self.CharacterController.ExternalMotorActive = true;
                self.CharacterController.ExternalMotorVelocity = externalVel;
            }
        }
        
        #endregion

        #region 连击超时
        
        /// <summary>
        /// 重置连击超时定时器
        /// </summary>
        private static void ResetComboTimeout(this AttackComponent self)
        {
            var timerComponent = self.Root().GetComponent<TimerComponent>();
            if (timerComponent == null)
                return;

            // 移除旧定时器
            if (self.ComboTimeoutTimer != 0)
            {
                timerComponent.Remove(ref self.ComboTimeoutTimer);
            }

            // 创建新定时器
            int timeoutMs = self.Config?.ComboTimeoutMs ?? 800;
            long timeoutTime = TimeInfo.Instance.ServerFrameTime() + timeoutMs;
            self.ComboTimeoutTimer = timerComponent.NewOnceTimer(timeoutTime, TimerInvokeType.AttackComboTimeout, self);
        }

        /// <summary>
        /// 连击超时处理（由定时器调用）
        /// </summary>
        public static void OnComboTimeout(this AttackComponent self)
        {
            Log.Debug("AttackComponent: Combo timeout");
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

            int lastIndex = self.CurrentSegmentIndex;

            // 清理定时器
            self.CleanupTimers();

            // 结束顿帧
            if (self.State == AttackState.HitStop)
            {
                self.EndHitStop();
            }

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

            Log.Debug("AttackComponent: Exited attack state");
        }
        
        #endregion

        #region 辅助方法
        
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

            long now = TimeInfo.Instance.ClientFrameTime();
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
    }
}