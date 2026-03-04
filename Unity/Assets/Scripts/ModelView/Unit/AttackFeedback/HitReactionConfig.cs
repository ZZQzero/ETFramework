using System;

namespace ET
{
    /// <summary>
    /// 受击“表现分组”：
    /// - 只表达“目标愿不愿意播放某类受击表现”
    /// - 不表达“规则能不能生效”（免疫击飞/免疫眩晕应由规则层处理）
    /// </summary>
    [Flags]
    public enum HitReactionGroup : byte
    {
        None = 0,
        /// <summary>轻/中受击（连击主力）</summary>
        Minor = 1 << 0,
        /// <summary>重受击（明显反馈）</summary>
        Major = 1 << 1,
        /// <summary>控制类表现（Stagger/Stun 等）</summary>
        Control = 1 << 2,
        All = Minor | Major | Control,
    }

    /// <summary>
    /// 状态动画可视化许可（只影响“播不播动画”，不影响规则状态流转）。
    /// </summary>
    [Flags]
    public enum HitStateVisualMask : byte
    {
        None = 0,
        Grounded = 1 << 0,
        Airborne = 1 << 1,
        Knockdown = 1 << 2,
        GetUp = 1 << 3,
        All = Grounded | Airborne | Knockdown | GetUp,
    }
    
    /// <summary>
    /// 受击规则 Profile（目标侧 Gameplay 规则）：
    /// - 规则：数值缩放与钳制（stun/knockback/knockup）
    /// - 表现许可：允许哪些受击表现分组、允许哪些状态动画播放
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

        public readonly struct HitVisualPolicy
        {
            public readonly HitReactionGroup AllowedReactionGroups;
            public readonly HitStateVisualMask AllowedStateVisuals;

            public HitVisualPolicy(HitReactionGroup allowedReactionGroups, HitStateVisualMask allowedStateVisuals)
            {
                this.AllowedReactionGroups = allowedReactionGroups;
                this.AllowedStateVisuals = allowedStateVisuals;
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

        /// <summary>表现策略（允许哪些动画/表现）。</summary>
        public readonly HitVisualPolicy Visual;

        public HitReactionConfig(
            HitReactionGroup allowedReactionGroups,
            HitStateVisualMask allowedStateVisuals,
            in Scales scales,
            in Limits limits)
        {
            Rule = new HitRuleConfig(scales, limits);
            Visual = new HitVisualPolicy(allowedReactionGroups, allowedStateVisuals);
        }

        public override string ToString()
        {
            return $"受击规则配置(允许表现组={Visual.AllowedReactionGroups}, 允许状态动画={Visual.AllowedStateVisuals}, " +
                   $"数值缩放[击退/击飞]=[{Rule.Scale.Knockback:0.###}/{Rule.Scale.Knockup:0.###}], " +
                   $"上限限制[硬直ms/击退力/击飞力]=[{Rule.Limit.MaxHitStunMs}/{Rule.Limit.MaxKnockbackForce:0.###}/{Rule.Limit.MaxKnockupForce:0.###}])";
        }
    }
}

