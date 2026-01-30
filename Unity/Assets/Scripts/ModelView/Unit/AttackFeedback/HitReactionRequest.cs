using UnityEngine;

namespace ET
{
    /// <summary>
    /// 一次命中对“目标侧”的受击请求
    /// - Effect：影响目标的 gameplay effect（受击类型/控制/击退/击飞/硬直/过滤）
    /// - Feedback：命中反馈（受击者侧顿帧、以及可选的镜头/时间反馈参数）
    /// </summary>
    public readonly struct HitReactionRequest
    {
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

        public HitReactionRequest(in HitEffectData effect, in HitFeedbackData feedback, Vector3 hitDirection, int defaultHitStopMs)
        {
            ReactionType = effect.HitReaction;
            MotionData = effect.HitMotion;
            TargetStates = effect.TargetStates;
            HitDirection = hitDirection;
            HitStunMs = effect.HitStunMs;
            VictimHitStopMs = feedback.ResolveVictimHitStopMs(defaultHitStopMs);
            ScreenShakeIntensity = feedback.ScreenShakeIntensity;
            ScreenShakeDurationMs = feedback.ScreenShakeDurationMs;
            TimeScale = feedback.TimeScale;
            TimeScaleDurationMs = feedback.TimeScaleDurationMs;
        }


        public override string ToString()
        {
            return $"受击请求(类型={this.ReactionType}, 运动={this.MotionData.MotionType}:强度{this.MotionData.Force}, 目标状态过滤={this.TargetStates}, 方向={this.HitDirection}, 硬直时长={this.HitStunMs}ms, 受击停顿={this.VictimHitStopMs}ms)";
        }
    }
}
