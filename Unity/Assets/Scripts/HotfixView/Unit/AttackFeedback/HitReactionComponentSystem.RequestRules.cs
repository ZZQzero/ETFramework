using UnityEngine;

namespace ET
{
    /// <summary>
    /// HitReaction 请求归一化/过滤/视觉降级等规则辅助。
    /// 说明：这些规则属于受击状态机域，而非配置解析域。
    /// </summary>
    public static partial class HitReactionComponentSystem
    {
        private static byte GetDefaultPriority(HitReactionType type)
        {
            // 仅作为“未配置 HitStrength 的兜底规则”，数值可以后续完全由策划配置覆盖。
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

        public static HitImpactData Normalize(this HitReactionComponent self, in HitImpactData r, in HitReactionConfig config)
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
                if (config.Rule.Scale.Knockup > 0f && !Mathf.Approximately(config.Rule.Scale.Knockup, 1f))
                {
                    motion.Force *= config.Rule.Scale.Knockup;
                }
                if (config.Rule.Limit.MaxKnockupForce >= 0f && motion.Force > config.Rule.Limit.MaxKnockupForce)
                {
                    motion.Force = config.Rule.Limit.MaxKnockupForce;
                }
            }

            int hitStop = Mathf.Max(0, r.Feedback.VictimHitStopMs);
            float shakeIntensity = Mathf.Max(0f, r.Feedback.ScreenShakeIntensity);
            int shakeDurationMs = Mathf.Max(0, r.Feedback.ScreenShakeDurationMs);
            float timeScale = r.Feedback.TimeScale <= 0f ? 1f : r.Feedback.TimeScale;
            int timeScaleMs = Mathf.Max(0, r.Feedback.TimeScaleDurationMs);

            byte hitStrength = r.Rule.HasHitStrength ? r.Rule.HitStrength : GetDefaultPriority(r.Rule.ReactionType);
            var normalizedRule = new HitImpactData.HitRuleData(
                r.Rule.ReactionType,
                hitStrength,
                true,
                motion,
                r.Rule.TargetStates,
                dir,
                stun);
            var normalizedFeedback = new HitImpactData.HitFeedbackRequestData(
                hitStop,
                shakeIntensity,
                shakeDurationMs,
                timeScale,
                timeScaleMs);

            return new HitImpactData(normalizedRule, normalizedFeedback, r.AirCombo);
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
        /// 过滤空中受击时的运动类型。
        /// - Knockup：根据 allowSecondaryKnockup + MaxAirborneKnockupForce 决定是否允许，以及力上限。
        /// </summary>
        /// <param name="allowSecondaryKnockup">Airborne 时 true 允许 capped 二次击飞；AirFinisher 时 false 完全禁止。</param>
        public static HitImpactData FilterAirborneMotion(this HitReactionComponent self, in HitImpactData request, bool allowSecondaryKnockup = true)
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
            
            if (self.CombatConfig.CachedAirCombo.MaxAirborneKnockupForce <= 0f)
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
            capped.Force = Mathf.Min(capped.Force, self.CombatConfig.CachedAirCombo.MaxAirborneKnockupForce);
            return BuildFilteredRequest(in request, capped);
        }

        private static HitImpactData BuildFilteredRequest(in HitImpactData request, HitMotionData motionData)
        {
            var rule = new HitImpactData.HitRuleData(
                request.Rule.ReactionType,
                request.Rule.HitStrength,
                request.Rule.HasHitStrength,
                motionData,
                request.Rule.TargetStates,
                request.Rule.HitDirection,
                request.Rule.HitStunMs);
            return new HitImpactData(rule, request.Feedback, request.AirCombo);
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

        public static HitReactionType DegradeReactionType(this HitReactionComponent self, HitReactionType desired, HitReactionGroup allowed)
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

        public static AirborneReason ResolveAirborneReasonForRequest(in HitImpactData request)
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
    }
}

