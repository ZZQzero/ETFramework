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

            // Ground 落地事件：只记录“落地事实”，不在回调内直接切状态（避免与 FixedUpdate/HitStop 产生竞态）。
            if (self.Ground != null && self.GroundOnLandedHandler == null)
            {
                self.GroundOnLandedHandler = () =>
                {
                    // 仅在空中受击会话内才消费落地事实；其余落地（例如正常移动落地）不关心。
                    if (!self.IsInHitReaction)
                    {
                        return;
                    }
                    if (self.CurrentHitState != HitState.Airborne && self.CurrentHitState != HitState.AirFinisher)
                    {
                        return;
                    }
                    self.LandSession.LandQueued = true;
                    self.LandSession.LandCombatMs = self.GetCombatNowMs();
                };
                self.Ground.OnLanded += self.GroundOnLandedHandler;
            }
        }

        [EntitySystem]
        private static void Destroy(this HitReactionComponent self)
        {
            // 兜底：确保会话级别的外部状态全部释放（避免异常残留锁/地检配置）
            self.ReleaseHitSessionLocksIfNeeded();
            self.ReleaseGroundDetectBoostIfNeeded();

            if (self.Ground != null && self.GroundOnLandedHandler != null)
            {
                self.Ground.OnLanded -= self.GroundOnLandedHandler;
                self.GroundOnLandedHandler = null;
            }

            if (self.AirCombo != null && self.AirComboEventsBound)
            {
                self.AirCombo.OnGroundDetectRequested -= self.AirComboGroundDetectHandler;
                self.AirCombo.OnExitCompleted -= self.AirComboExitCompletedHandler;
                self.AirComboEventsBound = false;
                self.AirComboGroundDetectHandler = null;
                self.AirComboExitCompletedHandler = null;
            }

            self.CurrentHitState = HitState.None;
            self.VisualState = HitState.None;
            self.VisualReactionType = HitReactionType.None;
            self.CurrentAnimEnd = false;
            self.OnHitReactionStart = null;
            self.OnHitReactionEnd = null;
            self.OnLanded = null;

            self.ResetAirborneLandSession();
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
                case HitState.Grounded:
                    self.UpdateGroundedHitState();
                    break;
                case HitState.Airborne:
                case HitState.AirFinisher:
                    self.UpdateAirborne();
                    break;
                case HitState.Knockdown:
                    self.UpdateKnockdown();
                    break;
                case HitState.GetUp:
                    self.UpdateGetUp();
                    break;
            }
        }

        private static void UpdateHitMotion(this HitReactionComponent self)
        {
            if (self.CurrentMotionType == HitMotionType.Normal || self.MotionEndTime <= 0) return;

            long now = self.GetCombatNowMs();
            long durationMs = self.MotionEndTime - self.MotionStartTime;
            // 检查运动是否结束
            if (durationMs <= 0 || now >= self.MotionEndTime)
            {
                self.CurrentMotionSpeed = 0f;
                self.CurrentMotionType = HitMotionType.Normal;
                
                // 核心修复：位移结束时必须清理意图，防止怪物无限滑行
                if (self.LocomotionIntent != null)
                {
                    self.LocomotionIntent.ExternalTargetVelocity = Vector2.zero;
                }
                return;
            }

            // --- 核心逻辑：基于曲线采样计算当前速度 ---
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
                            ref horizontalVel,
                            self.GetCombatDeltaSeconds()
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
            if (self.GetCombatNowMs() >= self.StunEndTime && self.CurrentMotionSpeed < 0.1f)
            {
                Log.Error("Grounded 受击结束");
                self.EndHitReaction();
            }
        }

        /// <summary>
        /// 统一受击入口
        /// - Resistance 按状态切表（Grounded/Airborne/AirStun/Knockdown/GetUp）
        /// </summary>
        public static bool TryApplyHit(this HitReactionComponent self, in HitReactionRequest hitReaction)
        {
            if (self == null || self.IsDisposed || self.Owner == null || self.OwnerUnit == null)
            {
                return false;
            }

            HitReactionProfileProvider.ResolveConfig(self.OwnerUnit, out var rulesConfig, out var hitFeedbackConfig);
            // 缓存本单位的视觉许可（用于帧内/帧间状态流转时同步视觉状态）
            self.AllowedReactionGroups = rulesConfig.AllowedReactionGroups;
            self.AllowedStateVisuals = rulesConfig.AllowedStateVisuals;

            bool hasVisualOrPhysical = hitReaction.Rule.ReactionType != HitReactionType.None || hitReaction.Rule.MotionData.MotionType != HitMotionType.Normal;
            bool hasAnyFeedback =
                hitReaction.Feedback.VictimHitStopMs > 0 ||
                hitReaction.Feedback.ScreenShakeIntensity > 0f ||
                hitReaction.Feedback.ScreenShakeDurationMs > 0 ||
                (hitReaction.Feedback.TimeScale > 0f && !Mathf.Approximately(hitReaction.Feedback.TimeScale, 1f)) ||
                hitReaction.Feedback.TimeScaleDurationMs > 0;

            // 允许“仅反馈”的请求（例如格挡成功、护盾命中）：不进入受击状态机
            // 注意：此分支不参与 AcceptMask/TargetStates/CanBeHit 判定，避免把表现反馈耦合进 gameplay 受击规则。
            if (!hasVisualOrPhysical)
            {
                if (!hasAnyFeedback)
                {
                    return false;
                }
                // 反馈-only：只按反馈 profile 应用，不进入受击会话
                self.ApplyFeedback(in hitReaction, in hitFeedbackConfig);
                return true;
            }

            if (!self.PassTargetStateFilter(hitReaction.Rule.TargetStates))
                return false;

            // 归一化参数（应用 Scale 和 Limit）
            HitReactionRequest normalized = self.Normalize(hitReaction, in rulesConfig);

            // GetUp：起身中，门槛不够直接拒绝（不进入受击会话，也不触发反馈）
            if (self.CurrentHitState == HitState.GetUp) 
            {
                var getUpRes = rulesConfig.HitInterrupt.GetUp;
                if (normalized.Rule.HitStrength < getUpRes.GetUpInterruptThreshold)
                {
                    return false;
                }
            }
            
            // 受击会话：进入会话才 acquire 外部能力锁（规则生效，与是否播放动画无关）
            self.AcquireHitSessionLocksIfNeeded();

            try
            {
                // 应用受击（状态切表）
                self.ApplyHit(in normalized, in rulesConfig);
            
                // 规则生效后，才做“表现许可/降级/禁播”
                self.ApplyVisualOutcome(in normalized);

                // 反馈：默认仍按反馈 profile 执行（可后续进一步纳入 VisualOutcome 细分）
                self.ApplyFeedback(in normalized, in hitFeedbackConfig);
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

        private static void ApplyHit(this HitReactionComponent self, in HitReactionRequest request, in HitReactionRulesConfig reactionRulesConfig)
        {
            // 统一更新“当前受击表现类型”和“硬直截止点”
            self.CurrentReactionType = request.Rule.ReactionType;
            long now = self.GetCombatNowMs();
            long nextStunEnd = now + Mathf.Max(0, request.Rule.HitStunMs);
            if (nextStunEnd > self.StunEndTime)
            {
                self.StunEndTime = nextStunEnd;
            }
            
            switch (self.CurrentHitState)
            {
                case HitState.None:
                case HitState.Grounded:
                    self.HandleGroundedHit(in request, in reactionRulesConfig);
                    return;
                case HitState.Airborne:
                    self.HandleAirborneHit(in request, in reactionRulesConfig);
                    return;
                case HitState.AirFinisher:
                    self.HandleAirStunHit(in request, in reactionRulesConfig);
                    return;
                case HitState.Knockdown:
                    self.HandleKnockdownHit(in request, in reactionRulesConfig);
                    return;
                case HitState.GetUp:
                    self.HandleGetUpHit(in request, in reactionRulesConfig);
                    return;
            }
        }

        private static void SwitchState(this HitReactionComponent self, HitState next)
        {
            if (self.CurrentHitState == next)
            {
                return;
            }
            var prev = self.CurrentHitState;
            self.CurrentHitState = next;
            self.UpdateVisualStateForCurrentState();
            self.OnHitReactionStart?.Invoke(next);
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
                HitState.Grounded => HitStateVisualMask.Grounded,
                HitState.Airborne => HitStateVisualMask.Airborne,
                HitState.AirFinisher => HitStateVisualMask.AirStun,
                HitState.Knockdown => HitStateVisualMask.Knockdown,
                HitState.GetUp => HitStateVisualMask.GetUp,
                _ => HitStateVisualMask.None
            };
            return flag != HitStateVisualMask.None && (allowed & flag) != 0;
        }

        private static void ApplyVisualOutcome(this HitReactionComponent self, in HitReactionRequest request)
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

        private static void HandleGroundedHit(this HitReactionComponent self, in HitReactionRequest request, in HitReactionRulesConfig reactionRulesConfig)
        {
            var res = reactionRulesConfig.HitInterrupt.Grounded;
            long now = self.GetCombatNowMs();

            // Grounded：起身中才允许被门槛打断，否则直接按地面受击处理
            self.SwitchState(HitState.Grounded);
           
            switch (request.Rule.MotionData.MotionType)
            {
                case HitMotionType.Normal:
                    //只处理硬值，怪物不会被击退,怪物不往前走
                    if (request.Rule.HitStrength >= res.LightReactionThreshold)
                    {
                        self.CurrentMotionType = request.Rule.MotionData.MotionType;
                    }
                    break;
                case HitMotionType.Knockback:
                case HitMotionType.PullTowardAttacker:
                    //击退或吸过来,拉拽
                    if (request.Rule.HitStrength > res.KnockbackThreshold)
                    {
                        self.InitPhysicalMotion(in request);
                    }
                    break;
                case HitMotionType.Knockup:
                    //从地面击飞
                    if (request.Rule.HitStrength > res.AirborneThreshold)
                    {
                        self.EnterAirborneFromGrounded(in request);
                    }
                    break;
                case HitMotionType.KnockDown:
                    //砸地
                    self.SwitchState(HitState.Knockdown);
                    self.KnockdownEndTime = now + self.KnockdownDurationMs;
                    break;
            }
        }

        private static void HandleAirborneHit(this HitReactionComponent self, in HitReactionRequest request, in HitReactionRulesConfig reactionRulesConfig)
        {
            var res = reactionRulesConfig.HitInterrupt.Airborne;

            // 空中：高优先级 -> 空中终结
            if (request.Rule.HitStrength >= res.AirborneThreshold)
            {
                self.EnterAirStun(in request);
                return;
            }

            // 空中：达到门槛 -> 砸地（通过 AirborneReason 驱动落地后倒地）
            if (request.Rule.HitStrength >= res.KnockdownThreshold)
            {
                self.BeginAirSlamToKnockdown(in request);
                return;
            }

            // 空中：轻/中等命中成立；Knockup 根据 MaxAirborneKnockupForce 允许 capped 二次击飞
            HitReactionRequest filtered = self.FilterAirborneMotion(in request, allowSecondaryKnockup: true);
            if (filtered.Rule.MotionData.MotionType != HitMotionType.Normal)
            {
                self.InitPhysicalMotion(in filtered);
                if (filtered.Rule.MotionData.MotionType == HitMotionType.Knockup)
                {
                    self.ApplyVerticalImpulse(HitMotionType.Knockup, filtered.Rule.MotionData.Force);
                }
            }

            // AirCombo：命中续期（KeepAlive）——只续期，不叠加高度
            if (self.AirCombo != null && self.AirCombo.Active)
            {
                HitReactionProfileProvider.ResolveAirCombo(self.OwnerUnit, out var acProfile);
                self.AirCombo.OnHit(self.GetCombatNowMs(), in acProfile, request.AirCombo.AttackerSegmentComboTimeoutMs, request.AirCombo.AttackRadius);
                
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
                
                self.OnHitReactionStart?.Invoke(self.CurrentHitState);
            }
        }

        private static void HandleAirStunHit(this HitReactionComponent self, in HitReactionRequest request, in HitReactionRulesConfig reactionRulesConfig)
        {
            var res = reactionRulesConfig.HitInterrupt.AirFinisher;

            // 允许更强的砸地（或保持终结态）
            if (request.Rule.HitStrength >= res.KnockdownThreshold)
            {
                self.BeginAirSlamToKnockdown(in request);
                return;
            }

            // 其余命中：只刷新硬直/反馈（不改变状态），完全禁止二次击飞
            HitReactionRequest filtered = self.FilterAirborneMotion(in request, allowSecondaryKnockup: false);
            if (filtered.Rule.MotionData.MotionType != HitMotionType.Normal)
            {
                self.InitPhysicalMotion(in filtered);
            }
        }

        private static void HandleKnockdownHit(this HitReactionComponent self, in HitReactionRequest request, in HitReactionRulesConfig reactionRulesConfig)
        {
            var res = reactionRulesConfig.HitInterrupt.Knockdown;

            // 倒地：只有达到门槛才允许“续倒地/打断起身”（避免无限压起身）
            if (request.Rule.HitStrength >= res.KnockdownThreshold)
            {
                self.KnockdownEndTime = self.GetCombatNowMs() + self.KnockdownDurationMs;
            }
        }

        private static void HandleGetUpHit(this HitReactionComponent self, in HitReactionRequest request, in HitReactionRulesConfig reactionRulesConfig)
        {
            var res = reactionRulesConfig.HitInterrupt.GetUp;

            // 起身：门槛不够则直接拒绝（不进入受击会话）
            if (request.Rule.HitStrength < res.GetUpInterruptThreshold)
            {
                return;
            }

            // 门槛够：打断起身，按地面受击重新评估（可能击飞）
            self.SwitchState(HitState.Grounded);
            self.HandleGroundedHit(in request, in reactionRulesConfig);
        }

        

        private static void EnterAirStun(this HitReactionComponent self, in HitReactionRequest request)
        {
            // 进入终结态：强制结束 AirCombo（防止继续挂空），之后进入自然下落，必须恢复地检才能落地
            if (self.OwnerUnit != null)
            {
                var airCombo = self.OwnerUnit.GetComponent<CombatContextComponent>()?.AirCombo;
                if (airCombo != null && airCombo.Active)
                {
                    airCombo.ForceEnd();
                }
            }

            self.EnableGroundDetectForLandingIfNeeded();

            self.SwitchState(HitState.AirFinisher);

            // 空中终结仍允许 Push/Pull（但完全禁止 Launch）
            HitReactionRequest filtered = self.FilterAirborneMotion(in request, allowSecondaryKnockup: false);
            if (filtered.Rule.MotionData.MotionType != HitMotionType.Normal)
            {
                self.InitPhysicalMotion(in filtered);
            }
        }

        private static void BeginAirSlamToKnockdown(this HitReactionComponent self, in HitReactionRequest request)
        {
            // Slam：设置落地语义为 Knockdown，并注入向下冲量（不改变为空中二次击飞）
            self.SwitchState(self.CurrentHitState == HitState.AirFinisher ? HitState.AirFinisher : HitState.Airborne);

            // 落地分流语义必须在“语义产生点”固化，避免落地时读 Ground.AirborneReason 产生竞态。
            self.LandSession.Outcome = PendingLandOutcome.Knockdown;
            self.LandSession.LandQueued = false;
            self.LandSession.LandHandled = false;
            self.LandSession.LandCombatMs = 0;

            if (self.Ground != null)
            {
                // 已经在空中时不需要 ForceBreakGround，但需要把落地语义改为 Knockdown
                self.Ground.StateContext.AirborneReason = AirborneReason.Knockdown;
            }

            if (request.Rule.MotionData.MotionType == HitMotionType.KnockDown)
            {
                self.InitPhysicalMotion(in request);
                self.ApplyVerticalImpulse(HitMotionType.KnockDown, request.Rule.MotionData.Force);
            }
        }

        private static void EnterAirborneFromGrounded(this HitReactionComponent self, in HitReactionRequest request)
        {
            self.SwitchState(HitState.Airborne);

            // 新一段空中受击会话：初始化落地语义（默认普通落地硬直）与落地事件消费标记
            self.LandSession.Outcome = PendingLandOutcome.Grounded;
            self.LandSession.LandingStunMs = 0;
            self.LandSession.LandQueued = false;
            self.LandSession.LandHandled = false;
            self.LandSession.LandCombatMs = 0;

            // 进入空中连击维持期：关闭地检（性能），并确保退出期的高频地检加持不会泄漏
            self.ReleaseGroundDetectBoostIfNeeded();

            if (self.Ground != null)
            {
                var type = HitReactionProfileProvider.ResolveAirborneReasonForRequest(in request);
                self.Ground.ForceBreakGround(type);
            }
            self.EnsureAirComboEventBindings();
            self.InitPhysicalMotion(in request);
            self.ApplyVerticalImpulse(request.Rule.MotionData.MotionType, request.Rule.MotionData.Force);
            HitReactionProfileProvider.ResolveAirCombo(self.OwnerUnit, out var acProfile);
            // 连段中心：优先使用攻击者位置，未设置时 fallback 到受击者位置
            Vector3 comboCenterPos = request.AirCombo.HasAttackerWorldPos ? request.AirCombo.AttackerWorldPos : self.Owner.position;
            self.AirCombo.Enter(self.GetCombatNowMs(), comboCenterPos, in acProfile, request.AirCombo.AttackerSegmentComboTimeoutMs, request.AirCombo.AttackRadius);
            
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
                var combat = self.OwnerUnit?.GetComponent<CombatContextComponent>();
                combat?.Attack?.ForceCancel();
                combat?.AttackCommand?.Clear();
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

        private static void ApplyFeedback(this HitReactionComponent self, in HitReactionRequest request, in HitFeedbackConfig config)
        {
            if (config.Option.AllowVictimHitStop && request.Feedback.VictimHitStopMs > 0)
            {
                int ms = Mathf.RoundToInt(request.Feedback.VictimHitStopMs * Mathf.Max(0f, config.Option.VictimHitStopScale));
                var hitStop = self.OwnerUnit?.GetComponent<CombatContextComponent>()?.HitStop;
                var anim = self.OwnerUnit?.GetComponent<AnimatorComponent>()?.Animancer;
                if (hitStop != null && anim != null)
                {
                    hitStop.RequestHitStop(ms, anim, HitStopFreezeMode.FreezeAll);
                }
            }
        }

        /// <summary>
        /// 提取公共物理初始化逻辑
        /// </summary>
        private static void InitPhysicalMotion(this HitReactionComponent self, in HitReactionRequest request)
        {
            self.CurrentMotionType = request.Rule.MotionData.MotionType;
            self.MotionDirection = request.Rule.HitDirection.sqrMagnitude > 0.0001f ? request.Rule.HitDirection.normalized : Vector3.zero;
            self.MotionBaseForce = Mathf.Max(0f, request.Rule.MotionData.Force);
            self.CurrentMotionSpeed = self.MotionBaseForce;
            self.CurrentMotionCurve = request.Rule.MotionData.MotionCurve;
            self.MotionStartTime = self.GetCombatNowMs();
            self.MotionEndTime = self.MotionStartTime + Mathf.Max(0, request.Rule.MotionData.DurationMs);
        }

        /// <summary>
        /// 统一处理垂直力注入（通过 LocomotionIntent 注入 3D 瞬时冲量请求）
        /// </summary>
        private static void ApplyVerticalImpulse(this HitReactionComponent self, HitMotionType motionType, float force)
        {
            var intent = self.LocomotionIntent;
            if (intent != null)
            {
                // 注入 3D 冲量请求（使用 += 确保多源力叠加）
                float yImpulse = (motionType == HitMotionType.KnockDown) ? -force : force;
                intent.ExternalImpulse += new Vector3(0, yImpulse, 0);
            }
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 重置“空中受击落地语义”会话字段（进入新空中会话/退出受击会话/销毁时调用）。
        /// </summary>
        private static void ResetAirborneLandSession(this HitReactionComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.LandSession.Reset();
        }

        /// <summary>
        /// 落地已消费后清理本次落地语义（保留 LandHandled=true，防止重复消费）。
        /// </summary>
        private static void ClearAirborneLandIntentAfterConsumed(this HitReactionComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.LandSession.ClearAfterConsumed();
        }

        /// <summary>
        /// 退出/下落期打开地检，并在短窗口内强制高频检测（避免空中降频错过落地）。
        /// </summary>
        private static void EnableGroundDetectForLandingIfNeeded(this HitReactionComponent self)
        {
            if (self == null || self.Ground == null)
            {
                return;
            }

            self.Ground.Enable = true;

            if (!self.GroundDetectBoosted)
            {
                Log.Error("增强地面检测频率以确保落地");
                self.Ground.InhibitReduceFrequencyCount++;
                self.GroundDetectBoosted = true;
            }
        }

        private static void ReleaseGroundDetectBoostIfNeeded(this HitReactionComponent self)
        {
            if (self == null)
            {
                return;
            }

            if (!self.GroundDetectBoosted)
            {
                return;
            }

            if (self.Ground != null)
            {
                self.Ground.InhibitReduceFrequencyCount--;
                if (self.Ground.InhibitReduceFrequencyCount < 0)
                {
                    self.Ground.InhibitReduceFrequencyCount = 0;
                }
            }

            self.GroundDetectBoosted = false;
        }

        private static void UpdateAirborne(this HitReactionComponent self)
        {
            // HitStop 冻结全部运动时，不允许发生“空中→落地→倒地”的状态跃迁（否则会在顿帧窗口出现假落地）
            if (self.HitStop != null && self.HitStop.IsHitStopActive && self.HitStop.FreezeMode == HitStopFreezeMode.FreezeAll)
            {
                return;
            }

            // 优先消费“落地事实”（由 Ground.OnLanded 置位），确保只处理一次且不受 Ground.AirborneReason 重置影响
            if (self.LandSession.LandQueued && !self.LandSession.LandHandled)
            {
                self.OnLand();
                return;
            }
            
            if (self.OwnerUnit != null && self.AirCombo != null)
            {
                if (self.AirCombo.Active)
                {
                    long now = self.GetCombatNowMs();
                    if (now >= self.AirCombo.AbsoluteEndCombatMs)
                    {
                        self.AirCombo.BeginExit(now);
                    }
                    else if (now >= self.AirCombo.EndCombatMs)
                    {
                        self.AirCombo.BeginExit(now);
                    }

                    if (self.AirCombo.IsExitCompleted(now))
                    {
                        // 退出完成：交给自然物理下落（不再接管）
                        self.AirCombo.ForceEnd();
                    }
                }
                else
                {
                    // 空中/终结态但 AirCombo 已不 Active（例如 EnterAirStun 已 ForceEnd）：自然下落，必须开地检才能落地
                    self.EnableGroundDetectForLandingIfNeeded();
                }
            }
            else if (self.IsAirborne)
            {
                // AirCombo 组件不存在时，空中态也需地检才能落地
                self.EnableGroundDetectForLandingIfNeeded();
            }

            // 落地只由 Ground.OnLanded 事件驱动，避免与轮询路径重复触发
        }

        private static void OnLand(this HitReactionComponent self)
        {
            self.OnLanded?.Invoke();

            // 落地处理必须只消费一次（可能被 Update 轮询/地检状态机多次观测到 grounded）
            if (self.LandSession.LandHandled)
            {
                return;
            }
            self.LandSession.LandQueued = false;
            self.LandSession.LandHandled = true;
            Log.Error("OnLand 受击落地处理");
            // 落地后恢复正常地检，并归还“退出/下落期”高频加持
            if (self.Ground != null)
            {
                self.Ground.Enable = true;
            }
            self.ReleaseGroundDetectBoostIfNeeded();

            long now = self.GetCombatNowMs();

            // 落地必定结束空中连段（无论是普通落地还是砸地倒地）
            int landingStunMs = 0;
            if (self.AirCombo != null)
            {
                landingStunMs = self.AirCombo.LandingStunMs;
                self.AirCombo.ForceEnd();
            }

            // 固化语义：落地分流不再依赖 Ground.AirborneReason（该值会在落地事件后被重置）
            PendingLandOutcome outcome = self.LandSession.Outcome;
            int cachedLandingStunMs = self.LandSession.LandingStunMs;
            self.ClearAirborneLandIntentAfterConsumed();

            if (self.CurrentHitState == HitState.Airborne || self.CurrentHitState == HitState.AirFinisher)
            {
                if (outcome == PendingLandOutcome.Knockdown)
                {
                    self.SwitchState(HitState.Knockdown);
                    long end = now + self.KnockdownDurationMs;
                    Log.Error("落地硬直切倒地，KnockdownEndTime=" + end + "   " + self.KnockdownDurationMs);
                    if (end > self.KnockdownEndTime)
                    {
                        self.KnockdownEndTime = end;
                    }
                    return;
                }

                // 默认：普通落地硬直（落地硬直取 max-extend，不覆盖缩短）
                int finalLandingStunMs = cachedLandingStunMs > 0 ? cachedLandingStunMs : landingStunMs;
                self.SwitchState(HitState.Grounded);
                long nextStunEnd = now + Mathf.Max(0, finalLandingStunMs);
                if (nextStunEnd > self.StunEndTime)
                {
                    self.StunEndTime = nextStunEnd;
                }
                return;
            }

            self.EndHitReaction();
        }

        private static void EnsureAirComboEventBindings(this HitReactionComponent self)
        {
            if (self == null || self.AirCombo == null || self.AirComboEventsBound)
            {
                return;
            }

            if (self.AirComboGroundDetectHandler == null)
            {
                self.AirComboGroundDetectHandler = enable =>
                {
                    if (enable)
                    {
                        self.EnableGroundDetectForLandingIfNeeded();
                        return;
                    }

                    if (self.Ground != null)
                    {
                        self.Ground.Enable = false;
                    }
                    self.ReleaseGroundDetectBoostIfNeeded();
                };
            }

            if (self.AirComboExitCompletedHandler == null)
            {
                self.AirComboExitCompletedHandler = () =>
                {
                    // 退出完成后：确保地检开启以稳定落地
                    self.EnableGroundDetectForLandingIfNeeded();
                };
            }

            self.AirCombo.OnGroundDetectRequested += self.AirComboGroundDetectHandler;
            self.AirCombo.OnExitCompleted += self.AirComboExitCompletedHandler;
            self.AirComboEventsBound = true;
        }

        private static void UpdateKnockdown(this HitReactionComponent self)
        {
            if (self.GetCombatNowMs() >= self.KnockdownEndTime)
            {
                self.SwitchState(HitState.GetUp);
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
            self.CurrentAnimEnd = false;
            self.CurrentMotionSpeed = 0;
            self.GetUpStartTime = 0;

            // 会话结束：清理落地语义与地检短窗口加持
            if (self.Ground != null)
            {
                self.Ground.Enable = true;
            }
            self.ReleaseGroundDetectBoostIfNeeded();
            self.ResetAirborneLandSession();

            // 会话结束：先 release 外部锁，再做表现域的防御性清理
            self.ReleaseHitSessionLocksIfNeeded();

            var intent = self.LocomotionIntent;
            if (intent != null)
            {
                // 防御性清理所有外部物理信号，确保受击结束后角色彻底静止
                intent.ExternalTargetVelocity = Vector2.zero;
                intent.ExternalImpulse = Vector3.zero;
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
