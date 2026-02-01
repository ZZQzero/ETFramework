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
            self.HitStop = self.OwnerUnit.GetComponent<HitStopComponent>();
            self.Ground = self.OwnerUnit.GetComponent<CheckGroundedComponent>();
            self.LocomotionIntent = self.OwnerUnit.GetComponent<LocomotionIntentComponent>();
            self.AirCombo = self.OwnerUnit.GetComponent<AirComboComponent>();
        }

        [EntitySystem]
        private static void Destroy(this HitReactionComponent self)
        {
            // 兜底：确保会话级别的外部状态全部释放（避免异常残留锁/地检配置）
            self.ReleaseHitSessionLocksIfNeeded();

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

            // --- 轨道 B：视觉状态机 (Reaction State Machine) ---
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
                    // 写入水平目标速度 (XZ 轴)
                    intent.ExternalTargetVelocity = new Vector2(
                        self.MotionDirection.x * self.CurrentMotionSpeed,
                        self.MotionDirection.z * self.CurrentMotionSpeed);
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
                self.EndHitReaction();
            }
        }

        /// <summary>
        /// 统一受击入口
        /// - Resistance 按状态切表（Grounded/Airborne/AirStun/Knockdown/GetUp）
        /// - 空中禁止二次击飞（Launch 在空中会被降级/忽略）
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
            Log.Error($"允许播放的分组 {self.AllowedReactionGroups}   {self.AllowedStateVisuals}  \n");

            bool hasVisualOrPhysical = hitReaction.ReactionType != HitReactionType.None || hitReaction.MotionData.MotionType != HitMotionType.Normal;
            bool hasAnyFeedback =
                hitReaction.VictimHitStopMs > 0 ||
                hitReaction.ScreenShakeIntensity > 0f ||
                hitReaction.ScreenShakeDurationMs > 0 ||
                (hitReaction.TimeScale > 0f && !Mathf.Approximately(hitReaction.TimeScale, 1f)) ||
                hitReaction.TimeScaleDurationMs > 0;

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

            if (!self.PassTargetStateFilter( hitReaction.TargetStates))
                return false;

            // 归一化参数（应用 Scale 和 Limit）
            HitReactionRequest normalized = self.Normalize(hitReaction, in rulesConfig);

            Log.Error($"归一化参数 {normalized}");
            // GetUp：起身中，门槛不够直接拒绝（不进入受击会话，也不触发反馈）
            if (self.CurrentHitState == HitState.GetUp) 
            {
                var getUpRes = rulesConfig.hitInterruptThresholds.GetUp;
                if (normalized.HitStrength < getUpRes.GetUpInterruptThreshold)
                {
                    return false;
                }
            }
            
            // 受击会话：进入会话才 acquire 外部能力锁（规则生效，与是否播放动画无关）
            self.AcquireHitSessionLocksIfNeeded();

            // 应用受击（状态切表）
            self.ApplyHit(in normalized, in rulesConfig);
            
            // 规则生效后，才做“表现许可/降级/禁播”
            self.ApplyVisualOutcome(in normalized);

            Log.Error($"视觉许可 {self.VisualReactionType}  {self.IsInHitVisual}");

            // 反馈：默认仍按反馈 profile 执行（可后续进一步纳入 VisualOutcome 细分）
            self.ApplyFeedback(in normalized, in hitFeedbackConfig);
            return true;
        }

        private static void ApplyHit(this HitReactionComponent self, in HitReactionRequest request, in HitReactionRulesConfig reactionRulesConfig)
        {
            // 统一更新“当前受击表现类型”和“硬直截止点”
            self.CurrentReactionType = request.ReactionType;
            long now = self.GetCombatNowMs();
            long nextStunEnd = now + Mathf.Max(0, request.HitStunMs);
            if (nextStunEnd > self.StunEndTime)
            {
                self.StunEndTime = nextStunEnd;
            }

            Log.Error($"正式进入受击：  {self.OwnerUnit.UnitName}  {self.CurrentReactionType}  {self.CurrentHitState}");
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
            Log.Error($"切换状态： {self.CurrentHitState}  {self.VisualState}");
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
            HitReactionType desired = request.ReactionType;
            HitReactionType visualType = self.DegradeReactionType(desired, self.AllowedReactionGroups);
            self.VisualReactionType = visualType;
        }

        private static void HandleGroundedHit(this HitReactionComponent self, in HitReactionRequest request, in HitReactionRulesConfig reactionRulesConfig)
        {
            var res = reactionRulesConfig.hitInterruptThresholds.Grounded;
            long now = self.GetCombatNowMs();

            // Grounded：起身中才允许被门槛打断，否则直接按地面受击处理
            self.SwitchState(HitState.Grounded);
            Log.Error($"MotionType {request.MotionData.MotionType}  {request.HitStrength}  {res.KnockbackThreshold}  {res.LightReactionThreshold}  {res.AirborneThreshold}");

            switch (request.MotionData.MotionType)
            {
                case HitMotionType.Normal:
                    //只处理硬值，怪物不会被击退,怪物不往前走
                    if (request.HitStrength >= res.LightReactionThreshold)
                    {
                        self.CurrentMotionType = request.MotionData.MotionType;
                    }
                    break;
                case HitMotionType.Knockback:
                case HitMotionType.PullTowardAttacker:
                    //击退或吸过来,拉拽
                    if (request.HitStrength > res.KnockbackThreshold)
                    {
                        self.InitPhysicalMotion(in request);
                    }
                    break;
                case HitMotionType.Knockup:
                    //从地面击飞
                    if (request.HitStrength > res.AirborneThreshold)
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
            var res = reactionRulesConfig.hitInterruptThresholds.Airborne;

            // 空中：高优先级 -> 空中终结
            if (request.HitStrength >= res.AirborneThreshold)
            {
                self.EnterAirStun(in request);
                return;
            }

            // 空中：达到门槛 -> 砸地（通过 AirborneReason 驱动落地后倒地）
            if (request.HitStrength >= res.KnockdownThreshold)
            {
                self.BeginAirSlamToKnockdown(in request);
                return;
            }

            // 空中：轻/中等命中成立；Knockup 根据 MaxAirborneKnockupForce 允许 capped 二次击飞
            HitReactionRequest filtered = self.FilterAirborneMotion(in request, allowSecondaryKnockup: true);
            if (filtered.MotionData.MotionType != HitMotionType.Normal)
            {
                self.InitPhysicalMotion(in filtered);
                if (filtered.MotionData.MotionType == HitMotionType.Knockup)
                {
                    self.ApplyVerticalImpulse(HitMotionType.Knockup, filtered.MotionData.Force);
                }
            }

            // AirCombo：命中续期（KeepAlive）——只续期，不叠加高度
            if (self.AirCombo != null && self.AirCombo.Active)
            {
                HitReactionProfileProvider.ResolveAirCombo(self.OwnerUnit, out var acProfile);
                self.AirCombo.OnHit(self.GetCombatNowMs(), in acProfile);
            }
        }

        private static void HandleAirStunHit(this HitReactionComponent self, in HitReactionRequest request, in HitReactionRulesConfig reactionRulesConfig)
        {
            var res = reactionRulesConfig.hitInterruptThresholds.AirFinisher;

            // 允许更强的砸地（或保持终结态）
            if (request.HitStrength >= res.KnockdownThreshold)
            {
                self.BeginAirSlamToKnockdown(in request);
                return;
            }

            // 其余命中：只刷新硬直/反馈（不改变状态），完全禁止二次击飞
            HitReactionRequest filtered = self.FilterAirborneMotion(in request, allowSecondaryKnockup: false);
            if (filtered.MotionData.MotionType != HitMotionType.Normal)
            {
                self.InitPhysicalMotion(in filtered);
            }
        }

        private static void HandleKnockdownHit(this HitReactionComponent self, in HitReactionRequest request, in HitReactionRulesConfig reactionRulesConfig)
        {
            var res = reactionRulesConfig.hitInterruptThresholds.Knockdown;

            // 倒地：只有达到门槛才允许“续倒地/打断起身”（避免无限压起身）
            if (request.HitStrength >= res.GetUpInterruptThreshold)
            {
                self.KnockdownEndTime = self.GetCombatNowMs() + self.KnockdownDurationMs;
            }
        }

        private static void HandleGetUpHit(this HitReactionComponent self, in HitReactionRequest request, in HitReactionRulesConfig reactionRulesConfig)
        {
            var res = reactionRulesConfig.hitInterruptThresholds.GetUp;

            // 起身：门槛不够则直接拒绝（不进入受击会话）
            if (request.HitStrength < res.GetUpInterruptThreshold)
            {
                return;
            }

            // 门槛够：打断起身，按地面受击重新评估（可能击飞）
            self.SwitchState(HitState.Grounded);
            self.HandleGroundedHit(in request, in reactionRulesConfig);
        }

        

        private static void EnterAirStun(this HitReactionComponent self, in HitReactionRequest request)
        {
            // 进入终结态：强制结束 AirCombo（防止继续挂空）
            if (self.OwnerUnit != null)
            {
                var airCombo = self.OwnerUnit.GetComponent<AirComboComponent>();
                if (airCombo != null && airCombo.Active)
                {
                    airCombo.ForceEnd();
                }
            }

            self.SwitchState(HitState.AirFinisher);

            // 空中终结仍允许 Push/Pull（但完全禁止 Launch）
            HitReactionRequest filtered = self.FilterAirborneMotion(in request, allowSecondaryKnockup: false);
            if (filtered.MotionData.MotionType != HitMotionType.Normal)
            {
                self.InitPhysicalMotion(in filtered);
            }
        }

        private static void BeginAirSlamToKnockdown(this HitReactionComponent self, in HitReactionRequest request)
        {
            // Slam：设置落地语义为 Knockdown，并注入向下冲量（不改变为空中二次击飞）
            self.SwitchState(self.CurrentHitState == HitState.AirFinisher ? HitState.AirFinisher : HitState.Airborne);
            if (self.Ground != null)
            {
                // 已经在空中时不需要 ForceBreakGround，但需要把落地语义改为 Knockdown
                self.Ground.AirborneReason = AirborneReason.Knockdown;
            }

            if (request.MotionData.MotionType == HitMotionType.KnockDown)
            {
                self.InitPhysicalMotion(in request);
                self.ApplyVerticalImpulse(HitMotionType.KnockDown, request.MotionData.Force);
            }
        }

        private static void EnterAirborneFromGrounded(this HitReactionComponent self, in HitReactionRequest request)
        {
            self.SwitchState(HitState.Airborne);

            if (self.AirCombo == null)
            {
                self.AirCombo = self.OwnerUnit.GetComponent<AirComboComponent>();
            }
            if (self.Ground != null)
            {
                self.Ground.Enable = false;
                var type = HitReactionProfileProvider.ResolveAirborneReasonForRequest(in request);
                self.Ground.ForceBreakGround(type);
            }
            self.InitPhysicalMotion(in request);
            self.ApplyVerticalImpulse(request.MotionData.MotionType, request.MotionData.Force);
            HitReactionProfileProvider.ResolveAirCombo(self.OwnerUnit, out var acProfile);
            self.AirCombo.Enter(self.GetCombatNowMs(), self.OwnerUnit.Position.y, in acProfile);
            
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
                self.OwnerUnit?.GetComponent<AttackComponent>()?.ForceCancel();
                self.OwnerUnit?.GetComponent<AttackCommandComponent>()?.Clear();
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
            if (config.Option.AllowVictimHitStop && request.VictimHitStopMs > 0)
            {
                int ms = Mathf.RoundToInt(request.VictimHitStopMs * Mathf.Max(0f, config.Option.VictimHitStopScale));
                var hitStop = self.OwnerUnit?.GetComponent<HitStopComponent>();
                var anim = self.OwnerUnit?.GetComponent<AnimatorComponent>()?.Animancer;
                Log.Error($"顿帧 {ms}");
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
            self.CurrentMotionType = request.MotionData.MotionType;
            self.MotionDirection = request.HitDirection.sqrMagnitude > 0.0001f ? request.HitDirection.normalized : Vector3.zero;
            self.MotionBaseForce = Mathf.Max(0f, request.MotionData.Force);
            self.CurrentMotionSpeed = self.MotionBaseForce;
            self.CurrentMotionCurve = request.MotionData.MotionCurve;
            self.MotionStartTime = self.GetCombatNowMs();
            self.MotionEndTime = self.MotionStartTime + Mathf.Max(0, request.MotionData.DurationMs);
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
                Log.Error($"ApplyVerticalImpulse  {self.OwnerUnit.UnitName}  {yImpulse}  {intent.ExternalImpulse}");
            }
        }

        #endregion

        #region 内部方法

        private static void UpdateAirborne(this HitReactionComponent self)
        {
            // HitStop 冻结全部运动时，不允许发生“空中→落地→倒地”的状态跃迁（否则会在顿帧窗口出现假落地）
            if (self.HitStop != null && self.HitStop.IsHitStopActive && self.HitStop.FreezeMode == HitStopFreezeMode.FreezeAll)
            {
                return;
            }

            // AirCombo：超时后启动退出（重力 lerp 回 1），退出完成后交给自然重力落地
            if (self.OwnerUnit != null)
            {
                //TODO 检查这里
                var airCombo = self.OwnerUnit.GetComponent<AirComboComponent>();
                if (airCombo != null && airCombo.Active)
                {
                    long now = self.GetCombatNowMs();
                    if (now >= airCombo.AbsoluteEndCombatMs)
                    {
                        airCombo.BeginExit(now);
                    }
                    else if (now >= airCombo.EndCombatMs)
                    {
                        airCombo.BeginExit(now);
                    }

                    if (airCombo.IsExitCompleted(now))
                    {
                        // 退出完成：交给自然物理下落（不再接管）
                        airCombo.ForceEnd();
                    }
                }
            }

            var cc = self.OwnerUnit?.GetComponent<CharacterControllerComponent>();

            // B) 落地触发：避免在 Landing 过渡态的第一帧就触发受击落地（会导致空中受击会话过早结束）。
            // 约束：
            // - PrevState：允许 Airborne 类或 Landing（Landing->Grounded 的收尾帧也要能触发）
            // - State：必须是稳定地面态（排除 Landing 本身）
            if (self.Ground != null &&
                (self.Ground.IsAirborne(self.Ground.PrevState) || self.Ground.PrevState == GroundState.Landing) &&
                self.Ground.State != GroundState.Landing &&
                self.Ground.IsGrounded(self.Ground.State) &&
                (cc == null || cc.CurrentVelocity.y <= 0.01f))
            {
                // Debug：落地触发点（定位“击飞后立刻假落地/会话被提前结束”）
                Log.Info(
                    $"[HitReaction] LandTrigger unit={self.OwnerUnit?.UnitName} state={self.CurrentHitState} " +
                    $"ground={self.Ground.PrevState}->{self.Ground.State} reason={self.Ground.AirborneReason} " +
                    $"vy={(cc != null ? cc.CurrentVelocity.y : 0f):F3} now={self.GetCombatNowMs()}");
                self.OnLand();
            }
        }

        private static void OnLand(this HitReactionComponent self)
        {
            self.OnLanded?.Invoke();
            if (self.CurrentHitState == HitState.Airborne || self.CurrentHitState == HitState.AirFinisher)
            {
                // 落地分流：Launch/Juggled 通常是 LandingStun；Slam/Knockdown 才必倒地
                var reason = self.Ground != null ? self.Ground.AirborneReason : AirborneReason.None;
                if (reason == AirborneReason.Knockdown)
                {
                    self.SwitchState(HitState.Knockdown);
                    self.KnockdownEndTime = self.GetCombatNowMs() + self.KnockdownDurationMs;
                }
                else
                {
                    int landingStunMs = 0;
                    if (self.OwnerUnit != null)
                    {
                        var airCombo = self.OwnerUnit.GetComponent<AirComboComponent>();
                        if (airCombo != null)
                        {
                            landingStunMs = airCombo.LandingStunMs;
                            airCombo.ForceEnd(); // 落地后结束空中连段
                        }
                    }

                    self.SwitchState(HitState.Grounded);
                    self.StunEndTime = self.GetCombatNowMs() + Mathf.Max(0, landingStunMs);
                }
            }
            else
            {
                self.EndHitReaction();
            }
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
            return self.HitStop != null ? self.HitStop.NowCombatMs() : TimeInfo.Instance.ClientFrameTime();
        }

        private static float GetCombatDeltaSeconds(this HitReactionComponent self)
        {
            return self.HitStop != null ? Mathf.Max(0f, self.HitStop.CombatDeltaMs * 0.001f) : Time.deltaTime;
        }
        #endregion
    }
}
