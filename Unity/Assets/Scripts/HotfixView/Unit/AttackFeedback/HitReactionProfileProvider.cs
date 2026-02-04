using UnityEngine;

namespace ET
{
    /// <summary>
    /// Profile 选择与默认值（先硬编码默认，后续可由表/资产覆盖）。
    /// </summary>
    public static class HitReactionProfileProvider
    {
        public static void ResolveConfig(Unit unit, out HitReactionConfig reaction, out HitFeedbackConfig feedback)
        {
            ResolveConfig(unit, null, out reaction, out feedback);
        }

        public static void ResolveConfig(Unit unit, CombatConfigComponent combat, out HitReactionConfig reaction, out HitFeedbackConfig feedback)
        {
            if (TryGetProfile(unit, combat, out combat, out var profile))
            {
                EnsureCache(combat, profile);
                reaction = combat.cached;
                feedback = combat.CachedFeedback;
                return;
            }

            LogMissingProfileOnce(unit, combat);
            reaction = Default;
            feedback = DefaultFeedback;
        }

        public static void ResolveAirCombo(Unit unit, out HitAirComboProfile airCombo)
        {
            ResolveAirCombo(unit, null, out airCombo);
        }

        public static void ResolveAirCombo(Unit unit, CombatConfigComponent combat, out HitAirComboProfile airCombo)
        {
            if (TryGetProfile(unit, combat, out combat, out var profile))
            {
                EnsureCache(combat, profile);
                airCombo = combat.CachedAirCombo;
                return;
            }

            LogMissingProfileOnce(unit, combat);
            airCombo = DefaultAirCombo;
        }

        private static bool TryGetProfile(Unit unit, CombatConfigComponent combat, out CombatConfigComponent resolvedCombat, out HitReactionProfileAsset profile)
        {
            resolvedCombat = combat ?? unit?.GetComponent<CombatConfigComponent>();
            profile = resolvedCombat?.HitReactionAsset;
            return resolvedCombat != null && profile != null;
        }

        internal static HitReactionConfig BuildRules(in HitReactionRulesData data)
        {
            var thresholds = data.InterruptThresholds;

            HitReactionConfig.HitInterruptThresholds hitInterrupt =
                new HitReactionConfig.HitInterruptThresholds(
                    thresholds.Grounded,
                    thresholds.Airborne,
                    thresholds.AirFinisher,
                    thresholds.Knockdown,
                    thresholds.GetUp);

            var scales = new HitReactionConfig.Scales(
                data.Scales.Stun,
                data.Scales.Knockback,
                data.Scales.Knockup);

            var limits = new HitReactionConfig.Limits(
                data.Limits.MaxHitStunMs,
                data.Limits.MaxKnockbackForce,
                data.Limits.MaxKnockupForce);

            return new HitReactionConfig(
                data.AllowedReactionGroups,
                data.AllowedStateVisuals,
                hitInterrupt,
                scales,
                limits);
        }

        internal static HitFeedbackConfig BuildFeedback(in HitReactionFeedbackData data)
        {
            return new HitFeedbackConfig(new HitFeedbackConfig.Options(
                data.Option.AllowVictimHitStop,
                data.Option.VictimHitStopScale,
                data.Option.AllowScreenShake,
                data.Option.ScreenShakeScale,
                data.Option.AllowTimeScale,
                data.Option.TimeScaleScale));
        }

        internal static HitAirComboProfile BuildAirCombo(in HitAirComboData data)
        {
            return new HitAirComboProfile(
                data.Enable,
                data.MinAirTimeMs,
                data.MaxTotalHangMs,
                data.GravityScaleDuringCombo,
                data.MinFallSpeedAbs,
                data.MinHeightOffset,
                data.MaxHeightOffset,
                data.ExitLerpMs,
                data.LandingStunMs,
                data.MaxAirborneKnockupForce,
                data.MaxAirHorizontalDistance,
                data.MaxAirHorizontalSpeed,
                data.RecenterStrength,
                data.RecenterDeadZone);
        }

        private static readonly HitReactionConfig Default = new HitReactionConfig(
            HitReactionGroup.All,
            HitStateVisualMask.All,
            new HitReactionConfig.HitInterruptThresholds(
                new GroundedThresholdData(20, 30, 50, 60),
                new AirborneThresholdData(70, 60),
                new AirFinisherThresholdData(65),
                new KnockdownThresholdData(80),
                new GetUpThresholdData(80)),
            new HitReactionConfig.Scales(1f, 1f, 1f),
            new HitReactionConfig.Limits(int.MaxValue, float.MaxValue, float.MaxValue));

        private static readonly HitFeedbackConfig DefaultFeedback = new HitFeedbackConfig(
            new HitFeedbackConfig.Options(
                allowVictimHitStop: false,
                victimHitStopScale: 0f,
                allowScreenShake: false,
                screenShakeScale: 0f,
                allowTimeScale: false,
                timeScaleScale: 0f));

        private static readonly HitAirComboProfile DefaultAirCombo = new HitAirComboProfile(
            enable: false,
            minAirTimeMs: 0,
            maxTotalHangMs: 0,
            gravityScaleDuringCombo: 1f,
            minFallSpeedAbs: 0f,
            minHeightOffset: 0f,
            maxHeightOffset: 0f,
            exitLerpMs: 0,
            landingStunMs: 0,
            maxAirborneKnockupForce: 0f,
            maxHorizontalDistance: 0f,
            maxHorizontalSpeed: 0f,
            recenterStrength: 0f,
            recenterDeadZone: 0f);

        private static void EnsureCache(CombatConfigComponent combat, HitReactionProfileAsset profile)
        {
            if (combat.CacheReady)
            {
                return;
            }
            combat.cached = BuildRules(profile.Rules);
            combat.CachedFeedback = BuildFeedback(profile.Feedback);
            combat.CachedAirCombo = BuildAirCombo(profile.AirCombo);
            combat.CacheReady = true;
        }

        private static void LogMissingProfileOnce(Unit unit, CombatConfigComponent combat)
        {
            var resolvedCombat = combat ?? unit?.GetComponent<CombatConfigComponent>();
            if (resolvedCombat == null || resolvedCombat.MissingProfileLogged)
            {
                return;
            }
            resolvedCombat.MissingProfileLogged = true;
            Log.Error($"[HitReactionProfileProvider] 未找到 HitReactionProfileAsset, Unit={unit?.UnitName}");
        }
        
        public static AirborneReason ResolveAirborneReasonForRequest(in HitReactionRequest request)
        {
            // Slam 通常意味着“砸地/击倒”语义；Launch 表示击飞；Refresh(空中追击)默认 Juggled

            AirborneReason type = AirborneReason.None;
            switch (request.Rule.MotionData.MotionType)
            {
                case HitMotionType.Normal:
                case HitMotionType.Knockback:
                case HitMotionType.PullTowardAttacker:
                    type = AirborneReason.Juggled;
                    break;
                case HitMotionType.Knockup:
                    type = AirborneReason.Launched;
                    break;
                case HitMotionType.KnockDown:
                    type = AirborneReason.Knockdown;
                    break;
            }

            return type;
        }
        
        public static HitReactionRequest From(
            in HitEffectData effect,
            in HitFeedbackData feedback,
            Vector3 hitDirection,
            int defaultHitStopMs,
            int attackerSegmentComboTimeoutMs = 0,
            float attackRadius = 0f,
            Vector3 attackerWorldPos = default,
            bool hasAttackerWorldPos = false)
        {
            var airCombo = new HitReactionRequest.AirComboHint(
                attackerSegmentComboTimeoutMs,
                attackRadius,
                attackerWorldPos,
                hasAttackerWorldPos: hasAttackerWorldPos);
            return new HitReactionRequest(effect, feedback, hitDirection, defaultHitStopMs, airCombo);
        }
        
        private static byte GetDefaultPriority(HitReactionType type)
        {
            // 仅作为“未配置 Priority 的兜底规则”，数值可以后续完全由策划配置覆盖。
            switch (type)
            {
                case HitReactionType.MinorHit: return 10;
                case HitReactionType.MediumHit: return 20;
                case HitReactionType.MajorHit: return 40;
                case HitReactionType.StaggerHit: return 60;
                case HitReactionType.StunHit: return 80;
                default: return 0;
            }
        }
        
        public static HitReactionRequest Normalize(this HitReactionComponent self, in HitReactionRequest r, in HitReactionConfig config)
        {
            Vector3 dir = r.Rule.HitDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
            }

            int stun = Mathf.Max(0, r.Rule.HitStunMs);
            if (config.Rule.Scale.Stun > 0f && !Mathf.Approximately(config.Rule.Scale.Stun, 1f))
            {
                stun = Mathf.RoundToInt(stun * config.Rule.Scale.Stun);
            }
            if (config.Rule.Limit.MaxHitStunMs > 0 && stun > config.Rule.Limit.MaxHitStunMs)
            {
                stun = config.Rule.Limit.MaxHitStunMs;
            }

            // 物理轨道归一化
            HitMotionData motion = r.Rule.MotionData;
            motion.Force = Mathf.Max(0f, motion.Force);
            
            // 根据 Profile 缩放和限制力
            if (motion.MotionType == HitMotionType.Knockback || motion.MotionType == HitMotionType.PullTowardAttacker)
            {
                if (config.Rule.Scale.Knockback > 0f && !Mathf.Approximately(config.Rule.Scale.Knockback, 1f)) motion.Force *= config.Rule.Scale.Knockback;
                if (config.Rule.Limit.MaxKnockbackForce > 0f && motion.Force > config.Rule.Limit.MaxKnockbackForce) motion.Force = config.Rule.Limit.MaxKnockbackForce;
            }
            else if (motion.MotionType == HitMotionType.Knockup || motion.MotionType == HitMotionType.KnockDown)
            {
                if (config.Rule.Scale.Knockup > 0f && !Mathf.Approximately(config.Rule.Scale.Knockup, 1f)) motion.Force *= config.Rule.Scale.Knockup;
                if (config.Rule.Limit.MaxKnockupForce >= 0f && motion.Force > config.Rule.Limit.MaxKnockupForce) motion.Force = config.Rule.Limit.MaxKnockupForce;
            }

            int hitStop = Mathf.Max(0, r.Feedback.VictimHitStopMs);
            float shakeIntensity = Mathf.Max(0f, r.Feedback.ScreenShakeIntensity);
            int shakeDurationMs = Mathf.Max(0, r.Feedback.ScreenShakeDurationMs);
            float timeScale = r.Feedback.TimeScale <= 0f ? 1f : r.Feedback.TimeScale;
            int timeScaleMs = Mathf.Max(0, r.Feedback.TimeScaleDurationMs);

            byte hitStrength = r.Rule.HasHitStrength ? r.Rule.HitStrength : GetDefaultPriority(r.Rule.ReactionType);
            var normalizedRule = new HitReactionRequest.HitRuleData(
                r.Rule.ReactionType,
                hitStrength,
                true,
                motion,
                r.Rule.TargetStates,
                dir,
                stun);
            var normalizedFeedback = new HitReactionRequest.HitFeedbackRequestData(
                hitStop,
                shakeIntensity,
                shakeDurationMs,
                timeScale,
                timeScaleMs);

            return new HitReactionRequest(normalizedRule, normalizedFeedback, r.AirCombo);
        }
        
        public static bool PassTargetStateFilter(this HitReactionComponent self, TargetStateMask filter)
        {
            if (filter == TargetStateMask.Any)
            {
                return true;
            }

            TargetStateMask current;
            if (self.IsKnockdown)
            {
                current = TargetStateMask.Knockdown;
            }
            else if (self.IsAirborne)
            {
                current = TargetStateMask.Airborne;
            }
            else
            {
                current = TargetStateMask.Grounded;
            }

            return (filter & current) != 0;
        }
        
        /// <summary>
        /// 过滤空中受击时的运动类型。Knockup 根据 allowSecondaryKnockup 和 MaxAirborneKnockupForce 决定是否允许、以及力上限。
        /// </summary>
        /// <param name="allowSecondaryKnockup">Airborne 时为 true 允许 capped 二次击飞；AirFinisher 时为 false 完全禁止。</param>
        public static HitReactionRequest FilterAirborneMotion(this HitReactionComponent self, in HitReactionRequest request, bool allowSecondaryKnockup = true)
        {
            if (request.Rule.MotionData.MotionType != HitMotionType.Knockup)
            {
                return request;
            }

            if (!allowSecondaryKnockup)
            {
                // 完全禁止二次击飞
                HitMotionData m = request.Rule.MotionData;
                m.MotionType = HitMotionType.Normal;
                m.Force = 0f;
                m.DurationMs = 0;
                return BuildFilteredRequest(in request, m);
            }

            ResolveAirCombo(self.OwnerUnit, out var profile);
            if (profile.MaxAirborneKnockupForce <= 0f)
            {
                // 配置为 0：禁止
                HitMotionData m = request.Rule.MotionData;
                m.MotionType = HitMotionType.Normal;
                m.Force = 0f;
                m.DurationMs = 0;
                return BuildFilteredRequest(in request, m);
            }

            // 允许二次击飞，夹持力上限
            HitMotionData capped = request.Rule.MotionData;
            capped.Force = Mathf.Min(capped.Force, profile.MaxAirborneKnockupForce);
            return BuildFilteredRequest(in request, capped);
        }

        private static HitReactionRequest BuildFilteredRequest(in HitReactionRequest request, HitMotionData motionData)
        {
            var rule = new HitReactionRequest.HitRuleData(
                request.Rule.ReactionType,
                request.Rule.HitStrength,
                request.Rule.HasHitStrength,
                motionData,
                request.Rule.TargetStates,
                request.Rule.HitDirection,
                request.Rule.HitStunMs);
            return new HitReactionRequest(rule, request.Feedback, request.AirCombo);
        }
        
        private static HitReactionGroup ToGroup(HitReactionType type)
        {
            return type switch
            {
                HitReactionType.MinorHit => HitReactionGroup.Minor,
                HitReactionType.MediumHit => HitReactionGroup.Minor,
                HitReactionType.MajorHit => HitReactionGroup.Major,
                HitReactionType.StaggerHit => HitReactionGroup.Control,
                HitReactionType.StunHit => HitReactionGroup.Control,
                _ => HitReactionGroup.None
            };
        }

        public static HitReactionType DegradeReactionType(this HitReactionComponent self,HitReactionType desired, HitReactionGroup allowed)
        {
            if (desired == HitReactionType.None)
            {
                return HitReactionType.None;
            }

            HitReactionGroup g = ToGroup(desired);
            if (g != HitReactionGroup.None && (allowed & g) != 0)
            {
                return desired;
            }

            // 降级链：Control → Major → Minor → None
            if (g == HitReactionGroup.Control)
            {
                return self.DegradeReactionType(HitReactionType.MajorHit, allowed);
            }
            if (g == HitReactionGroup.Major)
            {
                return self.DegradeReactionType(HitReactionType.MediumHit, allowed);
            }
            if (g == HitReactionGroup.Minor)
            {
                return HitReactionType.None;
            }

            return HitReactionType.None;
        }
    }
}

