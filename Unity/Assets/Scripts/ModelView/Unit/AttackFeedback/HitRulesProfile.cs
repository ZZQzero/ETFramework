using System;

namespace ET
{
    /// <summary>
    /// 受击类型掩码（用于 RulesProfile 的可接受类型集合）。
    /// </summary>
    [Flags]
    public enum HitReactionMask : int
    {
        None = 0,
        /// <summary>轻受击</summary>
        Light = 1 << 0,
        /// <summary>中受击</summary>
        Medium = 1 << 1,
        /// <summary>重受击</summary>
        Heavy = 1 << 2,
        /// <summary>踉跄</summary>
        Stagger = 1 << 3,
        /// <summary>眩晕</summary>
        Stun = 1 << 4,
        /// <summary>全选</summary>
        All = Light | Medium | Heavy | Stagger | Stun,
    }
    
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
            public readonly byte Stagger;
            public readonly byte Stun;

            public Priorities(byte light, byte medium, byte heavy, byte stagger, byte stun)
            {
                this.Light = light;
                this.Medium = medium;
                this.Heavy = heavy;
                this.Stagger = stagger;
                this.Stun = stun;
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

        /// <summary>数值倍率。</summary>
        public readonly Scales Scale;

        /// <summary>上限限制。</summary>
        public readonly Limits Limit;

        public HitRulesProfile(
            HitReactionMask acceptMask,
            in Priorities priorities,
            in Resistances resistances,
            in Scales scales,
            in Limits limits)
        {
            this.AcceptMask = acceptMask;
            this.Priority = priorities;
            this.Resistance = resistances;
            this.Scale = scales;
            this.Limit = limits;
        }

        public override string ToString()
        {
            return $"受击规则配置(接受类型={this.AcceptMask}, 优先级[轻/中/重/踉跄/眩晕]=[{this.Priority.Light}/{this.Priority.Medium}/{this.Priority.Heavy}/{this.Priority.Stagger}/{this.Priority.Stun}], 抵抗力[眩晕/击退/浮空/倒地/起身]=[{this.Resistance.Stun}/{this.Resistance.Knockback}/{this.Resistance.Airborne}/{this.Resistance.Knockdown}/{this.Resistance.GetUp}], 数值缩放[硬直/击退/击飞]=[{this.Scale.Stun:0.###}/{this.Scale.Knockback:0.###}/{this.Scale.Knockup:0.###}], 上限限制[硬直ms/击退力/击飞力]=[{this.Limit.MaxHitStunMs}/{this.Limit.MaxKnockbackForce:0.###}/{this.Limit.MaxKnockupForce:0.###}])";
        }
    }
}

