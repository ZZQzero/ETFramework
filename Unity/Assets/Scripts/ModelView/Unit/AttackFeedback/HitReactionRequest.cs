using UnityEngine;

namespace ET
{
    /// <summary>
    /// 一次命中对“目标侧”的受击请求
    /// - Effect：影响目标的 gameplay effect（受击类型/控制/击退/击飞/硬直/过滤）
    /// - Feedback：命中反馈（受击者侧顿帧、以及可选的镜头/时间反馈参数）
    /// 说明：
    /// - 这里刻意使用值类型+基础字段，避免把可变的配置对象直接引用进运行时状态。
    /// </summary>
    public readonly struct HitReactionRequest
    {
        /// <summary>
        /// 反馈参数（表现域）。
        /// 目的：减少主构造函数参数数量，让语义更清晰。
        /// </summary>
        public readonly struct FeedbackPayload
        {
            public readonly int VictimHitStopMs;
            public readonly float ScreenShakeIntensity;
            public readonly int ScreenShakeDurationMs;
            public readonly float TimeScale;
            public readonly int TimeScaleDurationMs;

            public FeedbackPayload(
                int victimHitStopMs,
                float screenShakeIntensity,
                int screenShakeDurationMs,
                float timeScale,
                int timeScaleDurationMs)
            {
                this.VictimHitStopMs = victimHitStopMs;
                this.ScreenShakeIntensity = screenShakeIntensity;
                this.ScreenShakeDurationMs = screenShakeDurationMs;
                this.TimeScale = timeScale;
                this.TimeScaleDurationMs = timeScaleDurationMs;
            }

            public static readonly FeedbackPayload Default = new FeedbackPayload(
                victimHitStopMs: 0,
                screenShakeIntensity: 0f,
                screenShakeDurationMs: 0,
                timeScale: 1f,
                timeScaleDurationMs: 0);
        }

        /// <summary>视觉受击类型（决定规则层优先级/动画选择）。</summary>
        public readonly HitReactionType ReactionType;
        /// <summary>物理运动数据（推/飞/砸/拉）。</summary>
        public readonly HitMotionData MotionData;
        /// <summary>目标状态过滤（可多选；Any=都可命中）。</summary>
        public readonly TargetStateMask TargetStates;

        /// <summary>命中方向（通常是 attacker→target 的水平向量）。</summary>
        public readonly Vector3 HitDirection;
        /// <summary>硬直时间(ms)（目标侧受击无法行动的持续时间）。</summary>
        public readonly int HitStunMs;

        // ===== 反馈（可选） =====
        /// <summary>受击者侧顿帧(ms)。</summary>
        public readonly int VictimHitStopMs;
        /// <summary>震屏强度(0-1)。</summary>
        public readonly float ScreenShakeIntensity;
        /// <summary>震屏时长(ms)。</summary>
        public readonly int ScreenShakeDurationMs;
        /// <summary>慢动作倍率（1=正常）。</summary>
        public readonly float TimeScale;
        /// <summary>慢动作持续时间(ms)。</summary>
        public readonly int TimeScaleDurationMs;

        public HitReactionRequest(
            HitReactionType reactionType,
            HitMotionData motionData,
            TargetStateMask targetStates,
            Vector3 hitDirection,
            int hitStunMs,
            in FeedbackPayload feedback)
            : this(
                reactionType,
                motionData,
                targetStates,
                hitDirection,
                hitStunMs,
                victimHitStopMs: feedback.VictimHitStopMs,
                screenShakeIntensity: feedback.ScreenShakeIntensity,
                screenShakeDurationMs: feedback.ScreenShakeDurationMs,
                timeScale: feedback.TimeScale,
                timeScaleDurationMs: feedback.TimeScaleDurationMs)
        {
        }

        public HitReactionRequest(
            HitReactionType reactionType,
            HitMotionData motionData,
            TargetStateMask targetStates,
            Vector3 hitDirection,
            int hitStunMs,
            int victimHitStopMs = 0,
            float screenShakeIntensity = 0f,
            int screenShakeDurationMs = 0,
            float timeScale = 1f,
            int timeScaleDurationMs = 0)
        {
            this.ReactionType = reactionType;
            this.MotionData = motionData;
            this.TargetStates = targetStates;
            this.HitDirection = hitDirection;
            this.HitStunMs = hitStunMs;

            this.VictimHitStopMs = victimHitStopMs;
            this.ScreenShakeIntensity = screenShakeIntensity;
            this.ScreenShakeDurationMs = screenShakeDurationMs;
            this.TimeScale = timeScale;
            this.TimeScaleDurationMs = timeScaleDurationMs;
        }

        public static HitReactionRequest From(in HitEffectData effect, in HitFeedbackData feedback, Vector3 hitDirection, int defaultHitStopMs)
        {
            FeedbackPayload fp = new FeedbackPayload(
                victimHitStopMs: feedback.ResolveVictimHitStopMs(defaultHitStopMs),
                screenShakeIntensity: feedback.ScreenShakeIntensity,
                screenShakeDurationMs: feedback.ScreenShakeDurationMs,
                timeScale: feedback.TimeScale,
                timeScaleDurationMs: feedback.TimeScaleDurationMs);
            return new HitReactionRequest(
                effect.HitReaction,
                effect.HitMotion,
                effect.TargetStates,
                hitDirection,
                effect.HitStunMs,
                in fp
            );
        }

        public override string ToString()
        {
            return $"受击请求(类型={this.ReactionType}, 运动={this.MotionData.MotionType}:强度{this.MotionData.Force}, 目标状态过滤={this.TargetStates}, 方向={this.HitDirection}, 硬直时长={this.HitStunMs}ms, 受击停顿={this.VictimHitStopMs}ms)";
        }
    }
}
