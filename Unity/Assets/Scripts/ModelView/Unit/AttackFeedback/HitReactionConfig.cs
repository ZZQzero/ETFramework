namespace ET
{
    /// <summary>
    /// 受击规则 Profile（目标侧 Gameplay 规则）：
    /// - 规则：数值缩放与钳制（stun/knockback/knockup）
    /// </summary>
    public readonly struct HitReactionConfig
    {
        public readonly struct HitRuleConfig
        {
            public readonly Scales Scale;
            public readonly Limits Limit;

            public HitRuleConfig(in Scales scale, in Limits limit)
            {
                Scale = scale;
                Limit = limit;
            }
        }

        /// <summary>
        /// 数值倍率分组。
        /// </summary>
        public readonly struct Scales
        {
            public readonly float Knockback;
            public readonly float Knockup;

            public Scales(float knockback, float knockup)
            {
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

        /// <summary>规则配置（门槛/缩放/上限）。</summary>
        public readonly HitRuleConfig Rule;

        public HitReactionConfig(in Scales scales, in Limits limits)
        {
            Rule = new HitRuleConfig(scales, limits);
        }

        public override string ToString()
        {
            return $"受击规则配置(" +
                   $"数值缩放[击退/击飞]=[{Rule.Scale.Knockback:0.###}/{Rule.Scale.Knockup:0.###}], " +
                   $"上限限制[硬直ms/击退力/击飞力]=[{Rule.Limit.MaxHitStunMs}/{Rule.Limit.MaxKnockbackForce:0.###}/{Rule.Limit.MaxKnockupForce:0.###}])";
        }
    }
}

