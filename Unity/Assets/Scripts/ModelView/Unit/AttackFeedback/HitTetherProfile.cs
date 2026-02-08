namespace ET
{
    /// <summary>
    /// 受击拴系 Profile（运行时只读缓存，地面+空中共用）。
    /// NormalHit 时基于距离维持：保持受击者在 [AnchorPointDistance, MaxDistance] 区间内。
    /// 非 NormalHit 不受拴系约束，纯 Force/Curve 物理驱动。
    /// </summary>
    public readonly struct HitTetherProfile
    {
        public readonly bool Enable;

        /// <summary>锚点距离（距攻击者的最优距离），怪物会被推到至少此距离。</summary>
        public readonly float AnchorPointDistance;

        /// <summary>最大水平距离（距攻击者），超过此距离怪物会被拉回。</summary>
        public readonly float MaxDistance;

        /// <summary>重定位弹簧刚度（越大响应越快）。</summary>
        public readonly float RepositionSpeed;

        /// <summary>最大水平速度上限（m/s）。</summary>
        public readonly float MaxHorizontalSpeed;

        public HitTetherProfile(
            bool enable,
            float anchorPointDistance,
            float maxDistance,
            float repositionSpeed,
            float maxHorizontalSpeed)
        {
            this.Enable = enable;
            this.AnchorPointDistance = anchorPointDistance;
            this.MaxDistance = maxDistance;
            this.RepositionSpeed = repositionSpeed;
            this.MaxHorizontalSpeed = maxHorizontalSpeed;
        }
    }
}