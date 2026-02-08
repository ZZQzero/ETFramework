using UnityEngine;

namespace ET
{
    /// <summary>
    /// 空中连段（Air Combo）Profile：客户端表现域/本地运动规则。
    /// 目的：把“是否挂空中/挂多久/如何下落”从具体技能与瞬时冲量中解耦出来，形成可配置的商用规则集。
    /// </summary>
    public readonly struct HitAirComboProfile
    {
        /// <summary>是否启用空中连段物理模式。</summary>
        public readonly bool Enable;

        /// <summary>每次命中最大维持的空中时间偏移（combat-time）。</summary>
        public readonly int MaxAirOffsetMs;

        /// <summary>空中连段阶段重力缩放（0~1，推荐 0~0.2）。</summary>
        public readonly float GravityScaleDuringCombo;

        /// <summary>
        /// 最小下落速度下限（负值的绝对值，单位 m/s）。
        /// 例如 1 表示 vY 不会低于 -1。
        /// </summary>
        public readonly float MinFallSpeedAbs;

        /// <summary>最小高度偏移（相对 EnteredHeight）。用于防止自然下落导致“慢慢掉”。</summary>
        public readonly float MinHeightOffset;

        /// <summary>最大高度偏移（相对 EnteredHeight）。用于防止飞走。</summary>
        public readonly float MaxHeightOffset;

        /// <summary>退出空中连段时重力恢复到 1 的 lerp 时长（combat-time）。</summary>
        public readonly int ExitLerpMs;

        /// <summary>绝对最大高度（相对首次击飞点），防止 UpwardImpulse 命中逐级抬升天花板。</summary>
        public readonly float AbsoluteMaxHeight;

        public HitAirComboProfile(
            bool enable,
            int maxAirOffsetMs,
            float gravityScaleDuringCombo,
            float minFallSpeedAbs,
            float minHeightOffset,
            float maxHeightOffset,
            int exitLerpMs,
            float absoluteMaxHeight)
        {
            this.Enable = enable;
            this.MaxAirOffsetMs = maxAirOffsetMs;
            this.GravityScaleDuringCombo = gravityScaleDuringCombo;
            this.MinFallSpeedAbs = minFallSpeedAbs;
            this.MinHeightOffset = minHeightOffset;
            this.MaxHeightOffset = maxHeightOffset;
            this.ExitLerpMs = exitLerpMs;
            this.AbsoluteMaxHeight = absoluteMaxHeight;
        }

        public override string ToString()
        {
            return $"HitAirComboProfile(Enable={this.Enable}, MaxAirOffsetTimeMs={this.MaxAirOffsetMs}, " +
                   $"GravityScaleDuringCombo={this.GravityScaleDuringCombo:0.###}, MinFallSpeedAbs={this.MinFallSpeedAbs:0.###}, " +
                   $"MinHeightOffset={this.MinHeightOffset:0.###}, MaxHeightOffset={this.MaxHeightOffset:0.###}, " +
                   $"ExitLerpMs={this.ExitLerpMs}, AbsoluteMaxHeight={this.AbsoluteMaxHeight:0.###}";
        }
    }
}

