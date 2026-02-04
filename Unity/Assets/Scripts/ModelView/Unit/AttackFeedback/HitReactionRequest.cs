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
        public readonly struct HitRuleData
        {
            /// <summary>视觉受击类型（决定规则层优先级/动画选择）。</summary>
            public readonly HitReactionType ReactionType;

            /// <summary>攻击强度（用于目标侧规则判定）。数值越大越“强”。</summary>
            public readonly byte HitStrength;

            /// <summary>是否显式设置过 HitStrength。</summary>
            public readonly bool HasHitStrength;

            /// <summary>物理运动数据（推/飞/砸/拉）。</summary>
            public readonly HitMotionData MotionData;

            /// <summary>目标状态过滤（可多选；Any=都可命中）。</summary>
            public readonly TargetStateMask TargetStates;

            /// <summary>命中方向（通常是 attacker→target 的水平向量）。</summary>
            public readonly Vector3 HitDirection;

            /// <summary>硬直时间(ms, combat-time)。</summary>
            public readonly int HitStunMs;

            public HitRuleData(
                HitReactionType reactionType,
                byte hitStrength,
                bool hasHitStrength,
                HitMotionData motionData,
                TargetStateMask targetStates,
                Vector3 hitDirection,
                int hitStunMs)
            {
                this.ReactionType = reactionType;
                this.HitStrength = hitStrength;
                this.HasHitStrength = hasHitStrength;
                this.MotionData = motionData;
                this.TargetStates = targetStates;
                this.HitDirection = hitDirection;
                this.HitStunMs = hitStunMs;
            }
        }

        public readonly struct HitFeedbackRequestData
        {
            /// <summary>受击者侧顿帧(ms, combat-time)。</summary>
            public readonly int VictimHitStopMs;

            /// <summary>震屏强度(0-1)。</summary>
            public readonly float ScreenShakeIntensity;

            /// <summary>震屏时长(ms, combat-time)。</summary>
            public readonly int ScreenShakeDurationMs;

            /// <summary>慢动作倍率（1=正常）。</summary>
            public readonly float TimeScale;

            /// <summary>慢动作持续时间(ms, combat-time)。</summary>
            public readonly int TimeScaleDurationMs;

            public HitFeedbackRequestData(
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
        }

        public readonly struct AirComboHint
        {
            /// <summary>
            /// 攻击方当前段的连击超时(ms, combat-time)。
            /// 用于受击方空连续期对齐，避免提前 BeginExit。
            /// </summary>
            public readonly int AttackerSegmentComboTimeoutMs;
            public readonly float AttackRadius;
            public readonly bool HasAttackerWorldPos;
            public readonly Vector3 AttackerWorldPos;

            public AirComboHint(
                int attackerSegmentComboTimeoutMs,
                float attackRadius,
                Vector3 attackerWorldPos,
                bool hasAttackerWorldPos)
            {
                this.AttackerSegmentComboTimeoutMs = attackerSegmentComboTimeoutMs;
                this.AttackRadius = attackRadius;
                this.AttackerWorldPos = attackerWorldPos;
                this.HasAttackerWorldPos = hasAttackerWorldPos;
            }
        }

        /// <summary>受击规则数据。</summary>
        public readonly HitRuleData Rule;

        /// <summary>受击反馈数据。</summary>
        public readonly HitFeedbackRequestData Feedback;

        /// <summary>空中连击提示数据（续期/半径/中心点）。</summary>
        public readonly AirComboHint AirCombo;

        /// <summary>
        /// 完整构造（用于规则层 Normalize 后生成“有效请求”）。
        /// </summary>
        public HitReactionRequest(
            in HitRuleData rule,
            in HitFeedbackRequestData feedback,
            in AirComboHint airCombo)
        {
            this.Rule = rule;
            this.Feedback = feedback;
            this.AirCombo = airCombo;
        }

        public HitReactionRequest(in HitEffectData effect, in HitFeedbackData feedback, Vector3 hitDirection, int defaultHitStopMs, AirComboHint airCombo = default)
        {
            Rule = new HitRuleData(
                effect.HitReaction,
                effect.HitStrength,
                effect.HitStrength != 0,
                effect.HitMotion,
                effect.TargetStates,
                hitDirection,
                effect.HitStunMs);
            Feedback = new HitFeedbackRequestData(
                feedback.ResolveVictimHitStopMs(defaultHitStopMs),
                feedback.ScreenShakeIntensity,
                feedback.ScreenShakeDurationMs,
                feedback.TimeScale,
                feedback.TimeScaleDurationMs);
            AirCombo = airCombo;
        }


        public override string ToString()
        {
            return $"受击请求(类型={this.Rule.ReactionType}, Priority={this.Rule.HitStrength}, 运动={this.Rule.MotionData.MotionType}:强度{this.Rule.MotionData.Force}, 目标状态过滤={this.Rule.TargetStates}, 方向={this.Rule.HitDirection}, 硬直时长={this.Rule.HitStunMs}ms, 受击停顿={this.Feedback.VictimHitStopMs}ms),攻击超时={this.AirCombo.AttackerSegmentComboTimeoutMs}ms";
        }
    }
}
