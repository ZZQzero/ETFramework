using UnityEngine;

namespace ET
{
    /// <summary>
    /// 一次命中对“目标侧”的数据
    /// - Effect：影响目标的 gameplay effect（受击类型/控制/击退/击飞/硬直/过滤）
    /// - Feedback：命中反馈（受击者侧顿帧、以及可选的镜头/时间反馈参数）
    /// </summary>
    public readonly struct HitImpactData
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
            //攻击范围
            public readonly float AttackRadius;
            //是否设置攻击者位置
            public readonly bool HasAttackerWorldPos;
            //攻击者位置
            public readonly Vector3 AttackerWorldPos;
            /// <summary>硬直时间(ms, combat-time)。</summary>
            public readonly int HitStunMs;
            //当前段攻击超时时长
            public readonly int AttackerSegmentTimeoutMs;
            //攻击总时长
            public readonly int AttackTotalTimeoutMs;

            public HitRuleData(
                HitReactionType reactionType,
                byte hitStrength,
                bool hasHitStrength,
                HitMotionData motionData,
                TargetStateMask targetStates,
                Vector3 hitDirection,
                float attackRadius,
                Vector3 attackerWorldPos,
                bool hasAttackerWorldPos,
                int hitStunMs,
                int attackerSegmentTimeoutMs,
                int attackTotalTimeoutMs)
            {
                this.ReactionType = reactionType;
                this.HitStrength = hitStrength;
                this.HasHitStrength = hasHitStrength;
                this.MotionData = motionData;
                this.TargetStates = targetStates;
                this.HitDirection = hitDirection;
                this.AttackRadius = attackRadius;
                this.AttackerWorldPos = attackerWorldPos;
                this.HasAttackerWorldPos = hasAttackerWorldPos;
                this.HitStunMs = hitStunMs;
                this.AttackerSegmentTimeoutMs = attackerSegmentTimeoutMs;
                this.AttackTotalTimeoutMs = attackTotalTimeoutMs;
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

        /// <summary>受击规则数据。</summary>
        public readonly HitRuleData Rule;

        /// <summary>受击反馈数据。</summary>
        public readonly HitFeedbackRequestData Feedback;
        
        /// <summary>
        /// 完整构造（用于规则层 Normalize 后生成“有效请求”）。
        /// </summary>
        public HitImpactData(
            in HitRuleData rule,
            in HitFeedbackRequestData feedback)
        {
            this.Rule = rule;
            this.Feedback = feedback;
        }

        public HitImpactData(
            in HitEffectData effect,
            in HitFeedbackData feedback,
            Vector3 hitDirection,
            int defaultHitStopMs,
            int attackerSegmentTimeoutMs,
            int attackerTotalTimeoutMs,
            float attackRadius,
            Vector3 attackerWorldPos,
            bool hasAttackerWorldPos)
        {
            Rule = new HitRuleData(
                effect.HitReaction,
                effect.HitStrength,
                effect.HitStrength != 0,
                effect.HitMotion,
                effect.TargetStates,
                hitDirection,
                attackRadius,
                attackerWorldPos,
                hasAttackerWorldPos,
                effect.HitStunMs,
                attackerSegmentTimeoutMs,
                attackerTotalTimeoutMs);
            Feedback = new HitFeedbackRequestData(
                feedback.ResolveVictimHitStopMs(defaultHitStopMs),
                feedback.ScreenShakeIntensity,
                feedback.ScreenShakeDurationMs,
                feedback.TimeScale,
                feedback.TimeScaleDurationMs);
        }


        public override string ToString()
        {
            var dir = Rule.HitDirection;
            var attackerPos = Rule.HasAttackerWorldPos ? Rule.AttackerWorldPos.ToString("F2") : "未设置";
            return $"HitImpactData[" +
                   $"类型={Rule.ReactionType}, " +
                   $"强度={Rule.HitStrength}, " +
                   $"运动={Rule.MotionData.MotionType}(力={Rule.MotionData.Force:F2}, 时长={Rule.MotionData.DurationMs}ms), " +
                   $"目标过滤={Rule.TargetStates}, " +
                   $"方向=({dir.x:F2}, {dir.y:F2}, {dir.z:F2}), " +
                   $"攻击半径={Rule.AttackRadius:F2}, " +
                   $"攻击者位置={attackerPos}, " +
                   $"硬直={Rule.HitStunMs}ms, " +
                   $"段超时={Rule.AttackerSegmentTimeoutMs}ms, " +
                   $"总超时={Rule.AttackTotalTimeoutMs}ms, " +
                   $"受击停顿={Feedback.VictimHitStopMs}ms, " +
                   $"震屏={Feedback.ScreenShakeIntensity:F2}({Feedback.ScreenShakeDurationMs}ms), " +
                   $"时间缩放={Feedback.TimeScale:F2}({Feedback.TimeScaleDurationMs}ms)" +
                   $"]";
        }
    }
}
