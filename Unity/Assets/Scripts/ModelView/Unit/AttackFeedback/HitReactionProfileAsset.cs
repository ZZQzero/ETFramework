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
    }
    
    /// <summary>
    /// 受击规则数据（可序列化版本）
    /// </summary>
    [System.Serializable]
    public struct HitReactionRulesData
    {
        [Header("数值倍率")]
        public HitReactionScalesData Scales;

        [Header("上限限制")]
        public HitReactionLimitsData Limits;

        public HitReactionRulesData(
            HitReactionScalesData scales = default,
            HitReactionLimitsData limits = default)
        {
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

        [Tooltip("期望空中连段维持高度（相对击飞起点），目标高度模型基准")]
        public float DesiredComboHeight;

        [Header("高度安全")]
        [Tooltip("绝对最大高度（相对首次击飞点），防止 UpwardImpulse 命中逐级抬升天花板")]
        [Min(1f)] public float AbsoluteMaxHeight;

        public HitAirComboData(
            bool enable = true,
            int maxAirOffsetMs = 250,
            int exitLerpMs = 160,
            float gravityScaleDuringCombo = 0.12f,
            float minFallSpeedAbs = 0.8f,
            float desiredComboHeight = 2.2f,
            float absoluteMaxHeight = 6f)
        {
            this.Enable = enable;
            this.MaxAirOffsetMs = maxAirOffsetMs;
            this.ExitLerpMs = exitLerpMs;
            this.GravityScaleDuringCombo = gravityScaleDuringCombo;
            this.MinFallSpeedAbs = minFallSpeedAbs;
            this.DesiredComboHeight = desiredComboHeight;
            this.AbsoluteMaxHeight = absoluteMaxHeight;
        }
    }

}
