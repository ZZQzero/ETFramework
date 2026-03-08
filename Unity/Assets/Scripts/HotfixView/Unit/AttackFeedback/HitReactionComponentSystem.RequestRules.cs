using UnityEngine;

namespace ET
{
    public static partial class HitReactionComponentSystem
    {
        /// <summary>
        /// 归一化：对 Rule 应用缩放和钳制（Feedback 透传，由消费端自行处理）。
        /// </summary>
        public static HitImpactData Normalize(this HitReactionComponent self, in HitImpactData impactData, in HitReactionConfig reaction)
        {
            int stun = Mathf.Min(impactData.Rule.HitStunMs, reaction.Rule.Limit.MaxHitStunMs);

            HitMotionData motion = impactData.Rule.MotionData;

            switch (motion.MotionType)
            {
                case HitMotionType.NormalHit:
                case HitMotionType.UpwardImpulse:
                case HitMotionType.DownwardImpulse:
                    motion.Force = Mathf.Min(motion.Force * reaction.Rule.Scale.Knockup, reaction.Rule.Limit.MaxKnockupForce);
                    break;
                case HitMotionType.HorizontalImpulse:
                case HitMotionType.TowardAttacker:
                case HitMotionType.CustomCurve:
                    motion.Force = Mathf.Min(motion.Force * reaction.Rule.Scale.Knockback, reaction.Rule.Limit.MaxKnockbackForce);
                    break;
            }

            var normalizedRule = new HitImpactData.HitRuleData(
                motion,
                impactData.Rule.TargetStates,
                impactData.Rule.HitDirection,
                stun,
                impactData.Rule.AttackerSegmentTimeoutMs);

            return new HitImpactData(normalizedRule, impactData.Feedback);
        }

        public static bool PassTargetStateFilter(this HitReactionComponent self, TargetStateMask filter)
        {
            if (filter == TargetStateMask.Any)
                return true;

            TargetStateMask current = self.CurrentHitState switch
            {
                HitState.KnockdownHit => TargetStateMask.Knockdown,
                HitState.AirborneHit => TargetStateMask.Airborne,
                _ => TargetStateMask.Grounded
            };

            return (filter & current) != 0;
        }

        public static AirborneReason ResolveAirborneReasonForRequest(in HitImpactData request)
        {
            return request.Rule.MotionData.MotionType switch
            {
                HitMotionType.UpwardImpulse => AirborneReason.Launched,
                HitMotionType.TowardAttacker => AirborneReason.Knockdown,
                _ => AirborneReason.Juggled
            };
        }
    }
}
