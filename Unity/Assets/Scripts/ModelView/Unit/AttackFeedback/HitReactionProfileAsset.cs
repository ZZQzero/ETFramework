using UnityEngine;

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

        // 运行时转换逻辑后续接入时放在 Hotfix 层处理
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

        [Header("空中终结状态门槛")]
        public AirFinisherThresholdData AirFinisher;

        [Header("倒地状态门槛")]
        public KnockdownThresholdData Knockdown;

        [Header("起身状态门槛")]
        public GetUpThresholdData GetUp;

    }

    /// <summary>
    /// 地面状态门槛数据
    /// </summary>
    [System.Serializable]
    public struct GroundedThresholdData
    {
        [Tooltip("轻度受击门槛")]
        public byte LightReactionThreshold;

        [Tooltip("击退门槛")]
        public byte KnockbackThreshold;

        [Tooltip("击飞门槛")]
        public byte AirborneThreshold;

    }

    /// <summary>
    /// 空中状态门槛数据
    /// </summary>
    [System.Serializable]
    public struct AirborneThresholdData
    {
        [Tooltip("空中终结门槛")]
        public byte AirborneThreshold;

        [Tooltip("砸地门槛")]
        public byte KnockdownThreshold;

    }

    /// <summary>
    /// 空中终结状态门槛数据
    /// </summary>
    [System.Serializable]
    public struct AirFinisherThresholdData
    {
        [Tooltip("砸地门槛")]
        public byte KnockdownThreshold;

    }

    /// <summary>
    /// 倒地状态门槛数据
    /// </summary>
    [System.Serializable]
    public struct KnockdownThresholdData
    {
        [Tooltip("打断起身门槛")]
        public byte GetUpInterruptThreshold;

    }

    /// <summary>
    /// 起身状态门槛数据
    /// </summary>
    [System.Serializable]
    public struct GetUpThresholdData
    {
        [Tooltip("打断起身门槛")]
        public byte GetUpInterruptThreshold;

    }

    /// <summary>
    /// 数值倍率数据
    /// </summary>
    [System.Serializable]
    public struct HitReactionScalesData
    {
        [Tooltip("硬直倍率")]
        public float Stun;

        [Tooltip("击退倍率")]
        public float Knockback;

        [Tooltip("击飞倍率")]
        public float Knockup;
    }

    /// <summary>
    /// 上限限制数据
    /// </summary>
    [System.Serializable]
    public struct HitReactionLimitsData
    {
        [Tooltip("最大硬直时长(ms)")]
        public int MaxHitStunMs;

        [Tooltip("最大击退力")]
        public float MaxKnockbackForce;

        [Tooltip("最大击飞力")]
        public float MaxKnockupForce;
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
        [Tooltip("最小空中时间（毫秒）")]
        public int MinAirTimeMs;

        [Tooltip("最大悬空时间（毫秒）")]
        public int MaxTotalHangMs;

        [Tooltip("退出渐变时间（毫秒）")]
        public int ExitLerpMs;

        [Tooltip("落地硬直时间（毫秒）")]
        public int LandingStunMs;

        [Tooltip("空中二次击飞允许的最大向上速度")]
        public float MaxAirborneKnockupForce;

        [Header("物理配置")]
        [Tooltip("空中重力缩放")]
        [Range(0f, 1f)]
        public float GravityScaleDuringCombo;

        [Tooltip("最小下落速度（绝对值）")]
        public float MinFallSpeedAbs;

        [Tooltip("最小高度偏移")]
        public float MinHeightOffset;

        [Tooltip("最大高度偏移")]
        public float MaxHeightOffset;

        [Header("水平运动配置")]
        [Tooltip("最大水平距离")]
        public float MaxAirHorizontalDistance;

        [Tooltip("最大水平速度")]
        public float MaxAirHorizontalSpeed;

        [Tooltip("回拉强度")]
        public float RecenterStrength;

        [Tooltip("回拉死区")]
        public float RecenterDeadZone;

    }
}
