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
            public readonly byte LightReactionThreshold;//硬直门槛
            public readonly byte KnockbackThreshold; //击退门槛
            public readonly byte AirborneThreshold; //（地面→空中门槛）
            public readonly byte KnockdownThreshold;//（空中→砸地/倒地门槛）
            public readonly byte GetUpInterruptThreshold;//（打断起身门槛）

            public HitInterruptThresholds(byte lightReactionThreshold, byte knockbackThreshold, byte airborneThreshold, byte knockdownThreshold, byte getUpInterruptThreshold)
            {
                this.LightReactionThreshold = lightReactionThreshold;
                this.KnockbackThreshold = knockbackThreshold;
                this.AirborneThreshold = airborneThreshold;
                this.KnockdownThreshold = knockdownThreshold;
                this.GetUpInterruptThreshold = getUpInterruptThreshold;
            }

            public static readonly HitInterruptThresholds Default = new HitInterruptThresholds(20, 40, 50, 60, 80);
        }

        /// <summary>
        /// 抵抗力切表：不同受击状态下，使用不同的抵抗力阈值。
        /// </summary>
        public readonly struct HitInterruptThresholdsTable
        {
            public readonly HitInterruptThresholds Grounded;
            public readonly HitInterruptThresholds Airborne;
            public readonly HitInterruptThresholds AirFinisher;
            public readonly HitInterruptThresholds Knockdown;
            public readonly HitInterruptThresholds GetUp;

            public HitInterruptThresholdsTable(
                in HitInterruptThresholds grounded,
                in HitInterruptThresholds airborne,
                in HitInterruptThresholds airFinisher,
                in HitInterruptThresholds knockdown,
                in HitInterruptThresholds getUp)
            {
                this.Grounded = grounded;
                this.Airborne = airborne;
                this.AirFinisher = airFinisher;
                this.Knockdown = knockdown;
                this.GetUp = getUp;
            }

            public HitInterruptThresholds GetByState(HitState state)
            {
                switch (state)
                {
                    case HitState.Airborne: return this.Airborne;
                    case HitState.AirFinisher: return this.AirFinisher;
                    case HitState.Knockdown: return this.Knockdown;
                    case HitState.GetUp: return this.GetUp;
                    case HitState.Grounded:
                    case HitState.None:
                    default:
                        return this.Grounded;
                }
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
        public readonly HitInterruptThresholdsTable hitInterruptThresholds;

        /// <summary>数值倍率。</summary>
        public readonly Scales Scale;

        /// <summary>上限限制。</summary>
        public readonly Limits Limit;

        public HitReactionRulesConfig(
            HitReactionGroup allowedReactionGroups,
            HitStateVisualMask allowedStateVisuals,
            in HitInterruptThresholdsTable hitInterruptThresholdsTable,
            in Scales scales,
            in Limits limits)
        {
            this.AllowedReactionGroups = allowedReactionGroups;
            this.AllowedStateVisuals = allowedStateVisuals;
            this.hitInterruptThresholds = hitInterruptThresholdsTable;
            this.Scale = scales;
            this.Limit = limits;
        }

        public override string ToString()
        {
            var g = this.hitInterruptThresholds.Grounded;
            var a = this.hitInterruptThresholds.Airborne;
            var s = this.hitInterruptThresholds.AirFinisher;
            var k = this.hitInterruptThresholds.Knockdown;
            var u = this.hitInterruptThresholds.GetUp;
            return $"受击规则配置(允许表现组={this.AllowedReactionGroups}, 允许状态动画={this.AllowedStateVisuals}, 抵抗力表[Grounded/Airborne/AirStun/Knockdown/GetUp]=[" +
                   $"[{g.LightReactionThreshold}/{g.KnockbackThreshold}/{g.AirborneThreshold}/{g.KnockdownThreshold}/{g.GetUpInterruptThreshold}]/" +
                   $"[{a.LightReactionThreshold}/{a.KnockbackThreshold}/{a.AirborneThreshold}/{a.KnockdownThreshold}/{a.GetUpInterruptThreshold}]/" +
                   $"[{s.LightReactionThreshold}/{s.KnockbackThreshold}/{s.AirborneThreshold}/{s.KnockdownThreshold}/{s.GetUpInterruptThreshold}]/" +
                   $"[{k.LightReactionThreshold}/{k.KnockbackThreshold}/{k.AirborneThreshold}/{k.KnockdownThreshold}/{k.GetUpInterruptThreshold}]/" +
                   $"[{u.LightReactionThreshold}/{u.KnockbackThreshold}/{u.AirborneThreshold}/{u.KnockdownThreshold}/{u.GetUpInterruptThreshold}]], " +
                   $"数值缩放[硬直/击退/击飞]=[{this.Scale.Stun:0.###}/{this.Scale.Knockback:0.###}/{this.Scale.Knockup:0.###}], " +
                   $"上限限制[硬直ms/击退力/击飞力]=[{this.Limit.MaxHitStunMs}/{this.Limit.MaxKnockbackForce:0.###}/{this.Limit.MaxKnockupForce:0.###}])";
        }
    }
}

