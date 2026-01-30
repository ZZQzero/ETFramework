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

        /// <summary>每次命中至少维持的空中时间（combat-time）。</summary>
        public readonly int MinAirTimeMs;

        /// <summary>
        /// 最大悬空总时长（combat-time）。
        /// fail-safe：避免 bug/极端情况导致永远悬空。
        /// </summary>
        public readonly int MaxTotalHangMs;

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

        /// <summary>落地硬直（非砸地/非倒地语义时使用）。</summary>
        public readonly int LandingStunMs;

        public HitAirComboProfile(
            bool enable,
            int minAirTimeMs,
            int maxTotalHangMs,
            float gravityScaleDuringCombo,
            float minFallSpeedAbs,
            float minHeightOffset,
            float maxHeightOffset,
            int exitLerpMs,
            int landingStunMs)
        {
            this.Enable = enable;
            this.MinAirTimeMs = minAirTimeMs;
            this.MaxTotalHangMs = maxTotalHangMs;
            this.GravityScaleDuringCombo = gravityScaleDuringCombo;
            this.MinFallSpeedAbs = minFallSpeedAbs;
            this.MinHeightOffset = minHeightOffset;
            this.MaxHeightOffset = maxHeightOffset;
            this.ExitLerpMs = exitLerpMs;
            this.LandingStunMs = landingStunMs;
        }
    }
}

