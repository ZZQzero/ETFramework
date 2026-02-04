using System;

namespace ET
{
    /// <summary>
    /// 受击“表现分组”：
    /// - 只表达“目标愿不愿意播放某类受击表现”
    /// - 不表达“规则能不能生效”（免疫击飞/免疫眩晕应由规则层处理）
    /// </summary>
    [Flags]
    public enum HitReactionGroup : int
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
    public enum HitStateVisualMask : int
    {
        None = 0,
        Grounded = 1 << 0,
        Airborne = 1 << 1,
        AirStun = 1 << 2,
        Knockdown = 1 << 3,
        GetUp = 1 << 4,
        All = Grounded | Airborne | AirStun | Knockdown | GetUp,
    }
    
    /// <summary>
    /// 受击规则 Profile（目标侧 Gameplay 规则）：
    /// - 规则：抵抗力（按状态切表）、数值缩放、钳制
    /// - 表现许可：允许哪些受击表现分组、允许哪些状态动画播放
    /// - 抵抗力（按状态切表）
    /// - 数值修正与钳制（stun/knockback/knockup）
    /// </summary>
    public readonly struct HitReactionRulesConfig
    {
        /// <summary>
        /// 状态抵抗力分组（处于该状态时，需要多强的 Priority 才能打断）。
        /// </summary>
        public readonly struct HitInterruptThresholds
        {
            public readonly GroundedThresholdData Grounded;
            public readonly AirborneThresholdData Airborne;
            public readonly AirFinisherThresholdData AirFinisher;
            public readonly KnockdownThresholdData Knockdown;
            public readonly GetUpThresholdData GetUp;
            
            public HitInterruptThresholds(
                GroundedThresholdData grounded,
                AirborneThresholdData airborne,
                AirFinisherThresholdData airFinisher,
                KnockdownThresholdData knockdown,
                GetUpThresholdData getUp)
            {
                this.Grounded = grounded;
                this.Airborne = airborne;
                this.AirFinisher = airFinisher;
                this.Knockdown = knockdown;
                this.GetUp = getUp;
            }
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

        /// <summary>
        /// 允许播放哪些“受击表现分组”（只影响动画/表现，命中规则仍会生效）。
        /// </summary>
        public readonly HitReactionGroup AllowedReactionGroups;

        /// <summary>
        /// 允许播放哪些“受击状态动画”（只影响动画；规则状态流转仍会发生）。
        /// </summary>
        public readonly HitStateVisualMask AllowedStateVisuals;

        /// <summary>抵抗力：按状态切表。</summary>
        public readonly HitInterruptThresholds HitInterrupt;
        /// <summary>数值倍率。</summary>
        public readonly Scales Scale;

        /// <summary>上限限制。</summary>
        public readonly Limits Limit;

        public HitReactionRulesConfig(
            HitReactionGroup allowedReactionGroups,
            HitStateVisualMask allowedStateVisuals,
            in HitInterruptThresholds hitInterrupt,
            in Scales scales,
            in Limits limits)
        {
            this.AllowedReactionGroups = allowedReactionGroups;
            this.AllowedStateVisuals = allowedStateVisuals;
            this.HitInterrupt = hitInterrupt;
            this.Scale = scales;
            this.Limit = limits;
        }

        public override string ToString()
        {
            var g = this.HitInterrupt.Grounded;
            var a = this.HitInterrupt.Airborne;
            var s = this.HitInterrupt.AirFinisher;
            var k = this.HitInterrupt.Knockdown;
            var u = this.HitInterrupt.GetUp;
            return $"受击规则配置(允许表现组={this.AllowedReactionGroups}, 允许状态动画={this.AllowedStateVisuals}, " +
                   $"抵抗力表[Grounded(Light/Knockback/Airborne/Knockdown)]=[{g.LightReactionThreshold}/{g.KnockbackThreshold}/{g.AirborneThreshold}/{g.KnockdownThreshold}], " +
                   $"[Airborne(AirFinisher/Knockdown)]=[{a.AirborneThreshold}/{a.KnockdownThreshold}], " +
                   $"[AirFinisher(Knockdown)]=[{s.KnockdownThreshold}], " +
                   $"[Knockdown(GetUpInterrupt)]=[{k.KnockdownThreshold}], " +
                   $"[GetUp(GetUpInterrupt)]=[{u.GetUpInterruptThreshold}], " +
                   $"数值缩放[硬直/击退/击飞]=[{this.Scale.Stun:0.###}/{this.Scale.Knockback:0.###}/{this.Scale.Knockup:0.###}], " +
                   $"上限限制[硬直ms/击退力/击飞力]=[{this.Limit.MaxHitStunMs}/{this.Limit.MaxKnockbackForce:0.###}/{this.Limit.MaxKnockupForce:0.###}])";
        }
    }
}

