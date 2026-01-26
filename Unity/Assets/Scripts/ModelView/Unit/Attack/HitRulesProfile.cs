namespace ET
{
    /// <summary>
    /// 受击规则 Profile（目标侧 Gameplay 规则）：
    /// - 接受哪些受击类型（Mask）
    /// - 优先级（决定 Replace/Refresh/Reject）
    /// - 数值修正与钳制（stun/knockback/knockup）
    /// </summary>
    public readonly struct HitRulesProfile
    {
        /// <summary>可接受的受击类型集合（Boss 免疫挑空/击倒等用这里控制）。</summary>
        public readonly HitReactionMask AcceptMask;

        /// <summary>优先级：数值越大越“强”，可覆盖当前受击。</summary>
        public readonly byte LightPriority;
        public readonly byte MediumPriority;
        public readonly byte HeavyPriority;
        public readonly byte KnockbackPriority;
        public readonly byte KnockupPriority;
        public readonly byte KnockdownPriority;

        /// <summary>
        /// 当 incomingPriority &lt;= currentPriority 时的策略。
        /// 商业级默认：Refresh（不重播动画，延长硬直/叠加力度）。
        /// </summary>
        public readonly HitRules.ApplyMode LowerOrEqualMode;

        /// <summary>硬直时间倍率（1=不变，0.5=减半）。</summary>
        public readonly float StunScale;
        /// <summary>击退力度倍率（影响 KnockbackForce）。</summary>
        public readonly float KnockbackScale;
        /// <summary>击飞力度倍率（影响 KnockupForce）。</summary>
        public readonly float KnockupScale;

        /// <summary>硬直时间上限(ms)，0 表示不限制。</summary>
        public readonly int MaxHitStunMs;
        /// <summary>击退力度上限，0 表示不限制。</summary>
        public readonly float MaxKnockbackForce;
        /// <summary>击飞力度上限，0 表示不限制（Boss 可设为 0 直接禁用）。</summary>
        public readonly float MaxKnockupForce;

        public HitRulesProfile(
            HitReactionMask acceptMask,
            byte lightPriority,
            byte mediumPriority,
            byte heavyPriority,
            byte knockbackPriority,
            byte knockupPriority,
            byte knockdownPriority,
            HitRules.ApplyMode lowerOrEqualMode,
            float stunScale,
            float knockbackScale,
            float knockupScale,
            int maxHitStunMs,
            float maxKnockbackForce,
            float maxKnockupForce)
        {
            this.AcceptMask = acceptMask;

            this.LightPriority = lightPriority;
            this.MediumPriority = mediumPriority;
            this.HeavyPriority = heavyPriority;
            this.KnockbackPriority = knockbackPriority;
            this.KnockupPriority = knockupPriority;
            this.KnockdownPriority = knockdownPriority;

            this.LowerOrEqualMode = lowerOrEqualMode;

            this.StunScale = stunScale;
            this.KnockbackScale = knockbackScale;
            this.KnockupScale = knockupScale;

            this.MaxHitStunMs = maxHitStunMs;
            this.MaxKnockbackForce = maxKnockbackForce;
            this.MaxKnockupForce = maxKnockupForce;
        }

        public bool Accepts(HitReactionType type)
        {
            return (this.AcceptMask & HitReactionMaskExtensions.ToMask(type)) != 0;
        }

        public byte GetPriority(HitReactionType type)
        {
            switch (type)
            {
                case HitReactionType.Light: return this.LightPriority;
                case HitReactionType.Medium: return this.MediumPriority;
                case HitReactionType.Heavy: return this.HeavyPriority;
                case HitReactionType.Knockback: return this.KnockbackPriority;
                case HitReactionType.Knockup: return this.KnockupPriority;
                case HitReactionType.Knockdown: return this.KnockdownPriority;
                default: return 0;
            }
        }
    }
}

