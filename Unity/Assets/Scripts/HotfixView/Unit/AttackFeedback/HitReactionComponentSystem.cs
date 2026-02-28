using System;
using Animancer;
using UnityEngine;

namespace ET
{
    public static partial class HitReactionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HitReactionComponent self, Transform player)
        {
            self.Owner = player;
            self.OwnerUnit = self.GetParent<Unit>();
            self.InitComponentRefs(self.OwnerUnit);
        }

        [EntitySystem]
        private static void Destroy(this HitReactionComponent self)
        {
            // 兆底：确保会话级别的外部状态全部释放
            self.ReleaseHitSessionLocksIfNeeded();
            self.ReleaseGroundFrequencyInhibitIfNeeded();
            
            self.CurrentHitState = HitState.None;
            self.VisualState = HitState.None;
            self.VisualReactionType = HitReactionType.None;
            self.CurrentAnimEnd = false;
            self.OnHitReactionStart = null;
            self.OnHitReactionEnd = null;
        }

        [EntitySystem]
        private static void Update(this HitReactionComponent self)
        {
            if (!self.IsInHitReaction)
                return;
            
            // 无论视觉播什么动画，只要物理轨道有数据（Speed > 0），角色就会产生位移
            self.UpdateHitMotion();
            
            // 仅处理动画计时、落地判定和状态流转
            switch (self.CurrentHitState)
            {
                case HitState.GroundedHit:
                    self.UpdateGroundedHitState();
                    break;
                case HitState.AirborneHit:
                    self.UpdateAirborne();
                    break;
                case HitState.KnockdownHit:
                    self.UpdateKnockdown();
                    break;
                case HitState.GetUpHit:
                    self.UpdateGetUp();
                    break;
            }
        }

        private static void UpdateHitMotion(this HitReactionComponent self)
        {
            if (self.CurrentMotionType == HitMotionType.None || self.MotionEndTime <= 0)
            {
                return;
            }

            // NormalHit：距离维持路径（弹簧模型）
            /*if (self.CurrentMotionType == HitMotionType.NormalHit)
            {
                self.UpdateNormalHitMotion();
                return;
            }*/

            // ---- 非 NormalHit：Force/Curve 路径（无拴系约束）----
            long now = self.GetCombatNowMs();
            long durationMs = self.MotionEndTime - self.MotionStartTime;
            // 检查运动是否结束
            if (durationMs <= 0 || now >= self.MotionEndTime)
            {
                self.ClearPhysicalMotion();
                return;
            }

            // 基于曲线采样计算当前速度
            float t = Mathf.Clamp01((float)(now - self.MotionStartTime) / durationMs);
            float curveValue = self.CurrentMotionCurve != null ? self.CurrentMotionCurve.Evaluate(t) : (1f - t);
            self.CurrentMotionSpeed = self.MotionBaseForce * curveValue;

            var intent = self.LocomotionIntent;
            if (intent != null)
            {
                if (self.CurrentMotionSpeed > 0.001f)
                {
                    Vector3 horizontalVel = new Vector3(
                        self.MotionDirection.x * self.CurrentMotionSpeed, 0f,
                        self.MotionDirection.z * self.CurrentMotionSpeed);

                    intent.ExternalTargetVelocity = new Vector2(horizontalVel.x, horizontalVel.z);
                }
                else
                {
                    intent.ExternalTargetVelocity = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// NormalHit 距离维持：每帧根据攻击者-受击者水平距离，用弹簧模型计算速度，
        /// 保持受击者在 [AnchorPointDistance, MaxDistance] 区间内。
        /// </summary>
        private static void UpdateNormalHitMotion(this HitReactionComponent self)
        {
            long now = self.GetCombatNowMs();
            if (now >= self.MotionEndTime)
            {
                self.ClearPhysicalMotion();
                return;
            }

            var intent = self.LocomotionIntent;
            if (intent == null)
                return;

            // 实时更新锚点：跟踪攻击者 RootMotion 位移
            if (self.TetherAnchorTransform != null)
            {
                self.TetherAnchorPos = self.TetherAnchorTransform.position;
            }

            var profile = self.CombatConfig?.CachedTether;
            if (profile == null || !profile.Value.Enable)
            {
                intent.ExternalTargetVelocity = Vector2.zero;
                self.CurrentMotionSpeed = 0f;
                return;
            }

            var tether = profile.Value;

            // 计算水平距离
            Vector3 offset = self.Owner.position - self.TetherAnchorPos;
            offset.y = 0f;
            float dist = offset.magnitude;

            // 方向：受击者相对于攻击者的方向（远离方向）
            Vector3 awayDir = dist > 0.01f ? offset / dist : self.MotionDirection;

            // 目标距离：钳位到 [AnchorPointDistance, MaxDistance]
            float targetDist = Mathf.Clamp(dist, tether.AnchorPointDistance, tether.MaxDistance);
            float error = targetDist - dist; // + = 推离（距离不够），- = 拉回（距离过远）

            if (Mathf.Abs(error) < 0.01f)
            {
                // 已在最优区间
                intent.ExternalTargetVelocity = Vector2.zero;
                self.CurrentMotionSpeed = 0f;
                return;
            }

            // 弹簧模型（P 控制器）：速度 = 误差 * 刚度，自然减速趋近目标
            float speed = error * tether.RepositionSpeed;
            Vector3 vel = awayDir * speed;

            // 速度上限
            if (tether.MaxHorizontalSpeed > 0f)
            {
                float magnitude = vel.magnitude;
                if (magnitude > tether.MaxHorizontalSpeed)
                {
                    vel = vel / magnitude * tether.MaxHorizontalSpeed;
                }
            }

            self.CurrentMotionSpeed = vel.magnitude;
            intent.ExternalTargetVelocity = new Vector2(vel.x, vel.z);
        }

        private static void UpdateGroundedHitState(this HitReactionComponent self)
        {
            // 检查硬直结束 && 物理位移停止
            if (self.GetCombatNowMs() >= self.HitStunEndTimeMs && self.CurrentMotionSpeed < 0.1f && self.CurrentAnimEnd)
            {
                self.EndHitReaction();
            }
        }

        /// <summary>
        /// 统一受击入口
        /// - Resistance 按状态切表（Grounded/Airborne/AirStun/Knockdown/GetUp）
        /// </summary>
        public static bool TryApplyHit(this HitReactionComponent self, in HitImpactData hitReaction)
        {
            if (self == null || self.IsDisposed || self.Owner == null || self.OwnerUnit == null)
            {
                return false;
            }
            // 缓存本单位的视觉许可（用于帧内/帧间状态流转时同步视觉状态）
            self.AllowedReactionGroups = self.CombatConfig.HitReactionConfig.Visual.AllowedReactionGroups;
            self.AllowedStateVisuals = self.CombatConfig.HitReactionConfig.Visual.AllowedStateVisuals;
            self.LocomotionIntent.FaceDirection = -hitReaction.Rule.HitDirection;

            bool hasVisualOrPhysical = hitReaction.Rule.ReactionType != HitReactionType.None || hitReaction.Rule.MotionData.MotionType != HitMotionType.None;
            bool hasAnyFeedback =
                hitReaction.Feedback.VictimHitStopMs > 0 ||
                hitReaction.Feedback.ScreenShakeIntensity > 0f ||
                hitReaction.Feedback.ScreenShakeDurationMs > 0 ||
                (hitReaction.Feedback.TimeScale > 0f && !Mathf.Approximately(hitReaction.Feedback.TimeScale, 1f)) ||
                hitReaction.Feedback.TimeScaleDurationMs > 0;
            
            if (!hasVisualOrPhysical)
            {
                if (!hasAnyFeedback)
                {
                    return false;
                }
                // 反馈-only：只按反馈 profile 应用，不进入受击会话
                self.ApplyFeedback(in hitReaction, in self.CombatConfig.Feedback);
                return true;
            }

            if (!self.PassTargetStateFilter(hitReaction.Rule.TargetStates))
                return false;

            // 归一化参数（应用 Scale 和 Limit）
            HitImpactData normalized = self.Normalize(hitReaction, in self.CombatConfig.HitReactionConfig);

            // GetUp：起身中，门槛不够直接拒绝（不进入受击会话，也不触发反馈）
            if (self.CurrentHitState == HitState.GetUpHit) 
            {
                var getUpRes = self.CombatConfig.HitReactionConfig.Rule.HitInterrupt.GetUp;
                if (normalized.Rule.HitStrength < getUpRes.GetUpInterruptThreshold)
                {
                    return false;
                }
            }

            // Knockdown：倒地中，门槛不够直接拒绝（避免低强度命中消耗反馈资源）
            if (self.CurrentHitState == HitState.KnockdownHit)
            {
                var knockdownRes = self.CombatConfig.HitReactionConfig.Rule.HitInterrupt.Knockdown;
                if (normalized.Rule.HitStrength < knockdownRes.KnockdownThreshold)
                {
                    return false;
                }
            }

            // 受击会话：进入会话才 acquire 外部能力锁（规则生效，与是否播放动画无关）
            self.AcquireHitSessionLocksIfNeeded();

            try
            {
                // 应用受击
                self.ApplyHit(in normalized, in self.CombatConfig.HitReactionConfig);
            
                // 规则生效后，才做“表现许可/降级/禁播”
                self.ApplyVisualOutcome(in normalized);

                // 反馈：默认仍按反馈 profile 执行（可后续进一步纳入 VisualOutcome 细分）
                self.ApplyFeedback(in normalized, in self.CombatConfig.Feedback);
                return true;
            }
            catch (Exception e)
            {
                // 异常时确保释放引用计数，防止角色永久卡死
                Log.Error($"[HitReaction] TryApplyHit 异常，释放锁: {e}");
                self.ReleaseHitSessionLocksIfNeeded();
                throw;
            }
        }

        private static void ApplyHit(this HitReactionComponent self, in HitImpactData request, in HitReactionConfig reactionConfig)
        {
            // 统一更新“当前受击表现类型”和“硬直截止点”
            self.CurrentReactionType = request.Rule.ReactionType;
            long now = self.GetCombatNowMs();
            self.HitStunEndTimeMs = now + request.Rule.HitStunMs + request.Rule.AttackerSegmentTimeoutMs;
            self.FirstUpForce = request.Rule.MotionData.Force;
            switch (self.CurrentHitState)
            {
                case HitState.None:
                case HitState.GroundedHit:
                    self.HandleGroundedHit(in request, in reactionConfig);
                    return;
                case HitState.AirborneHit:
                    self.HandleAirborneHit(in request, in reactionConfig);
                    return;
                case HitState.KnockdownHit:
                    self.HandleKnockdownHit(in request, in reactionConfig);
                    return;
                case HitState.GetUpHit:
                    self.HandleGetUpHit(in request, in reactionConfig);
                    return;
            }
        }

        /// <summary>
        /// 切换受击状态并通知外部。
        /// 注意：同状态调用也会触发事件，以支持连续受击刷新动画（如地面连续受击、倒地续击）。
        /// CurrentMotionType 由 InitPhysicalMotion 统一管理，SwitchState 不负责物理字段。
        /// </summary>
        private static void SwitchState(this HitReactionComponent self, HitState next)
        {
            self.CurrentHitState = next;
            self.UpdateVisualStateForCurrentState();
            self.OnHitReactionStart?.Invoke(next);
            Log.Debug($"[HitReaction] SwitchState → {self.CurrentHitState}");
        }

        private static void UpdateVisualStateForCurrentState(this HitReactionComponent self)
        {
            // VisualState 仅受 AllowedStateVisuals 控制，不影响规则 CurrentState。
            if (self == null)
            {
                return;
            }

            HitState state = self.CurrentHitState;
            if (!IsStateVisualAllowed(self.AllowedStateVisuals, state))
            {
                self.VisualState = HitState.None;
                // 视觉禁播时也清掉地面 ReactionType，避免误触发
                self.VisualReactionType = HitReactionType.None;
                return;
            }

            self.VisualState = state;
            // VisualReactionType 由 ApplyVisualOutcome 负责降级；这里只保证状态一致性
        }

        private static bool IsStateVisualAllowed(HitStateVisualMask allowed, HitState state)
        {
            HitStateVisualMask flag = state switch
            {
                HitState.GroundedHit => HitStateVisualMask.Grounded,
                HitState.AirborneHit => HitStateVisualMask.Airborne,
                HitState.KnockdownHit => HitStateVisualMask.Knockdown,
                HitState.GetUpHit => HitStateVisualMask.GetUp,
                _ => HitStateVisualMask.None
            };
            return flag != HitStateVisualMask.None && (allowed & flag) != 0;
        }

        private static void ApplyVisualOutcome(this HitReactionComponent self, in HitImpactData request)
        {
            if (self == null)
            {
                return;
            }

            // 1) 状态动画许可：不允许则完全不播受击动画（但规则仍生效）
            self.UpdateVisualStateForCurrentState();
            if (self.VisualState == HitState.None)
            {
                return;
            }

            // 2) ReactionType 许可（仅 Grounded 使用；空中/倒地/起身等以状态动画为主）
            HitReactionType desired = request.Rule.ReactionType;
            HitReactionType visualType = self.DegradeReactionType(desired, self.AllowedReactionGroups);
            self.VisualReactionType = visualType;
        }

        /// <summary>
        /// 地面受击路由：仅根据 MotionType 决定反应类型，消除 HitStrength 双重门控。
        /// 每个 MotionType 分支都保证调用 SwitchState，不存在穿透路径。
        /// </summary>
        private static void HandleGroundedHit(this HitReactionComponent self, in HitImpactData request, in HitReactionConfig reactionConfig)
        {
            switch (request.Rule.MotionData.MotionType)
            {
                case HitMotionType.UpwardImpulse:
                    // 击飞：地面 → 空中
                    self.EnterAirborneFromGrounded(in request);
                    self.SwitchState(HitState.AirborneHit);
                    break;

                case HitMotionType.DownwardImpulse:
                    // 砸地：直接倒地
                    self.InitPhysicalMotion(in request);
                    self.SwitchState(HitState.KnockdownHit);
                    self.ApplyImpulse(HitMotionType.DownwardImpulse, request.Rule.MotionData.Force);
                    self.KnockdownEndTime = self.GetCombatNowMs() + self.KnockdownDurationMs;
                    break;

                case HitMotionType.NormalHit:
                    // 普通受击：距离维持路径（由 UpdateNormalHitMotion 弹簧模型驱动）
                    self.InitPhysicalMotion(in request);
                    self.SwitchState(HitState.GroundedHit);
                    break;

                case HitMotionType.HorizontalImpulse:
                case HitMotionType.TowardAttacker:
                    // 水平击退 / 拉拽（Force/Curve 驱动，无拴系）
                    self.InitPhysicalMotion(in request);
                    self.SwitchState(HitState.GroundedHit);
                    break;

                case HitMotionType.CustomCurve:
                    // 自定义曲线运动
                    self.InitPhysicalMotion(in request);
                    self.SwitchState(HitState.GroundedHit);
                    break;

                case HitMotionType.None:
                default:
                    // 纯硬直（无位移）：仍需进入 GroundedHit 状态以启动硬直计时，
                    // UpdateGroundedHitState 会在 HitStunEndTimeMs 到期时调用 EndHitReaction 释放 Inhibitors
                    self.SwitchState(HitState.GroundedHit);
                    break;
            }
        }

        private static void HandleAirborneHit(this HitReactionComponent self, in HitImpactData request, in HitReactionConfig reactionConfig)
        {
            self.InitPhysicalMotion(in request);
            switch (request.Rule.MotionData.MotionType)
            {
                case HitMotionType.NormalHit:
                case HitMotionType.HorizontalImpulse:
                case HitMotionType.TowardAttacker:
                case HitMotionType.CustomCurve:
                    self.ApplyImpulse(HitMotionType.UpwardImpulse, self.FirstUpForce);
                    break;
                case HitMotionType.UpwardImpulse:
                    self.ApplyImpulse(HitMotionType.UpwardImpulse, request.Rule.MotionData.Force);
                    break;
                case HitMotionType.DownwardImpulse:
                    // 空中终结判定：DownwardImpulse（砸地）触发空中终结
                    self.EnterAirFinish(in request);
                    return;
            }
            
            // 击飞型：使用自身 Force 作为向上冲量
            if (self.Ground != null)
            {
                self.Ground.ForceBreakGround(AirborneReason.Juggled);
            }
                
            float originY = self.Owner.position.y;
            float maxOffset = self.CombatConfig.CachedAirCombo.MaxHeightOffset;
            float candidateCeiling = originY + (maxOffset > 0f ? maxOffset : 2.2f);
            float absoluteMaxHeight = self.CombatConfig.CachedAirCombo.AbsoluteMaxHeight;
            if (absoluteMaxHeight > 0f)
            {
                float absoluteCeiling = self.AirborneOriginHeight + absoluteMaxHeight;
                candidateCeiling = Mathf.Min(candidateCeiling, absoluteCeiling);
            }
            self.MaxAirborneHeight = candidateCeiling;

            // AirCombo：命中续期
            if (self.AirCombo != null && self.AirCombo.Active)
            {
                self.AirCombo.OnHit(
                    self.HitStunEndTimeMs,
                    in self.CombatConfig.CachedAirCombo,
                    request.Rule.AttackRadius);
            }

            // 非 NormalHit：击退方向偏向拴系锚点（攻击者位置），防止越打越远
            // NormalHit 不需要方向偏移——水平位移完全由 UpdateNormalHitMotion 弹簧控制
            if (request.Rule.MotionData.MotionType != HitMotionType.NormalHit)
            {
                Vector3 toAnchor = self.TetherAnchorPos - self.Owner.position;
                toAnchor.y = 0f;
                if (toAnchor.sqrMagnitude > 0.01f)
                {
                    self.MotionDirection = Vector3.Lerp(
                        self.MotionDirection, toAnchor.normalized, 0.3f);
                }
            }

            // 空中命中通知外部刷新动画
            self.OnHitReactionStart?.Invoke(self.CurrentHitState);
        }

        private static void HandleKnockdownHit(this HitReactionComponent self, in HitImpactData request, in HitReactionConfig reactionConfig)
        {
            // TryApplyHit 已做 KnockdownThreshold 前置检查，此处只需续倒地时间
            // 倒地续击不产生新运动，但必须清除残留运动（例如倒地前被击退的水平滑行仍在进行）
            self.ClearPhysicalMotion();
            self.KnockdownEndTime = self.GetCombatNowMs() + self.KnockdownDurationMs;
            // 通知外部（如动画）刷新倒地表现
            self.OnHitReactionStart?.Invoke(HitState.KnockdownHit);
        }

        private static void HandleGetUpHit(this HitReactionComponent self, in HitImpactData request, in HitReactionConfig reactionConfig)
        {
            // TryApplyHit 已做 GetUpInterruptThreshold 前置检查，此处无需重复
            // 打断起身：重置为 None 以避免中间态事件，然后由 HandleGroundedHit 评估最终状态并触发一次正确的事件
            self.CurrentHitState = HitState.None;
            self.HandleGroundedHit(in request, in reactionConfig);
        }

        
        private static void EnterAirFinish(this HitReactionComponent self, in HitImpactData request)
        {
            // 进入终结态：强制结束 AirCombo
            self.AirCombo.BeginExit(self.GetCombatNowMs());
            self.AcquireGroundFrequencyInhibitIfNeeded();

            // 根据击飞类型设置不同的落地语义
            switch (request.Rule.MotionData.MotionType)
            {
                case HitMotionType.DownwardImpulse:
                case HitMotionType.HorizontalImpulse:
                case HitMotionType.TowardAttacker:
                case HitMotionType.CustomCurve:
                    if (self.Ground != null)
                        self.Ground.StateContext.AirborneReason = AirborneReason.Knockdown;
                    break;
                case HitMotionType.UpwardImpulse:
                    break;
                default:
                    if (self.Ground != null)
                        self.Ground.StateContext.AirborneReason = AirborneReason.Knockdown;
                    break;
            }

            self.ApplyImpulse(request.Rule.MotionData.MotionType, request.Rule.MotionData.Force);
            self.SwitchState(HitState.AirborneHit);
        }

        private static void EnterAirborneFromGrounded(this HitReactionComponent self, in HitImpactData request)
        {
            // 记录击飞起始高度，用于全局空中高度上限
            float originY = self.Owner.position.y;
            self.AirborneOriginHeight = originY;
            float maxOffset = self.CombatConfig.CachedAirCombo.MaxHeightOffset;
            self.MaxAirborneHeight = originY + (maxOffset > 0f ? maxOffset : 2.2f);

            if (self.Ground != null)
            {
                var type = ResolveAirborneReasonForRequest(in request);
                self.Ground.ForceBreakGround(type);
            }

            self.FirstUpForce = request.Rule.MotionData.Force;
            self.AcquireGroundFrequencyInhibitIfNeeded();
            self.InitPhysicalMotion(in request);
            self.ApplyImpulse(request.Rule.MotionData.MotionType, request.Rule.MotionData.Force);
            // 连段中心：优先使用攻击者位置
            Vector3 comboCenterPos = self.TetherAnchorTransform.position;
            self.AirCombo.Enter(
                self.HitStunEndTimeMs, 
                comboCenterPos, 
                in self.CombatConfig.CachedAirCombo,
                request.Rule.AttackRadius);
        }

        #region 统一入口

        /// <summary>
        /// 受击会话：外部能力锁 acquire（一次会话只允许 acquire 一次）。
        /// </summary>
        private static void AcquireHitSessionLocksIfNeeded(this HitReactionComponent self)
        {
            if (self == null)
            {
                return;
            }

            if (self.HitSessionLocksAcquired)
            {
                return;
            }
            self.HitSessionLocksAcquired = true;

            var intent = self.LocomotionIntent;
            if (intent != null)
            {
                // 增加屏蔽计数（多源锁定）
                intent.MoveInhibitors++;
                intent.RotateInhibitors++;
                intent.AttackInhibitors++;
                intent.JumpInhibitors++;
                intent.JumpRequested = false;
            }

            if (self.CancelAttackOnHit)
            {
                self.Attack?.AttackCommand?.Clear();
                self.Attack?.ForceCancel();
            }
        }

        /// <summary>
        /// 受击会话：外部能力锁 release（一次会话只允许 release 一次）。
        /// </summary>
        private static void ReleaseHitSessionLocksIfNeeded(this HitReactionComponent self)
        {
            if (self == null)
            {
                return;
            }

            if (!self.HitSessionLocksAcquired)
            {
                return;
            }
            self.HitSessionLocksAcquired = false;

            var intent = self.LocomotionIntent;
            if (intent == null)
            {
                return;
            }

            intent.MoveInhibitors--;
            intent.RotateInhibitors--;
            intent.AttackInhibitors--;
            intent.JumpInhibitors--;

            if (intent.MoveInhibitors < 0) intent.MoveInhibitors = 0;
            if (intent.RotateInhibitors < 0) intent.RotateInhibitors = 0;
            if (intent.AttackInhibitors < 0) intent.AttackInhibitors = 0;
            if (intent.JumpInhibitors < 0) intent.JumpInhibitors = 0;
        }

        private static void ApplyFeedback(this HitReactionComponent self, in HitImpactData request, in HitFeedbackConfig config)
        {
            if (config.Option.AllowVictimHitStop && request.Feedback.VictimHitStopMs > 0)
            {
                int ms = Mathf.RoundToInt(request.Feedback.VictimHitStopMs * Mathf.Max(0f, config.Option.VictimHitStopScale));
              
                if (self.HitStop != null && self.HitStop.Animancer != null)
                {
                    self.HitStop.RequestHitStop(ms, self.HitStop.Animancer, HitStopFreezeMode.FreezeAll);
                }
            }
        }

        /// <summary>
        /// 停止当前水平物理运动并清零 LocomotionIntent 上的残留速度。
        /// 任何将 CurrentMotionType 设为 None 的地方都必须调用此方法，避免速度残留导致滑行。
        /// </summary>
        private static void ClearPhysicalMotion(this HitReactionComponent self)
        {
            self.CurrentMotionType = HitMotionType.None;
            self.CurrentMotionSpeed = 0f;
            self.MotionBaseForce = 0f;
            self.MotionStartTime = 0;
            self.MotionEndTime = 0;
            self.CurrentMotionCurve = null;
            
            if (self.LocomotionIntent != null)
            {
                self.LocomotionIntent.ExternalTargetVelocity = Vector2.zero;
            }
        }

        // 旧 ApplyHitTether 已移除：NormalHit 使用 UpdateNormalHitMotion 弹簧模型；
        // 非 NormalHit 纯 Force/Curve 驱动，无拴系约束。

        /// <summary>
        /// 进入空中受击时抵制地检降频，确保每帧检测落地。
        /// </summary>
        private static void AcquireGroundFrequencyInhibitIfNeeded(this HitReactionComponent self)
        {
            if (self.GroundFrequencyInhibited) return;
            if (self.Ground == null) return;
            self.GroundFrequencyInhibited = true;
            self.Ground.InhibitReduceFrequencyCount++;
        }

        /// <summary>
        /// 离开空中受击时释放地检降频抑制。
        /// </summary>
        private static void ReleaseGroundFrequencyInhibitIfNeeded(this HitReactionComponent self)
        {
            if (!self.GroundFrequencyInhibited) return;
            self.GroundFrequencyInhibited = false;
            if (self.Ground == null) return;
            self.Ground.InhibitReduceFrequencyCount--;
            if (self.Ground.InhibitReduceFrequencyCount < 0)
                self.Ground.InhibitReduceFrequencyCount = 0;
        }

        /// <summary>
        /// 提取公共物理初始化逻辑。
        /// - NormalHit：不使用 Force/Curve，由 UpdateNormalHitMotion 弹簧模型驱动，持续到硬直结束。
        /// - 其他类型：HitDirection 是 attacker→target 方向，对 TowardAttacker（拉向攻击者）需要取反。
        /// </summary>
        private static void InitPhysicalMotion(this HitReactionComponent self, in HitImpactData request)
        {
            self.CurrentMotionType = request.Rule.MotionData.MotionType;

            if (request.Rule.MotionData.MotionType == HitMotionType.NormalHit)
            {
                // NormalHit：水平位移由距离维持弹簧驱动，不需要 Force/Curve
                self.MotionDirection = request.Rule.HitDirection;
                self.MotionBaseForce = 0f;
                self.CurrentMotionSpeed = 0f;
                self.CurrentMotionCurve = null;
                self.MotionStartTime = self.GetCombatNowMs();
                // 持续到硬直结束（每次命中刷新）
                self.MotionEndTime = self.HitStunEndTimeMs;
            }
            else
            {
                // 非 NormalHit：Force/Curve 路径
                self.MotionDirection = request.Rule.MotionData.MotionType == HitMotionType.TowardAttacker
                    ? -request.Rule.HitDirection
                    : request.Rule.HitDirection;
                self.MotionBaseForce = Mathf.Max(0f, request.Rule.MotionData.Force);
                self.CurrentMotionSpeed = self.MotionBaseForce;
                self.CurrentMotionCurve = request.Rule.MotionData.MotionCurve;
                self.MotionStartTime = self.GetCombatNowMs();
                self.MotionEndTime = self.MotionStartTime + Mathf.Max(0, request.Rule.MotionData.DurationMs);
            }

            // 记录拴系锚点（攻击者位置）
            self.TetherAnchorPos = self.TetherAnchorTransform.position;
        }

        /// <summary>
        /// 竖直冲量注入：仅处理 UpwardImpulse 和 DownwardImpulse，通过 LocomotionIntent.ExternalImpulse 注入。
        /// - UpwardImpulse：Y轴正方向，受空中绝对高度上限约束（接近上限线性衰减）。
        /// - DownwardImpulse：Y轴负方向。
        /// - HorizontalImpulse / TowardAttacker 不走瞬时冲量，由 InitPhysicalMotion + UpdateHitMotion
        ///   通过 ExternalTargetVelocity 持续驱动，落地时 ClearPhysicalMotion 即可完全停止。
        /// </summary>
        private static void ApplyImpulse(this HitReactionComponent self, HitMotionType motionType, float force)
        {
            var intent = self.LocomotionIntent;
            if (intent == null || force <= 0f)
                return;

            switch (motionType)
            {
                case HitMotionType.UpwardImpulse:
                {
                    float yImpulse = force;
                    // 向上冲量高度限制：仅当接近绝对上限时才衰减，低于缓冲区起点时冲量完整保留。
                    if (self.MaxAirborneHeight > self.AirborneOriginHeight)
                    {
                        float currentY = self.Owner.position.y;
                        float maxY = self.MaxAirborneHeight;
                        float totalRange = maxY - self.AirborneOriginHeight;
                        float dampenZoneRatio = 0.3f;
                        float dampenZoneStart = maxY - totalRange * dampenZoneRatio;

                        if (currentY > dampenZoneStart)
                        {
                            float dampenRange = maxY - dampenZoneStart;
                            float factor = Mathf.Clamp01((maxY - currentY) / dampenRange);
                            yImpulse *= factor;
                        }
                    }
                    intent.ExternalImpulse += new Vector3(0, yImpulse, 0);
                    break;
                }
                case HitMotionType.DownwardImpulse:
                {
                    intent.ExternalImpulse += new Vector3(0, -force, 0);
                    break;
                }
            }

            Log.Debug($"[HitReaction] ApplyImpulse: {intent.ExternalImpulse}");
        }

        #endregion

        #region 内部方法
        
        private static void UpdateAirborne(this HitReactionComponent self)
        {
            // HitStop 冻结全部运动时，不允许发生“空中→落地→倒地”的状态跃迁
            if (self.HitStop != null && self.HitStop.IsHitStopActive && self.HitStop.FreezeMode == HitStopFreezeMode.FreezeAll)
            {
                return;
            }
            if (self.Ground != null && self.Ground.IsGrounded(self.Ground.StateContext.State))
            {
                self.FromAirToGround();
                return;
            }

            // AirCombo 存在且 Active 时：检查超时并推进退出流程
            if (self.OwnerUnit != null && self.AirCombo != null && self.AirCombo.Active)
            {
                long now = self.GetCombatNowMs();
                if (now >= self.AirCombo.AirEndCombatMs && !self.AirCombo.IsExiting)
                {
                    self.AirCombo.BeginExit(now);
                }
            }
        }

        private static void FromAirToGround(this HitReactionComponent self)
        {
            Log.Error("[HitReaction] FromAirToGround");
            self.AirCombo?.ForceEnd();
            self.ReleaseGroundFrequencyInhibitIfNeeded();
            self.ClearPhysicalMotion();
            long now = self.GetCombatNowMs();
            self.KnockdownEndTime = now + self.KnockdownDurationMs;
            self.SwitchState(HitState.KnockdownHit);
        }
        private static void UpdateKnockdown(this HitReactionComponent self)
        {
            if (self.GetCombatNowMs() >= self.KnockdownEndTime && self.CurrentAnimEnd)
            {
                self.SwitchState(HitState.GetUpHit);
                self.GetUpStartTime = self.GetCombatNowMs();
            }
        }

        private static void UpdateGetUp(this HitReactionComponent self)
        {
            long now = self.GetCombatNowMs();
            if (self.GetUpTimeoutMs > 0 && now - self.GetUpStartTime >= self.GetUpTimeoutMs && self.CurrentAnimEnd)
            {
                self.EndHitReaction();
            }
        }

        private static void EndHitReaction(this HitReactionComponent self)
        {
            self.CurrentHitState = HitState.None;
            self.VisualState = HitState.None;
            self.VisualReactionType = HitReactionType.None;
            self.CurrentReactionType = HitReactionType.None;
            self.CurrentAnimEnd = false;
            self.GetUpStartTime = 0;
            self.AirborneOriginHeight = 0f;
            self.MaxAirborneHeight = 0f;
            self.TetherAnchorTransform = null;

            // 统一清理物理运动（含 ExternalTargetVelocity）
            self.ReleaseGroundFrequencyInhibitIfNeeded();

            self.ClearPhysicalMotion();

            // 重置时间戳（防止跨会话泄漏导致的异常延长/缩短）
            self.HitStunEndTimeMs = 0;
            self.KnockdownEndTime = 0;
            // 会话结束：先 release 外部锁，再做表现域的防御性清理
            self.ReleaseHitSessionLocksIfNeeded();

            if (self.LocomotionIntent != null)
            {
                Log.Debug($"[HitReaction] EndHitReaction Jump={self.LocomotionIntent.IsJumpAllowed} Move={self.LocomotionIntent.IsMoveAllowed} Rotate={self.LocomotionIntent.IsRotateAllowed} Attack={self.LocomotionIntent.IsAttackAllowed}");
            }
            // 防御性清理垂直冲量残留，确保受击结束后角色彻底静止
            if (self.LocomotionIntent != null)
            {
                self.LocomotionIntent.ExternalImpulse = Vector3.zero;
            }
            self.OnHitReactionEnd?.Invoke();
        }

        #endregion

        #region CombatTime
        private static long GetCombatNowMs(this HitReactionComponent self)
        {
            return self.HitStop?.NowCombatMs() ?? TimeInfo.Instance.ClientFrameTime();
        }

        private static float GetCombatDeltaSeconds(this HitReactionComponent self)
        {
            return self.HitStop != null ? Mathf.Max(0f, self.HitStop.CombatDeltaMs * 0.001f) : Time.deltaTime;
        }
        #endregion
    }
}
