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
            self.LoadAnimationsAsync().NoContext();
        }

        [EntitySystem]
        private static void Destroy(this HitReactionComponent self)
        {
            // 兜底：确保会话级别的外部状态全部释放（避免异常残留锁/地检配置）
            self.ReleaseHitSessionLocksIfNeeded();
            self.ReleaseHitSessionGroundBoostIfNeeded();

            self.CurrentState = HitState.None;
            self.CurrentAnimState = null;
            self.OnHitReactionStart = null;
            self.OnHitReactionEnd = null;
            self.OnLanded = null;
        }

        [EntitySystem]
        private static void Update(this HitReactionComponent self)
        {
            if (!self.IsInHitReaction)
                return;

            // --- 轨道 A：物理运动处理器 (Hit Motion Processor) ---
            // 无论视觉播什么动画，只要物理轨道有数据（Speed > 0），角色就会产生位移
            self.UpdateHitMotion();

            // --- 轨道 B：视觉状态机 (Reaction State Machine) ---
            // 仅处理动画计时、落地判定和状态流转
            switch (self.CurrentState)
            {
                case HitState.Stun:
                case HitState.Knockback:
                    self.UpdateGroundedHitState();
                    break;
                case HitState.Airborne:
                case HitState.Falling:
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
            if (self.CurrentMotionType == HitMotionType.None || self.MotionEndTime <= 0) return;

            long now = self.GetCombatNowMs();
            long durationMs = self.MotionEndTime - self.MotionStartTime;
            
            // 检查运动是否结束
            if (durationMs <= 0 || now >= self.MotionEndTime)
            {
                self.CurrentMotionSpeed = 0f;
                self.CurrentMotionType = HitMotionType.None;
                
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
            // 检查硬直结束 && 物理位移停止（商业级：防止滑行中突然恢复操作带来的瞬移感）
            if (self.GetCombatNowMs() >= self.StunEndTime && self.CurrentMotionSpeed < 0.1f)
            {
                self.EndHitReaction();
            }
        }

        /// <summary>
        /// 加载动画资源
        /// </summary>
        private static async ETTask LoadAnimationsAsync(this HitReactionComponent self)
        {
            // 加载受击动画（根据实际资源路径调整）
            await ETTask.CompletedTask;
        }

        #region 受击类型处理（快捷接口）
        
        public static void PlayLightHit(this HitReactionComponent self, Vector3 direction, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Light,
                HitMotionData.Default,
                TargetStateMask.Any,
                direction,
                hitStunMs: stunMs));
        }

        public static void PlayMediumHit(this HitReactionComponent self, Vector3 direction, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Medium,
                HitMotionData.Default,
                TargetStateMask.Any,
                direction,
                hitStunMs: stunMs));
        }

        public static void PlayHeavyHit(this HitReactionComponent self, Vector3 direction, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Heavy,
                HitMotionData.Default,
                TargetStateMask.Any,
                direction,
                hitStunMs: stunMs));
        }

        public static void PlayKnockback(this HitReactionComponent self, Vector3 direction, float force, int durationMs, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Medium,
                new HitMotionData { MotionType = HitMotionType.Push, Force = force, DurationMs = durationMs, MotionCurve = AnimationCurve.Linear(0, 1, 1, 0) },
                TargetStateMask.Any,
                direction,
                hitStunMs: stunMs));
        }

        #endregion

        #region 统一入口

        /// <summary>
        /// 尝试对目标应用一次“受击请求”。
        /// </summary>
        public static bool TryApplyHit(this HitReactionComponent self, in HitReactionRequest request)
        {
            HitProfileLibrary.Resolve(self?.OwnerUnit, out var rulesProfile, out var feedbackProfile);
            var result = HitRules.Evaluate(self, in request, in rulesProfile);
            if (!result.Accepted)
            {
                return false;
            }

            HitReactionRequest effective = result.Request;

            // AirCombo：命中续期（KeepAlive）——只续期，不叠加高度
            var unit = self.OwnerUnit;
            if (unit != null)
            {
                var airCombo = unit.GetComponent<AirComboComponent>();
                if (airCombo != null && airCombo.Active)
                {
                    HitProfileLibrary.ResolveAirCombo(unit, out var acProfile);
                    airCombo.OnHit(self.GetCombatNowMs(), in acProfile);
                }
            }

            // 反馈：目标侧顿帧
            self.ApplyFeedback(in effective, in feedbackProfile);

            // 按规则决策执行
            switch (result.Mode)
            {
                case HitRules.ApplyMode.Replace:
                    // 会话式锁：仅在“从非受击进入受击”时 acquire 一次
                    self.AcquireHitSessionLocksIfNeeded();
                    self.StartHitReaction(in effective);
                    break;
                case HitRules.ApplyMode.Refresh:
                    // Refresh 仍处于受击会话内；但如果存在“反馈-only”后立刻进入受击的特殊路径，这里也要兜底 acquire
                    self.AcquireHitSessionLocksIfNeeded();
                    self.RefreshHitReaction(in effective);
                    break;
                case HitRules.ApplyMode.FeedbackOnly:
                    // 仅反馈，不进入受击状态机，也不 acquire 外部能力锁
                    return true;
                default:
                    return false;
            }
            return true;
        }

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

        private static void ApplyFeedback(this HitReactionComponent self, in HitReactionRequest request, in HitFeedbackProfile profile)
        {
            if (profile.Option.AllowVictimHitStop && request.VictimHitStopMs > 0)
            {
                int ms = Mathf.RoundToInt(request.VictimHitStopMs * Mathf.Max(0f, profile.Option.VictimHitStopScale));
                var hitStop = self.OwnerUnit?.GetComponent<HitStopComponent>();
                var anim = self.OwnerUnit?.GetComponent<AnimatorComponent>()?.Animancer;
                if (hitStop != null && anim != null)
                {
                    hitStop.RequestHitStop(ms, anim, HitStopFreezeMode.FreezeAll);
                }
            }
        }

        #region HitState 映射（单一权威）

        /// <summary>
        /// 由请求（以及当前状态）决定受击状态机要进入/保持的 <see cref="HitState"/>。
        /// 约束：
        /// - <see cref="HitMotionType"/> 负责“运动学怎么做”，<see cref="HitState"/> 负责“状态机与动画怎么播/怎么流转”
        /// - 两者分离，但“由请求推导状态”的规则必须集中在这里，避免 Start/Refresh 各写一套导致维护分叉
        /// </summary>
        private static HitState ResolveHitStateForStart(in HitReactionRequest request)
        {
            switch (request.MotionData.MotionType)
            {
                case HitMotionType.Launch:
                case HitMotionType.Slam:
                    return HitState.Airborne;
                case HitMotionType.Push:
                case HitMotionType.Pull:
                    return HitState.Knockback;
                default:
                    return HitState.Stun;
            }
        }

        private static HitState ResolveHitStateForRefresh(in HitReactionRequest request, HitState current)
        {
            // 处于 Falling 时，不要被 refresh 拉回 Airborne（避免倒序）
            if (current == HitState.Falling)
            {
                return HitState.Falling;
            }

            bool currentlyAirborne = current == HitState.Airborne || current == HitState.Falling;
            bool isLaunchOrSlam = request.MotionData.MotionType == HitMotionType.Launch || request.MotionData.MotionType == HitMotionType.Slam;

            if (isLaunchOrSlam || currentlyAirborne)
            {
                return HitState.Airborne;
            }

            switch (request.MotionData.MotionType)
            {
                case HitMotionType.Push:
                case HitMotionType.Pull:
                    return HitState.Knockback;
                default:
                    return HitState.Stun;
            }
        }

        private static AirborneReason ResolveAirborneReasonForRequest(in HitReactionRequest request, bool isRefresh)
        {
            // Slam 通常意味着“砸地/击倒”语义；Launch 表示击飞；Refresh(空中追击)默认 Juggled
            if (request.MotionData.MotionType == HitMotionType.Slam)
            {
                return AirborneReason.Knockdown;
            }
            if (request.MotionData.MotionType == HitMotionType.Launch)
            {
                return AirborneReason.Launched;
            }
            return isRefresh ? AirborneReason.Juggled : AirborneReason.Launched;
        }

        #endregion

        private static void StartHitReaction(this HitReactionComponent self, in HitReactionRequest request)
        {
            // --- 1. 初始化视觉轨道 ---
            self.CurrentReactionType = request.ReactionType;
            self.StunEndTime = self.GetCombatNowMs() + Mathf.Max(0, request.HitStunMs);

            HitState state = ResolveHitStateForStart(in request);

            // --- 2. 初始化物理轨道 ---
            self.InitPhysicalMotion(in request);

            // --- 3. 物理驱动状态修正 ---
            if (state == HitState.Airborne)
            {
                self.AcquireHitSessionGroundBoostIfNeeded();
                // Y轴权威：只有 Launch/Slam 才能注入垂直冲量；Push/Pull 的连段维持由 ComboPhysics 接管
                if (request.MotionData.MotionType == HitMotionType.Launch || request.MotionData.MotionType == HitMotionType.Slam)
                {
                    self.ApplyVerticalImpulse(request.MotionData.MotionType, request.MotionData.Force);
                }

                if (self.Ground != null)
                {
                    // 最优：击飞/砸地必须“立刻离地”，不能等待地检防抖窗口，否则会出现“受击已空中但地检仍在地面→假落地→倒地”
                    self.Ground.ForceBreakGround(ResolveAirborneReasonForRequest(in request, isRefresh: false));
                }

                // AirCombo：进入空中连段物理模式（由 profile 决定是否启用）
                if (self.OwnerUnit != null)
                {
                    var airCombo = self.OwnerUnit.GetComponent<AirComboComponent>();
                    if (airCombo != null)
                    {
                        HitProfileLibrary.ResolveAirCombo(self.OwnerUnit, out var acProfile);
                        airCombo.Enter(self.GetCombatNowMs(), self.OwnerUnit.Position.y, in acProfile);
                    }
                }
            }

            self.CurrentState = state;
            self.OnHitReactionStart?.Invoke(state);
        }

        private static void RefreshHitReaction(this HitReactionComponent self, in HitReactionRequest request)
        {
            // 1. 视觉刷新
            self.CurrentReactionType = request.ReactionType;
            long stunEnd = self.GetCombatNowMs() + Mathf.Max(0, request.HitStunMs);
            if (stunEnd > self.StunEndTime) self.StunEndTime = stunEnd;

            // 2. 物理刷新
            if (request.MotionData.MotionType != HitMotionType.None)
            {
                // 使用 BaseForce 判定覆盖，保证物理表现的连续性
                if (request.MotionData.Force >= self.MotionBaseForce)
                {
                    self.InitPhysicalMotion(in request);
                }

                HitState resolved = ResolveHitStateForRefresh(in request, self.CurrentState);
                if (resolved == HitState.Airborne)
                {
                    if (!self.IsAirborne)
                    {
                        self.CurrentState = HitState.Airborne;
                        self.OnHitReactionStart?.Invoke(HitState.Airborne);
                    }

                    self.AcquireHitSessionGroundBoostIfNeeded();
                    if (request.MotionData.MotionType == HitMotionType.Launch || request.MotionData.MotionType == HitMotionType.Slam)
                    {
                        self.ApplyVerticalImpulse(request.MotionData.MotionType, request.MotionData.Force);
                    }

                    if (self.Ground != null)
                    {
                        // 最优：确保地检立刻离地（但只在“当前还被认为在地面”时强制），避免 Refresh 重复重置离地时间/跌落高度基准
                        if (!self.Ground.IsAirborne(self.Ground.State))
                        {
                            self.Ground.ForceBreakGround(ResolveAirborneReasonForRequest(in request, isRefresh: true));
                        }
                        else
                        {
                            self.Ground.AirborneReason = ResolveAirborneReasonForRequest(in request, isRefresh: true);
                        }
                    }

                    // AirCombo：空中刷新也视为 Enter/KeepAlive（Enter 内部会做合并与 fail-safe）
                    if (self.OwnerUnit != null)
                    {
                        var airCombo = self.OwnerUnit.GetComponent<AirComboComponent>();
                        if (airCombo != null)
                        {
                            HitProfileLibrary.ResolveAirCombo(self.OwnerUnit, out var acProfile);
                            if (!airCombo.Active)
                            {
                                airCombo.Enter(self.GetCombatNowMs(), self.OwnerUnit.Position.y, in acProfile);
                            }
                            else
                            {
                                airCombo.OnHit(self.GetCombatNowMs(), in acProfile);
                            }
                        }
                    }
                }
                else
                {
                    if (self.CurrentState != resolved)
                    {
                        self.CurrentState = resolved;
                        self.OnHitReactionStart?.Invoke(resolved);
                    }
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
                float yImpulse = (motionType == HitMotionType.Slam) ? -force : force;
                intent.ExternalImpulse += new Vector3(0, yImpulse, 0);
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
            if (cc != null && cc.CurrentVelocity.y < -0.01f && self.CurrentState == HitState.Airborne)
            {
                self.CurrentState = HitState.Falling;
            }

            // 最优：落地必须是“地检从 airborne 类状态切到 grounded 类状态”的跃迁，而不是某一帧 Ground.State 读到 grounded
            if (self.Ground != null &&
                self.Ground.IsAirborne(self.Ground.PrevState) &&
                self.Ground.IsGrounded(self.Ground.State) &&
                (cc == null || cc.CurrentVelocity.y <= 0.01f))
            {
                self.OnLand();
            }
        }

        private static void OnLand(this HitReactionComponent self)
        {
            self.OnLanded?.Invoke();
            if (self.CurrentState == HitState.Falling || self.CurrentState == HitState.Airborne)
            {
                // 落地分流：Launch/Juggled 通常是 LandingStun；Slam/Knockdown 才必倒地
                var reason = self.Ground != null ? self.Ground.AirborneReason : AirborneReason.None;
                if (reason == AirborneReason.Knockdown)
                {
                    self.CurrentState = HitState.Knockdown;
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

                    self.CurrentState = HitState.Stun;
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
                self.CurrentState = HitState.GetUp;
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
            if (self.CurrentAnimState != null && self.CurrentAnimState.NormalizedTime >= 0.9f)
            {
                self.EndHitReaction();
            }
        }

        private static void EndHitReaction(this HitReactionComponent self)
        {
            self.CurrentState = HitState.None;
            self.CurrentAnimState = null;
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

            self.ReleaseHitSessionGroundBoostIfNeeded();
            self.OnHitReactionEnd?.Invoke();
        }

        #region 地面检测协作
        private static void AcquireHitSessionGroundBoostIfNeeded(this HitReactionComponent self)
        {
            if (self == null)
            {
                return;
            }
            if (self.HitSessionGroundBoosted)
            {
                return;
            }
            self.HitSessionGroundBoosted = true;

            if (self.Ground != null)
            {
                self.Ground.InhibitReduceFrequencyCount++;
            }
        }

        private static void ReleaseHitSessionGroundBoostIfNeeded(this HitReactionComponent self)
        {
            if (self == null)
            {
                return;
            }
            if (!self.HitSessionGroundBoosted)
            {
                return;
            }
            self.HitSessionGroundBoosted = false;

            if (self.Ground != null && self.Ground.InhibitReduceFrequencyCount > 0)
            {
                self.Ground.InhibitReduceFrequencyCount--;
            }
        }
        #endregion

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
