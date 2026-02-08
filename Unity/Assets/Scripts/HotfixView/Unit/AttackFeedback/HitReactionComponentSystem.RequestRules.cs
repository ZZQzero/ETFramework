using System;
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
                case HitReactionType.LightHit: return 10;
                case HitReactionType.HeavyHit: return 20;
                case HitReactionType.Launch: return 40;
                case HitReactionType.AirCombo: return 60;
                case HitReactionType.SlamDown: return 80;
                default: return 0;
            }
        }

        public static HitImpactData Normalize(this HitReactionComponent self, in HitImpactData impactData, in HitReactionConfig reaction)
        {
            int stun = Mathf.Min(impactData.Rule.HitStunMs, reaction.Rule.Limit.MaxHitStunMs);

            // 物理轨道归一化
            HitMotionData motion = impactData.Rule.MotionData;

            switch (motion.MotionType)
            {
                case HitMotionType.NormalHit:
                    // NormalHit：水平位移由拴系弹簧驱动，Force 仅空中用作向上冲量
                    // 按 Knockup 缩放（与 UpwardImpulse 一致），地面时 Force 不参与运动
                    motion.Force *= reaction.Rule.Scale.Knockup;
                    motion.Force = Mathf.Min(motion.Force, reaction.Rule.Limit.MaxKnockupForce);
                    break;
                case HitMotionType.HorizontalImpulse:
                case HitMotionType.TowardAttacker:
                case HitMotionType.CustomCurve:
                    // 水平类运动（击退、拉拽、自定义曲线）使用 Knockback 缩放
                    motion.Force *= reaction.Rule.Scale.Knockback;
                    motion.Force = Mathf.Min(motion.Force, reaction.Rule.Limit.MaxKnockbackForce);
                    break;
                case HitMotionType.UpwardImpulse:
                case HitMotionType.DownwardImpulse:
                    motion.Force *= reaction.Rule.Scale.Knockup;
                    motion.Force = Mathf.Min(motion.Force, reaction.Rule.Limit.MaxKnockupForce);
                    break;
            }

            int hitStop = Mathf.Max(0, impactData.Feedback.VictimHitStopMs);
            float shakeIntensity = Mathf.Max(0f, impactData.Feedback.ScreenShakeIntensity);
            int shakeDurationMs = Mathf.Max(0, impactData.Feedback.ScreenShakeDurationMs);
            float timeScale = impactData.Feedback.TimeScale <= 0f ? 1f : impactData.Feedback.TimeScale;
            int timeScaleMs = Mathf.Max(0, impactData.Feedback.TimeScaleDurationMs);

            byte hitStrength = impactData.Rule.HasHitStrength ? impactData.Rule.HitStrength : GetDefaultPriority(impactData.Rule.ReactionType);
            var normalizedRule = new HitImpactData.HitRuleData(
                impactData.Rule.ReactionType,
                hitStrength,
                true,
                motion,
                impactData.Rule.TargetStates,
                impactData.Rule.HitDirection,
                impactData.Rule.AttackRadius,
                impactData.Rule.AttackerWorldPos,
                impactData.Rule.HasAttackerWorldPos,
                stun,
                impactData.Rule.AttackerSegmentTimeoutMs,
                impactData.Rule.AttackTotalTimeoutMs);
            var normalizedFeedback = new HitImpactData.HitFeedbackRequestData(
                hitStop,
                shakeIntensity,
                shakeDurationMs,
                timeScale,
                timeScaleMs);

            return new HitImpactData(normalizedRule, normalizedFeedback);
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

        private static HitReactionGroup ToGroup(HitReactionType type)
        {
            return type switch
            {
                HitReactionType.LightHit => HitReactionGroup.Minor,
                HitReactionType.HeavyHit => HitReactionGroup.Minor,
                HitReactionType.Launch => HitReactionGroup.Major,
                HitReactionType.AirCombo => HitReactionGroup.Control,
                HitReactionType.SlamDown => HitReactionGroup.Control,
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
                return self.DegradeReactionType(HitReactionType.Launch, allowed);
            }
            if (g == HitReactionGroup.Major)
            {
                return self.DegradeReactionType(HitReactionType.HeavyHit, allowed);
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
                case HitMotionType.None:
                case HitMotionType.NormalHit:
                case HitMotionType.HorizontalImpulse:
                case HitMotionType.CustomCurve:
                    type = AirborneReason.Juggled;
                    break;
                case HitMotionType.UpwardImpulse:
                    type = AirborneReason.Launched;
                    break;
                case HitMotionType.TowardAttacker:
                    type = AirborneReason.Knockdown;
                    break;
            }

            return type;
        }
    }
}

