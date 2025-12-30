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
            if (!self.IsAttacking)
                return;

            self.UpdateHitStop();
            self.UpdateHitDetection();
            self.UpdateMovement();
            self.UpdateAnimationState();
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

            try
            {
                // 加载配置资源
                var configAsset = await ResourcesLoadManager.Instance.LoadAssetAsync<AttackConfigAsset>(self.ConfigPath);
                if (configAsset == null)
                {
                    Log.Error($"AttackComponent: Failed to load config from {self.ConfigPath}");
                    return;
                }

                self.Config = configAsset.Config;
                
                // 预加载所有动画
                await self.PreloadAnimationsAsync();
                
                Log.Info($"AttackComponent: Config loaded successfully, {self.Config.Segments.Count} segments");
            }
            catch (Exception e)
            {
                Log.Error($"AttackComponent: LoadConfigAsync failed - {e}");
            }
        }

        /// <summary>
        /// 预加载动画资源
        /// </summary>
        private static async ETTask PreloadAnimationsAsync(this AttackComponent self)
        {
            if (self.Config == null)
                return;

            var loadTasks = new List<ETTask>();
            
            foreach (var segment in self.Config.Segments)
            {
                /*if (string.IsNullOrEmpty(segment.AnimationPath))
                    continue;*/

                var task = self.LoadSegmentAnimationAsync(segment);
                loadTasks.Add(task);
            }

            await ETTaskHelper.WaitAll(loadTasks);
        }

        /// <summary>
        /// 加载单个攻击段动画
        /// </summary>
        private static async ETTask LoadSegmentAnimationAsync(this AttackComponent self, AttackSegmentData segment)
        {
            try
            {
                var animationSetAsset = await ResourcesLoadManager.Instance.LoadAssetAsync<AnimationClipAsset>("PlayerAttack");
                if (animationSetAsset != null)
                {
                    var transitions = animationSetAsset.GetAllTransitions();
                    for (int i = 0; i < transitions.Length && i < 4; i++)
                    {
                        //self.AttackTransitions[i] = transitions[i];
                    }
                    //segment.Transition = transition;
                    segment.IsLoaded = true;
                }
            }
            catch (Exception e)
            {
                Log.Error($"AttackComponent: LoadSegmentAnimationAsync failed for segment {segment.Id} - {e}");
            }
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
            self.PendingSegmentIndex = -1;
            self.PendingInputType = ComboInputType.None;
            self.ComboCount = 0;
            self.HasBufferedInput = false;
            self.BufferedInputType = ComboInputType.None;
            self.HasHitThisSegment = false;
            self.HitTargetsThisSegment.Clear();
            self.TotalHitCount = 0;
            self.IsMovementActive = false;
            self.TrackTarget = null;
            self.HitStopEndTime = 0;
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
            if (!self.IsAttacking)
            {
                return self.StartAttack(0, inputType);
            }

            // 如果在顿帧中，缓存输入
            if (self.IsInHitStop)
            {
                self.BufferInput(inputType);
                return true;
            }

            // 检查是否可以输入缓冲
            if (self.CanBufferInput)
            {
                // 尝试获取下一段攻击
                int nextIndex = self.GetNextSegmentIndex(inputType);
                if (nextIndex >= 0)
                {
                    // 检查当前动画是否已结束
                    if (self.IsCurrentAnimationEnded())
                    {
                        // 立即播放下一段
                        return self.StartAttack(nextIndex, inputType);
                    }
                    else
                    {
                        // 缓存输入，等待当前动画结束
                        self.BufferInput(inputType);
                        self.PendingSegmentIndex = nextIndex;
                        self.PendingInputType = inputType;
                        self.ResetComboTimeout();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 缓存输入
        /// </summary>
        private static void BufferInput(this AttackComponent self, ComboInputType inputType)
        {
            self.HasBufferedInput = true;
            self.BufferedInputType = inputType;
        }

        /// <summary>
        /// 消费缓冲输入
        /// </summary>
        private static bool ConsumeBufferedInput(this AttackComponent self)
        {
            if (!self.HasBufferedInput)
                return false;

            var inputType = self.BufferedInputType;
            self.HasBufferedInput = false;
            self.BufferedInputType = ComboInputType.None;

            int nextIndex = self.GetNextSegmentIndex(inputType);
            if (nextIndex >= 0)
            {
                return self.StartAttack(nextIndex, inputType);
            }

            return false;
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
                var branchSegment = self.Config.GetSegmentById(branchId);
                if (branchSegment != null && branchSegment.IsLoaded)
                {
                    return self.Config.Segments.IndexOf(branchSegment);
                }
            }

            // 默认线性连击
            int nextIndex = self.CurrentSegmentIndex + 1;
            if (nextIndex < self.Config.Segments.Count)
            {
                var nextSegment = self.Config.Segments[nextIndex];
                if (nextSegment.IsLoaded)
                {
                    return nextIndex;
                }
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
            if (segment == null || !segment.IsLoaded)
            {
                Log.Warning($"AttackComponent: Segment {segmentIndex} not loaded");
                return false;
            }

            if (self.AnimatorComponent == null)
            {
                return false;
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

            // 设置动画速度
            animState.Speed = segment.AnimationSpeed;

            // 更新状态
            self.CurrentAnimState = animState;
            self.CurrentSegmentIndex = segmentIndex;
            self.CurrentSegment = segment;
            self.State = AttackState.Attacking;
            self.PendingSegmentIndex = -1;
            self.PendingInputType = ComboInputType.None;
            self.HasBufferedInput = false;
            self.BufferedInputType = ComboInputType.None;

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
            if (segment.Effect == null)
                return;

            var unit = self.GetParent<Unit>();
            if (unit == null)
                return;

            // 播放攻击特效
            if (!string.IsNullOrEmpty(segment.Effect.AttackEffectPath))
            {
                //EffectManager.Instance?.PlayEffect(segment.Effect.AttackEffectPath, unit.Position, unit.Rotation);
            }

            // 播放攻击音效
            if (!string.IsNullOrEmpty(segment.Effect.AttackSoundPath))
            {
                //AudioManager.Instance?.PlaySound(segment.Effect.AttackSoundPath);
            }
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

            // 检查是否超过结束时间点
            return self.CurrentAnimState.NormalizedTime >= self.CurrentSegment.EndTime;
        }

        /// <summary>
        /// 当前动画结束处理
        /// </summary>
        private static void OnCurrentAnimationEnd(this AttackComponent self)
        {
            int completedIndex = self.CurrentSegmentIndex;

            // 触发攻击结束事件
            self.OnAttackEnd?.Invoke(completedIndex);

            // 检查是否有待播放的下一段
            if (self.PendingSegmentIndex >= 0)
            {
                int nextIndex = self.PendingSegmentIndex;
                var nextInputType = self.PendingInputType;
                self.PendingSegmentIndex = -1;
                self.PendingInputType = ComboInputType.None;
                self.StartAttack(nextIndex, nextInputType);
                return;
            }

            // 检查是否有缓冲输入
            if (self.ConsumeBufferedInput())
            {
                return;
            }

            // 没有后续攻击，进入后摇状态
            self.State = AttackState.Recovery;
            
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

            float normalizedTime = self.CurrentNormalizedTime;

            foreach (var hitBox in self.CurrentSegment.HitBoxes)
            {
                // 更新判定框激活状态
                if (!hitBox.IsCompleted)
                {
                    if (!hitBox.IsActive && normalizedTime >= hitBox.StartTime)
                    {
                        hitBox.IsActive = true;
                    }
                    else if (hitBox.IsActive && normalizedTime >= hitBox.EndTime)
                    {
                        hitBox.IsActive = false;
                        hitBox.IsCompleted = true;
                    }
                }

                // 执行命中检测
                if (hitBox.IsActive)
                {
                    self.PerformHitDetection(hitBox);
                }
            }
        }

        /// <summary>
        /// 执行命中检测
        /// </summary>
        private static void PerformHitDetection(this AttackComponent self, HitBoxData hitBox)
        {
            // 计算判定框世界坐标
            Vector3 worldPosition = self.Player.position + self.Player.rotation * hitBox.Offset;
            Quaternion worldRotation = self.Player.rotation;

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
                    hitTargets = PhysicsHelper.OverlapFan(worldPosition, self.Player.forward, hitBox.Size.x, hitBox.Size.y, LayerMask.GetMask("Enemy"));
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

                // 处理命中效果
                self.ProcessHit(target, self.CurrentSegment);
            }
        }

        /// <summary>
        /// TODO 处理命中效果
        /// </summary>
        private static void ProcessHit(this AttackComponent self, GameObject target, AttackSegmentData segment)
        {
            // 计算伤害
            float damage = self.CalculateDamage(target, segment);

            //TODO 应用伤害 
            /*var targetHealth = target.GetComponent<HealthComponent>();
            targetHealth?.TakeDamage(damage, attacker);*/

            // 应用受击反应
            self.ApplyHitReaction(target, segment);

            // 播放命中特效和音效
            self.PlayHitEffects(target, segment);

            // 应用顿帧
            if (segment.Effect != null && segment.Effect.HitStopMs > 0)
            {
                self.ApplyHitStop(segment.Effect.HitStopMs);
            }

            // 应用屏幕震动
            if (segment.Effect != null && segment.Effect.ScreenShakeIntensity > 0)
            {
                //CameraManager.Instance?.Shake(segment.Effect.ScreenShakeIntensity, segment.Effect.ScreenShakeDuration);
            }

            // 触发命中事件
            self.OnHit?.Invoke(target, segment);

            Log.Debug($"AttackComponent: Hit target {target.name}, damage: {damage}");
        }

        /// <summary>
        /// TODO 计算伤害
        /// </summary>
        private static float CalculateDamage(this AttackComponent self, GameObject target, AttackSegmentData segment)
        {
            // 获取攻击者属性
            /*var attackerAttr = attacker.GetComponent<AttributeComponent>();
            float baseAttack = attackerAttr?.GetAttribute(AttributeType.Attack) ?? 100f;

            // 获取目标防御
            var targetAttr = target.GetComponent<AttributeComponent>();
            float defense = targetAttr?.GetAttribute(AttributeType.Defense) ?? 0f;

            // 计算最终伤害
            float damage = baseAttack * segment.DamageMultiplier;
            damage = Mathf.Max(1, damage - defense);*/
            float damage = 100;
            return damage;
        }

        /// <summary>
        /// 应用受击反应
        /// </summary>
        private static void ApplyHitReaction(this AttackComponent self, GameObject target, AttackSegmentData segment)
        {
            var hitReactionComponent = target.GetComponent<HitReactionComponent>();
            if (hitReactionComponent == null)
                return;

            var attacker = self.GetParent<Unit>();
            Vector3 hitDirection = (target.transform.position - self.Player.position).normalized;
            hitDirection.y = 0;

            switch (segment.HitReaction)
            {
                case HitReactionType.Light:
                    hitReactionComponent.PlayLightHit(hitDirection, segment.HitStunMs);
                    break;
                case HitReactionType.Medium:
                    hitReactionComponent.PlayMediumHit(hitDirection, segment.HitStunMs);
                    break;
                case HitReactionType.Heavy:
                    hitReactionComponent.PlayHeavyHit(hitDirection, segment.HitStunMs);
                    break;
                case HitReactionType.Knockback:
                    hitReactionComponent.PlayKnockback(hitDirection, segment.KnockbackForce, segment.HitStunMs);
                    break;
                case HitReactionType.Knockup:
                    hitReactionComponent.PlayKnockup(segment.KnockupForce, segment.HitStunMs);
                    break;
                case HitReactionType.Knockdown:
                    hitReactionComponent.PlayKnockdown(hitDirection, segment.KnockbackForce, segment.HitStunMs);
                    break;
            }
        }

        /// <summary>
        /// TODO 播放命中特效和音效
        /// </summary>
        private static void PlayHitEffects(this AttackComponent self, GameObject target, AttackSegmentData segment)
        {
            if (segment.Effect == null)
                return;

            // 播放命中特效
            if (!string.IsNullOrEmpty(segment.Effect.HitEffectPath))
            {
                Vector3 hitPosition = target.transform.position + Vector3.up * 1f; // 命中点偏移
                //EffectManager.Instance?.PlayEffect(segment.Effect.HitEffectPath, hitPosition, Quaternion.identity);
            }

            // 播放命中音效
            if (!string.IsNullOrEmpty(segment.Effect.HitSoundPath))
            {
                //AudioManager.Instance?.PlaySound(segment.Effect.HitSoundPath);
            }
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
        private static void UpdateMovement(this AttackComponent self)
        {
            if (self.State != AttackState.Attacking)
                return;

            if (self.CurrentSegment?.Movement == null || !self.CurrentSegment.Movement.EnableMovement)
                return;

            var movement = self.CurrentSegment.Movement;
            float normalizedTime = self.CurrentNormalizedTime;

            // 检查是否在位移时间范围内
            if (normalizedTime < movement.StartTime)
            {
                self.IsMovementActive = false;
                return;
            }

            if (normalizedTime > movement.EndTime)
            {
                self.IsMovementActive = false;
                return;
            }

            self.IsMovementActive = true;

            // 计算位移进度
            float moveProgress = (normalizedTime - movement.StartTime) / (movement.EndTime - movement.StartTime);
            moveProgress = Mathf.Clamp01(moveProgress);

            // 应用曲线
            float curveValue = movement.MoveCurve.Evaluate(moveProgress);

            // 计算目标位置
            Vector3 targetPos = Vector3.Lerp(self.MovementStartPosition, self.MovementTargetPosition, curveValue);

            // TODO 移动角色,要和角色控制关联 CharacterControllerComponentSystem
            /*var unit = self.GetParent<Unit>();
            if (unit != null)
            {
                var moveComponent = unit.GetComponent<MoveComponent>();
                if (moveComponent != null)
                {
                    moveComponent.SetPosition(targetPos);
                }
                else
                {
                    unit.Position = targetPos;
                }
            }*/
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
            long timeoutTime = TimeInfo.Instance.ClientFrameTime() + timeoutMs;
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
            if (lastIndex >= 0)
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
    }
}