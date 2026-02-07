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
            self.EnsureAirComboEventBindings();
        }

        [EntitySystem]
        private static void Destroy(this HitReactionComponent self)
        {
            // 兆底：确保会话级别的外部状态全部释放
            self.ReleaseHitSessionLocksIfNeeded();
            self.ReleaseGroundFrequencyInhibitIfNeeded();

            if (self.AirCombo != null && self.AirComboEventsBound)
            {
                self.AirCombo.OnExitCompleted.Remove(self.AirComboExitCompletedHandler);
                self.AirComboEventsBound = false;
                self.AirComboExitCompletedHandler = null;
            }

            self.CurrentHitState = HitState.None;
            self.VisualState = HitState.None;
            self.VisualReactionType = HitReactionType.None;
            self.CurrentAnimEnd = false;
            self.OnHitReactionStart = null;
            self.OnHitReactionEnd = null;
            self.OnLanded = null;
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

            long now = self.GetCombatNowMs();
            long durationMs = self.MotionEndTime - self.MotionStartTime;
            // 检查运动是否结束
            if (durationMs <= 0 || now >= self.MotionEndTime)
            {
                // 彻底停止物理运动（含 ExternalTargetVelocity），避免速度残留和每帧无用检查
                self.ClearPhysicalMotion();
                return;
            }

            // --- 基于曲线采样计算当前速度 ---
            // 1. 计算归一化时间 (0-1)
            float t = Mathf.Clamp01((float)(now - self.MotionStartTime) / durationMs);
            
            // 2. 采样曲线
            float curveValue = self.CurrentMotionCurve != null ? self.CurrentMotionCurve.Evaluate(t) : (1f - t);
            self.CurrentMotionSpeed = self.MotionBaseForce * curveValue;

            var intent = self.LocomotionIntent;
            if (intent != null)
            {
                if (self.CurrentMotionSpeed > 0.001f)
                {
                    if (self.AirCombo != null && self.AirCombo.Active && !self.AirCombo.IsExiting)
                    {
                        Vector3 horizontalVel = new Vector3(self.MotionDirection.x * self.CurrentMotionSpeed, 0 ,self.MotionDirection.z * self.CurrentMotionSpeed);   
                        self.AirCombo.ApplyAirComboHorizontalSpeedClamp(ref horizontalVel);
                        self.AirCombo.ApplyAirComboHorizontalRecenter(
                            self.Owner.position,
                            ref horizontalVel
                        );
                        
                        intent.ExternalTargetVelocity = new Vector2(horizontalVel.x, horizontalVel.z);
                    }
                    else
                    {
                        // 写入水平目标速度 (XZ 轴)
                        intent.ExternalTargetVelocity = new Vector2(
                            self.MotionDirection.x * self.CurrentMotionSpeed,
                            self.MotionDirection.z * self.CurrentMotionSpeed);
                    }
                }
                else
                {
                    // 速度降为 0 时，主动清理意图
                    intent.ExternalTargetVelocity = Vector2.zero;
                }
            }
        }

        private static void UpdateGroundedHitState(this HitReactionComponent self)
        {
            // 检查硬直结束 && 物理位移停止
            if (self.GetCombatNowMs() >= self.HitStunEndTimeMs && self.CurrentMotionSpeed < 0.1f)
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

                // 安全检查：如果 ApplyHit 未产生有效状态转换（例如 CustomCurve TODO、轻命中 + MotionType.None 等分支），
                // 必须释放已获取的锁，避免角色永久卡死。
                if (!self.IsInHitReaction)
                {
                    self.ReleaseHitSessionLocksIfNeeded();
                    return false;
                }
            
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
            Log.Error($"{self.CurrentHitState}");
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

        private static void HandleGroundedHit(this HitReactionComponent self, in HitImpactData request, in HitReactionConfig reactionConfig)
        {
            var res = reactionConfig.Rule.HitInterrupt.Grounded;
            long now = self.GetCombatNowMs();

            Log.Error($"HandleGroundedHit {request.Rule.HitStrength}  {request.Rule.MotionData.MotionType}");
            //砸地
            if (request.Rule.HitStrength >= res.KnockdownThreshold && request.Rule.MotionData.MotionType == HitMotionType.DownwardImpulse) 
            {
                self.InitPhysicalMotion(in request);
                self.SwitchState(HitState.KnockdownHit);
                self.ApplyImpulse(HitMotionType.DownwardImpulse, request.Rule.MotionData.Force);
                self.KnockdownEndTime = now + self.KnockdownDurationMs;
                return;
            }

            //击飞
            if (request.Rule.HitStrength >= res.AirborneThreshold && request.Rule.MotionData.MotionType == HitMotionType.UpwardImpulse)
            {
                self.EnterAirborneFromGrounded(in request);
                self.SwitchState(HitState.AirborneHit);
                return;
            }
            
            //击退或拉拽
            if(request.Rule.HitStrength >= res.KnockbackThreshold && 
               (request.Rule.MotionData.MotionType == HitMotionType.HorizontalImpulse || 
                request.Rule.MotionData.MotionType == HitMotionType.TowardAttacker))
            {
                self.InitPhysicalMotion(in request);
                self.SwitchState(HitState.GroundedHit);
                return;
            }

            // TODO 自定义曲线,忽略request.Rule.HitStrength
            if (request.Rule.MotionData.MotionType == HitMotionType.CustomCurve)
            {
                return;
            }

            // TODO 轻命中
            if (request.Rule.HitStrength >= res.LightReactionThreshold && request.Rule.MotionData.MotionType != HitMotionType.None)
            {
                self.InitPhysicalMotion(in request);
                self.SwitchState(HitState.GroundedHit);
            }
        }

        private static void HandleAirborneHit(this HitReactionComponent self, in HitImpactData request, in HitReactionConfig reactionConfig)
        {
            var res = reactionConfig.Rule.HitInterrupt.Airborne;

            // 空中：高优先级 -> 空中终结
            if (request.Rule.HitStrength >= res.AirborneThreshold)
            {
                self.EnterAirFinish(in request);
                return;
            }
            
            self.InitPhysicalMotion(in request);

            // 空中再次施加向上冲量：强制脱离地面状态
            if (request.Rule.MotionData.MotionType == HitMotionType.UpwardImpulse)
            {
                if (self.Ground != null)
                {
                    self.Ground.ForceBreakGround(AirborneReason.Juggled);
                }
                
                float originY = self.Owner.position.y;
                float maxOffset = self.CombatConfig.CachedAirCombo.MaxHeightOffset;
                self.MaxAirborneHeight = originY + (maxOffset > 0f ? maxOffset : 2.2f);
                self.ApplyImpulse(request.Rule.MotionData.MotionType, request.Rule.MotionData.Force);
            }

            // AirCombo：命中续期
            if (self.AirCombo != null && self.AirCombo.Active)
            {
                self.AirCombo.OnHit(
                    self.HitStunEndTimeMs,
                    in self.CombatConfig.CachedAirCombo,
                    request.Rule.AttackRadius);
                
                if (!self.AirCombo.IsExiting)
                {
                    Vector3 toCenter = self.AirCombo.ComboCenterWorldPos - self.Owner.position;
                    toCenter.y = 0f;

                    if (toCenter.sqrMagnitude > 0.01f)
                    {
                        self.MotionDirection = Vector3.Lerp(
                            self.MotionDirection,
                            toCenter.normalized,
                            0.3f
                        );
                    }
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
            /*if (self.AirCombo != null && self.AirCombo.Active)
            {
                self.AirCombo.ForceEnd();
            }*/
            self.AirCombo.BeginExit(self.GetCombatNowMs());
            
Log.Error("空中终结");
            /*if (self.Ground != null)
            {
                var reason = request.Rule.MotionData.MotionType == HitMotionType.UpwardImpulse
                    ? AirborneReason.Launched
                    : AirborneReason.Knockdown;
                self.Ground.ForceBreakGround(reason);
            }*/

            self.InitPhysicalMotion(in request);
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
            self.EnsureAirComboEventBindings();
            self.AcquireGroundFrequencyInhibitIfNeeded();
            self.InitPhysicalMotion(in request);
            self.ApplyImpulse(request.Rule.MotionData.MotionType, request.Rule.MotionData.Force);
            // 连段中心：优先使用攻击者位置
            Vector3 comboCenterPos = request.Rule.HasAttackerWorldPos ? request.Rule.AttackerWorldPos : self.Owner.position;
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
                self.Attack.AttackCommand.Clear();
                self.Attack.ForceCancel();
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
        /// HitDirection 是 attacker→target 方向，对 TowardAttacker（拉向攻击者）需要取反。
        /// </summary>
        private static void InitPhysicalMotion(this HitReactionComponent self, in HitImpactData request)
        {
            self.CurrentMotionType = request.Rule.MotionData.MotionType;
            // TowardAttacker 拉向攻击者，方向与 HitDirection（attacker→target）相反
            self.MotionDirection = request.Rule.MotionData.MotionType == HitMotionType.TowardAttacker
                ? -request.Rule.HitDirection
                : request.Rule.HitDirection;
            self.MotionBaseForce = Mathf.Max(0f, request.Rule.MotionData.Force);
            self.CurrentMotionSpeed = self.MotionBaseForce;
            self.CurrentMotionCurve = request.Rule.MotionData.MotionCurve;
            self.MotionStartTime = self.GetCombatNowMs();
            self.MotionEndTime = self.MotionStartTime + Mathf.Max(0, request.Rule.MotionData.DurationMs);
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

            Log.Error($"{intent.ExternalImpulse}");
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
            Log.Error("FromAirToGround");
            self.AirCombo?.ForceEnd();
            self.OnLanded?.Invoke();
            self.ReleaseGroundFrequencyInhibitIfNeeded();
            self.ClearPhysicalMotion();
            long now = self.GetCombatNowMs();
            self.KnockdownEndTime = now + self.KnockdownDurationMs;
            self.SwitchState(HitState.KnockdownHit);
        }

        private static void EnsureAirComboEventBindings(this HitReactionComponent self)
        {
            if (self == null || self.AirCombo == null || self.AirComboEventsBound)
            {
                return;
            }

            if (self.AirComboExitCompletedHandler == null)
            {
                self.AirComboExitCompletedHandler = () =>
                {
                    // AirCombo 退出完成后无额外动作，落地由 UpdateAirborne 轮询 Ground 状态检测
                };
            }

            self.AirCombo.OnExitCompleted.Add(self.AirComboExitCompletedHandler);
            self.AirComboEventsBound = true;
        }

        private static void UpdateKnockdown(this HitReactionComponent self)
        {
            if (self.GetCombatNowMs() >= self.KnockdownEndTime)
            {
                self.SwitchState(HitState.GetUpHit);
                self.GetUpStartTime = self.GetCombatNowMs();
            }
        }

        private static void UpdateGetUp(this HitReactionComponent self)
        {
            long now = self.GetCombatNowMs();
            if (self.GetUpTimeoutMs > 0 && now - self.GetUpStartTime >= self.GetUpTimeoutMs)
            {
                self.EndHitReaction();
                return;
            }
            if (self.CurrentAnimEnd)
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

            Log.Error("受击结束");
            // 统一清理物理运动（含 ExternalTargetVelocity）
            self.ReleaseGroundFrequencyInhibitIfNeeded();

            self.ClearPhysicalMotion();

            // 重置时间戳（防止跨会话泄漏导致的异常延长/缩短）
            self.HitStunEndTimeMs = 0;
            self.KnockdownEndTime = 0;
            // 会话结束：先 release 外部锁，再做表现域的防御性清理
            self.ReleaseHitSessionLocksIfNeeded();

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
