using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace ET
{

    /// <summary>
    /// 受击配置资产（ScriptableObject）。
    /// 用于将硬编码的受击配置迁移到可编辑的资产文件。
    /// </summary>
    [CreateAssetMenu(fileName = "HitReactionProfile", menuName = "Combat/HitReactionProfile")]
    public class HitReactionProfileAsset : ScriptableObject
    {
        [Header("受击规则配置")]
        [Tooltip("受击规则数据")]
        public HitReactionRulesData Rules;

        [Header("受击反馈配置")]
        [Tooltip("受击反馈数据")]
        public HitReactionFeedbackData Feedback;

        [Header("空中连段配置")]
        [Tooltip("空中连段数据")]
        public HitAirComboData AirCombo;

        [Header("受击拴系配置（地面+空中共用）")]
        [Tooltip("受击水平距离约束，替代原 AirCombo 的水平字段")]
        public HitTetherData Tether;
    }
    
    /// <summary>
    /// 受击规则数据（可序列化版本）
    /// </summary>
    [System.Serializable]
    public struct HitReactionRulesData
    {
        [Header("允许的受击表现分组")]
        public HitReactionGroup AllowedReactionGroups;

        [Header("允许的受击状态动画")]
        public HitStateVisualMask AllowedStateVisuals;

        [Header("受击打断门槛")]
        public HitInterruptThresholdsData InterruptThresholds;

        [Header("数值倍率")]
        public HitReactionScalesData Scales;

        [Header("上限限制")]
        public HitReactionLimitsData Limits;

        public HitReactionRulesData(
            HitReactionGroup allowedReactionGroups = HitReactionGroup.All,
            HitStateVisualMask allowedStateVisuals = HitStateVisualMask.All,
            HitInterruptThresholdsData interruptThresholds = default,
            HitReactionScalesData scales = default,
            HitReactionLimitsData limits = default)
        {
            this.AllowedReactionGroups = allowedReactionGroups;
            this.AllowedStateVisuals = allowedStateVisuals;
            this.InterruptThresholds = interruptThresholds;
            this.Scales = scales;
            this.Limits = limits;
        }
    }

    /// <summary>
    /// 受击打断门槛数据（可序列化版本）
    /// </summary>
    [System.Serializable]
    public struct HitInterruptThresholdsData
    {
        [Header("地面状态门槛")]
        public GroundedThresholdData Grounded;

        [Header("空中状态门槛")]
        public AirborneThresholdData Airborne;

        [Header("倒地状态门槛")]
        public KnockdownThresholdData Knockdown;

        [Header("起身状态门槛")]
        public GetUpThresholdData GetUp;

    }

    /// <summary>
    /// 地面状态门槛数据
    /// [已废弃] 受击路由已改为仅按 MotionType 分发，这些阈值不再用于 HandleGroundedHit。
    /// 保留结构以兼容已有 ScriptableObject 序列化数据。
    /// </summary>
    [System.Serializable]
    public struct GroundedThresholdData
    {
        [Obsolete("受击路由已改为 MotionType 分发，此字段不再使用")]
        [Tooltip("轻度受击门槛（已废弃）")]
        public byte LightReactionThreshold;

        [Obsolete("受击路由已改为 MotionType 分发，此字段不再使用")]
        [Tooltip("击退门槛（已废弃）")]
        public byte KnockbackThreshold;

        [Obsolete("受击路由已改为 MotionType 分发，此字段不再使用")]
        [Tooltip("击飞门槛（已废弃）")]
        public byte AirborneThreshold;

        [Obsolete("受击路由已改为 MotionType 分发，此字段不再使用")]
        [Tooltip("砸地门槛（已废弃）")]
        public byte KnockdownThreshold;
        
        public GroundedThresholdData(
            byte lightReactionThreshold = 20,
            byte knockbackThreshold = 30,
            byte airborneThreshold = 50,
            byte knockdownThreshold = 60)
        {
            this.LightReactionThreshold = lightReactionThreshold;
            this.KnockbackThreshold = knockbackThreshold;
            this.AirborneThreshold = airborneThreshold;
            this.KnockdownThreshold = knockdownThreshold;
        }
    }

    /// <summary>
    /// 空中状态门槛数据
    /// [已废弃] 空中终结已改为按 MotionType == DownwardImpulse 触发，此阈值不再使用。
    /// 保留结构以兼容已有 ScriptableObject 序列化数据。
    /// </summary>
    [System.Serializable]
    public struct AirborneThresholdData
    {
        [Obsolete("空中终结已改为 MotionType 分发，此字段不再使用")]
        [Tooltip("空中终结门槛（已废弃）")]
        public byte AirborneThreshold;

        public AirborneThresholdData(byte airborneThreshold = 70)
        {
            this.AirborneThreshold = airborneThreshold;
        }
    }

    /// <summary>
    /// 倒地状态门槛数据
    /// </summary>
    [System.Serializable]
    public struct KnockdownThresholdData
    {
        [Tooltip("打断倒地门槛/从倒地状态拉起")]
        public byte KnockdownThreshold;

        public KnockdownThresholdData(byte knockdownThreshold = 80)
        {
            this.KnockdownThreshold = knockdownThreshold;
        }
    }

    /// <summary>
    /// 起身状态门槛数据
    /// </summary>
    [System.Serializable]
    public struct GetUpThresholdData
    {
        [Tooltip("打断起身门槛")]
        public byte GetUpInterruptThreshold;

        public GetUpThresholdData(byte getUpInterruptThreshold = 80)
        {
            this.GetUpInterruptThreshold = getUpInterruptThreshold;
        }
    }

    /// <summary>
    /// 数值倍率数据
    /// </summary>
    [System.Serializable]
    public struct HitReactionScalesData
    {
        [Tooltip("击退倍率")]
        [Min(0.1f)] public float Knockback;

        [Tooltip("击飞倍率")]
        [Min(0.1f)] public float Knockup;

        public HitReactionScalesData(float knockback = 1f, float knockup = 1f)
        {
            this.Knockback = knockback;
            this.Knockup = knockup;
        }
    }

    /// <summary>
    /// 上限限制数据
    /// </summary>
    [System.Serializable]
    public struct HitReactionLimitsData
    {
        [Tooltip("最大硬直时长(ms)")]
        [Min(1)] public int MaxHitStunMs;

        [Tooltip("最大击退力")]
        [Min(1f)] public float MaxKnockbackForce;

        [Tooltip("最大击飞力")]
        [Min(1f)] public float MaxKnockupForce;

        public HitReactionLimitsData(int maxHitStunMs = 1200, float maxKnockbackForce = 25f, float maxKnockupForce = 18f)
        {
            this.MaxHitStunMs = maxHitStunMs;
            this.MaxKnockbackForce = maxKnockbackForce;
            this.MaxKnockupForce = maxKnockupForce;
        }
    }

    /// <summary>
    /// 受击反馈数据（可序列化版本）
    /// </summary>
    [System.Serializable]
    public struct HitReactionFeedbackData
    {
        [Header("反馈选项")]
        public HitFeedbackOptionData Option;

    }

    /// <summary>
    /// 受击反馈选项数据
    /// </summary>
    [System.Serializable]
    public struct HitFeedbackOptionData
    {
        [Tooltip("是否允许受击顿帧")]
        public bool AllowVictimHitStop;

        [Tooltip("受击顿帧缩放系数")]
        [Range(0f, 2f)]
        public float VictimHitStopScale;

        [Tooltip("是否允许震屏")]
        public bool AllowScreenShake;

        [Tooltip("震屏缩放系数")]
        [Range(0f, 2f)]
        public float ScreenShakeScale;

        [Tooltip("是否允许时间缩放")]
        public bool AllowTimeScale;

        [Tooltip("时间缩放系数")]
        [Range(0f, 2f)]
        public float TimeScaleScale;

        public HitFeedbackOptionData(
            bool allowVictimHitStop = false,
            float victimHitStopScale = 0f,
            bool allowScreenShake = true,
            float screenShakeScale = 1f,
            bool allowTimeScale = false,
            float timeScaleScale = 0f)
        {
            this.AllowVictimHitStop = allowVictimHitStop;
            this.VictimHitStopScale = victimHitStopScale;
            this.AllowScreenShake = allowScreenShake;
            this.ScreenShakeScale = screenShakeScale;
            this.AllowTimeScale = allowTimeScale;
            this.TimeScaleScale = timeScaleScale;
        }
    }

    /// <summary>
    /// 空中连段数据（可序列化版本）
    /// </summary>
    [System.Serializable]
    public struct HitAirComboData
    {
        [Tooltip("是否启用空中连段")]
        public bool Enable;

        [Header("时间配置")]
        [Tooltip("最大空中时间偏移（毫秒）")]
        [Min(0)] public int MaxAirOffsetMs;

        [Tooltip("退出渐变时间（毫秒）")]
        [Min(0)] public int ExitLerpMs;

        [Header("物理配置")]
        [Tooltip("空中重力缩放")]
        [Range(0f, 1f)]
        public float GravityScaleDuringCombo;

        [Tooltip("最小下落速度（绝对值）")]
        [Min(0f)] public float MinFallSpeedAbs;

        [Tooltip("最小高度偏移")]
        public float MinHeightOffset;

        [Tooltip("最大高度偏移")]
        public float MaxHeightOffset;

        [Header("高度安全")]
        [Tooltip("绝对最大高度（相对首次击飞点），防止 UpwardImpulse 命中逐级抬升天花板")]
        [Min(1f)] public float AbsoluteMaxHeight;

        public HitAirComboData(
            bool enable = true,
            int maxAirOffsetMs = 250,
            int exitLerpMs = 160,
            float gravityScaleDuringCombo = 0.12f,
            float minFallSpeedAbs = 0.8f,
            float minHeightOffset = 0f,
            float maxHeightOffset = 2.2f,
            float absoluteMaxHeight = 6f)
        {
            this.Enable = enable;
            this.MaxAirOffsetMs = maxAirOffsetMs;
            this.ExitLerpMs = exitLerpMs;
            this.GravityScaleDuringCombo = gravityScaleDuringCombo;
            this.MinFallSpeedAbs = minFallSpeedAbs;
            this.MinHeightOffset = minHeightOffset;
            this.MaxHeightOffset = maxHeightOffset;
            this.AbsoluteMaxHeight = absoluteMaxHeight;
        }
    }

    /// <summary>
    /// 受击拴系数据（地面+空中共用，可序列化版本）。
    /// NormalHit 时基于距离维持：保持受击者在 [AnchorPointDistance, MaxDistance] 区间内。
    /// 非 NormalHit 不受拴系约束。
    /// </summary>
    [System.Serializable]
    public struct HitTetherData
    {
        [Tooltip("是否启用受击拴系（仅 NormalHit 生效）")]
        public bool Enable;

        [Tooltip("锚点距离（距攻击者的最优距离，怪物会被推到至少此距离）")]
        [Min(0.1f)] public float AnchorPointDistance;

        [Tooltip("最大水平距离（超过此距离怪物会被拉回）")]
        [Min(0.5f)] public float MaxDistance;

        [Tooltip("重定位弹簧刚度（越大响应越快）")]
        [Min(0.1f)] public float RepositionSpeed;

        [Tooltip("最大水平速度上限（m/s）")]
        [Min(0f)] public float MaxHorizontalSpeed;

        public HitTetherData(
            bool enable = true,
            float anchorPointDistance = 1.5f,
            float maxDistance = 2f,
            float repositionSpeed = 8f,
            float maxHorizontalSpeed = 6f)
        {
            this.Enable = enable;
            this.AnchorPointDistance = anchorPointDistance;
            this.MaxDistance = maxDistance;
            this.RepositionSpeed = repositionSpeed;
            this.MaxHorizontalSpeed = maxHorizontalSpeed;
        }
    }
}
