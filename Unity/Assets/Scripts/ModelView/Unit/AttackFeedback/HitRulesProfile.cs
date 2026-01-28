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
        /// <summary>
        /// 受击优先级分组（数值越大越强）。
        /// </summary>
        public readonly struct Priorities
        {
            public readonly byte Light;
            public readonly byte Medium;
            public readonly byte Heavy;
            public readonly byte Knockback;
            public readonly byte Knockup;
            public readonly byte Knockdown;

            public Priorities(byte light, byte medium, byte heavy, byte knockback, byte knockup, byte knockdown)
            {
                this.Light = light;
                this.Medium = medium;
                this.Heavy = heavy;
                this.Knockback = knockback;
                this.Knockup = knockup;
                this.Knockdown = knockdown;
            }
        }

        /// <summary>
        /// 状态抵抗力分组（处于该状态时，需要多强的 Priority 才能打断）。
        /// </summary>
        public readonly struct Resistances
        {
            public readonly byte Stun;
            public readonly byte Knockback;
            public readonly byte Airborne;
            public readonly byte Knockdown;
            public readonly byte GetUp;

            public Resistances(byte stun, byte knockback, byte airborne, byte knockdown, byte getUp)
            {
                this.Stun = stun;
                this.Knockback = knockback;
                this.Airborne = airborne;
                this.Knockdown = knockdown;
                this.GetUp = getUp;
            }

            public static readonly Resistances Default = new Resistances(20, 40, 50, 60, 80);
        }

        /// <summary>
        /// 数值倍率分组。
        /// </summary>
        public readonly struct Scales
        {
            public readonly float Stun;
            public readonly float Knockback;
            public readonly float Knockup;

            public Scales(float stun, float knockback, float knockup)
            {
                this.Stun = stun;
                this.Knockback = knockback;
                this.Knockup = knockup;
            }
        }

        /// <summary>
        /// 上限/钳制分组。
        /// </summary>
        public readonly struct Limits
        {
            public readonly int MaxHitStunMs;
            public readonly float MaxKnockbackForce;
            public readonly float MaxKnockupForce;

            public Limits(int maxHitStunMs, float maxKnockbackForce, float maxKnockupForce)
            {
                this.MaxHitStunMs = maxHitStunMs;
                this.MaxKnockbackForce = maxKnockbackForce;
                this.MaxKnockupForce = maxKnockupForce;
            }
        }

        /// <summary>可接受的受击类型集合（Boss 免疫挑空/击倒等用这里控制）。</summary>
        public readonly HitReactionMask AcceptMask;

        /// <summary>优先级：数值越大越“强”，可覆盖当前受击。</summary>
        public readonly Priorities Priority;

        /// <summary>当前状态抵抗力配置。</summary>
        public readonly Resistances Resistance;

        /// <summary>
        /// Refresh（不重播动画，延长硬直/叠加力度）。
        /// </summary>
        public readonly HitRules.ApplyMode LowerOrEqualMode;

        /// <summary>数值倍率。</summary>
        public readonly Scales Scale;

        /// <summary>上限限制。</summary>
        public readonly Limits Limit;

        public HitRulesProfile(
            HitReactionMask acceptMask,
            in Priorities priorities,
            in Resistances resistances,
            HitRules.ApplyMode lowerOrEqualMode,
            in Scales scales,
            in Limits limits)
        {
            this.AcceptMask = acceptMask;
            this.Priority = priorities;
            this.Resistance = resistances;
            this.LowerOrEqualMode = lowerOrEqualMode;
            this.Scale = scales;
            this.Limit = limits;
        }

        public bool Accepts(HitReactionType type)
        {
            return (this.AcceptMask & HitReactionMaskExtensions.ToMask(type)) != 0;
        }

        public byte GetPriority(HitReactionType type)
        {
            switch (type)
            {
                case HitReactionType.Light: return this.Priority.Light;
                case HitReactionType.Medium: return this.Priority.Medium;
                case HitReactionType.Heavy: return this.Priority.Heavy;
                case HitReactionType.Knockback: return this.Priority.Knockback;
                case HitReactionType.Knockup: return this.Priority.Knockup;
                case HitReactionType.Knockdown: return this.Priority.Knockdown;
                default: return 0;
            }
        }

        public override string ToString()
        {
            return $"HitRulesProfile(Accept={this.AcceptMask}, Pri[L/M/H/KB/KU/KD]=[{this.Priority.Light}/{this.Priority.Medium}/{this.Priority.Heavy}/{this.Priority.Knockback}/{this.Priority.Knockup}/{this.Priority.Knockdown}], Res[S/KB/A/KD/GU]=[{this.Resistance.Stun}/{this.Resistance.Knockback}/{this.Resistance.Airborne}/{this.Resistance.Knockdown}/{this.Resistance.GetUp}], LowerOrEq={this.LowerOrEqualMode}, Scale[Stun/KB/KU]=[{this.Scale.Stun:0.###}/{this.Scale.Knockback:0.###}/{this.Scale.Knockup:0.###}], Max[StunMs/KB/KU]=[{this.Limit.MaxHitStunMs}/{this.Limit.MaxKnockbackForce:0.###}/{this.Limit.MaxKnockupForce:0.###}])";
        }
    }
}

