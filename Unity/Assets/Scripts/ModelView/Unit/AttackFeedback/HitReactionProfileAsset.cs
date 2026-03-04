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

        [Header("数值倍率")]
        public HitReactionScalesData Scales;

        [Header("上限限制")]
        public HitReactionLimitsData Limits;

        public HitReactionRulesData(
            HitReactionGroup allowedReactionGroups = HitReactionGroup.All,
            HitStateVisualMask allowedStateVisuals = HitStateVisualMask.All,
            HitReactionScalesData scales = default,
            HitReactionLimitsData limits = default)
        {
            AllowedReactionGroups = allowedReactionGroups;
            AllowedStateVisuals = allowedStateVisuals;
            Scales = scales;
            Limits = limits;
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
