using UnityEngine;

namespace ET
{
    /// <summary>
    /// 一次命中对“目标侧”的受击请求（商业级：数据驱动、接口边界清晰）。
    /// - Effect：影响目标的 gameplay effect（受击类型/控制/击退/击飞/硬直/过滤）
    /// - Feedback：命中反馈（顿帧/震屏/慢动作等；通常主要作用于表现域）
    /// 说明：
    /// - 这里刻意使用值类型+基础字段，避免把可变的配置对象（HitEffectData/HitFeedbackData）直接引用进运行时状态。
    /// </summary>
    public readonly struct HitReactionRequest
    {
        /// <summary>受击类型（决定规则层优先级/动画选择）。</summary>
        public readonly HitReactionType ReactionType;
        /// <summary>目标状态过滤（可多选；Any=都可命中）。</summary>
        public readonly TargetStateMask TargetStates;

        /// <summary>命中方向（通常是 attacker→target 的水平向量，规则层会做归一化/钳制）。</summary>
        public readonly Vector3 HitDirection;   // 水平为主（建议已做归一化与 y=0）
        /// <summary>击退力度（仅击退/击倒类反应会用到）。</summary>
        public readonly float KnockbackForce;
        /// <summary>击飞力度（仅击飞/击倒类反应会用到）。</summary>
        public readonly float KnockupForce;
        /// <summary>硬直时间(ms)（目标侧受击无法行动的持续时间）。</summary>
        public readonly int HitStunMs;

        // ===== 反馈（可选） =====
        /// <summary>顿帧(ms)（表现域；是否生效由 HitFeedbackProfile 决定）。</summary>
        public readonly int HitStopMs;
        /// <summary>震屏强度(0-1)（通常由相机/反馈系统消费）。</summary>
        public readonly float ScreenShakeIntensity;
        /// <summary>震屏时长(秒)。</summary>
        public readonly float ScreenShakeDuration;
        /// <summary>慢动作倍率（1=正常，小于1=慢）。</summary>
        public readonly float TimeScale;
        /// <summary>慢动作持续时间(ms)。</summary>
        public readonly int TimeScaleDurationMs;

        public HitReactionRequest(
            HitReactionType reactionType,
            TargetStateMask targetStates,
            Vector3 hitDirection,
            float knockbackForce,
            float knockupForce,
            int hitStunMs,
            int hitStopMs = 0,
            float screenShakeIntensity = 0f,
            float screenShakeDuration = 0f,
            float timeScale = 1f,
            int timeScaleDurationMs = 0)
        {
            this.ReactionType = reactionType;
            this.TargetStates = targetStates;
            this.HitDirection = hitDirection;
            this.KnockbackForce = knockbackForce;
            this.KnockupForce = knockupForce;
            this.HitStunMs = hitStunMs;

            this.HitStopMs = hitStopMs;
            this.ScreenShakeIntensity = screenShakeIntensity;
            this.ScreenShakeDuration = screenShakeDuration;
            this.TimeScale = timeScale;
            this.TimeScaleDurationMs = timeScaleDurationMs;
        }

        public static HitReactionRequest From(HitEffectData effect, HitFeedbackData feedback, Vector3 hitDirection)
        {
            if (effect == null)
            {
                // 兜底：不做受击
                return new HitReactionRequest(HitReactionType.None, TargetStateMask.Any, hitDirection, 0f, 0f, 0);
            }

            // 说明：这里“只做数据拷贝”，不做归一化/钳制；归一化由规则层 HitRules 执行。
            return new HitReactionRequest(
                effect.HitReaction,
                effect.TargetStates,
                hitDirection,
                effect.KnockbackForce,
                effect.KnockupForce,
                effect.HitStunMs,
                feedback != null ? feedback.HitStopMs : 0,
                feedback != null ? feedback.ScreenShakeIntensity : 0f,
                feedback != null ? feedback.ScreenShakeDuration : 0f,
                feedback != null ? feedback.TimeScale : 1f,
                feedback != null ? feedback.TimeScaleDurationMs : 0
            );
        }
    }
}

