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
            /// <summary>物理运动数据（推/飞/砸/拉）。</summary>
            public readonly HitMotionData MotionData;

            /// <summary>目标状态过滤（可多选；Any=都可命中）。</summary>
            public readonly TargetStateMask TargetStates;

            /// <summary>命中方向（通常是 attacker→target 的水平向量）。</summary>
            public readonly Vector3 HitDirection;
            /// <summary>硬直时间(ms, combat-time)。</summary>
            public readonly int HitStunMs;
            //当前段攻击超时时长
            public readonly int AttackerSegmentTimeoutMs;

            public HitRuleData(
                HitMotionData motionData,
                TargetStateMask targetStates,
                Vector3 hitDirection,
                int hitStunMs,
                int attackerSegmentTimeoutMs)
            {
                this.MotionData = motionData;
                this.TargetStates = targetStates;
                this.HitDirection = hitDirection;
                this.HitStunMs = hitStunMs;
                this.AttackerSegmentTimeoutMs = attackerSegmentTimeoutMs;
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
            int attackerSegmentTimeoutMs)
        {
            Rule = new HitRuleData(
                effect.HitMotion,
                effect.TargetStates,
                hitDirection,
                effect.HitStunMs,
                attackerSegmentTimeoutMs);
            Feedback = new HitFeedbackRequestData(
                feedback.VictimHitStopMs,
                feedback.ScreenShakeIntensity,
                feedback.ScreenShakeDurationMs,
                feedback.TimeScale,
                feedback.TimeScaleDurationMs);
        }


        public override string ToString()
        {
            var dir = Rule.HitDirection;
            return $"HitImpactData[" +
                   $"运动={Rule.MotionData.MotionType}(力={Rule.MotionData.Force:F2}, 时长={Rule.MotionData.DurationMs}ms), " +
                   $"目标过滤={Rule.TargetStates}, " +
                   $"方向=({dir.x:F2}, {dir.y:F2}, {dir.z:F2}), " +
                   $"硬直={Rule.HitStunMs}ms, " +
                   $"段超时={Rule.AttackerSegmentTimeoutMs}ms, " +
                   $"受击停顿={Feedback.VictimHitStopMs}ms, " +
                   $"震屏={Feedback.ScreenShakeIntensity:F2}({Feedback.ScreenShakeDurationMs}ms), " +
                   $"时间缩放={Feedback.TimeScale:F2}({Feedback.TimeScaleDurationMs}ms)" +
                   $"]";
        }
    }
}
