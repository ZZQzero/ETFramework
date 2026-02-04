using System;
using UnityEngine;

namespace ET
{
    [CreateAssetMenu(fileName = "GroundDetectorConfig", menuName = "Movement/GroundDetectorConfig")]
    public class GroundDetectorConfigAsset : ScriptableObject
    {
        [Header("地面检测配置")]
        public GroundDetectorConfig Config = new GroundDetectorConfig();
    }
    
    /// <summary>
    /// 地面检测器配置。
    /// 统一控制角色“是否接地 / 如何接地 / 接地稳定性”的所有规则参数。
    /// 该配置直接影响：
    /// - 移动系统（加速、制动、斜坡行为）
    /// - 跳跃与下落判定
    /// - 技能取消窗口
    /// - 动画状态切换（落地 / 硬直）
    /// </summary>
    [Serializable]
    public class GroundDetectorConfig
    {
        // ==================== 基础参数 ====================
        /// <summary>
        /// 地面检测使用的 LayerMask。
        /// 用于识别“可站立”的所有实体：
        /// - 地形
        /// - 静态模型
        /// - 动态平台
        /// </summary>
        [Tooltip("可站立地面的 LayerMask（地形/静态模型/平台）")]
        public LayerMask GroundMask = ~0;

        /// <summary>
        /// 可穿透平台的 LayerMask。
        /// 通常用于：
        /// - 下跳平台
        /// - 单向平台
        /// </summary>
        [Tooltip("可穿透平台的 LayerMask（下跳/单向平台）")]
        public LayerMask PlatformMask;

        /// <summary>
        /// 最大可站立斜坡角度（度）。
        /// 超过该角度视为不稳定或不可站立表面。
        /// </summary>
        [Tooltip("最大可站立斜坡角度（度）")]
        [Range(20f, 60f)] public float MaxSlopeAngle = 50f;

        /// <summary>
        /// 可自动跨越的最大台阶高度。
        /// 用于平滑上下小台阶，避免角色“卡边”。
        /// </summary>
        [Tooltip("最大可跨越台阶高度（米）")]
        public float StepHeight = 0.3f;


        // ==================== 检测距离 ====================
        /// <summary>
        /// 地面状态下的检测距离。
        /// 用于维持 Grounded 状态，避免微小离地抖动。
        /// </summary>
        [Tooltip("地面状态检测距离（米），用于稳定贴地")]
        public float GroundCheckDistance = 0.15f;

        /// <summary>
        /// 空中状态下的检测距离。
        /// 通常比 GroundCheckDistance 更大，
        /// 用于高速下落或击飞时提前感知地面。
        /// </summary>
        [Tooltip("空中状态检测距离（米），用于提前感知地面")]
        public float AirborneCheckDistance = 0.5f;

        /// <summary>
        /// 地面吸附距离。
        /// 当检测到地面在该距离内时，
        /// 可强制将角色位置贴合地面，防止悬浮。
        /// </summary>
        [Tooltip("地面吸附距离（米），在此范围内强制贴地")]
        public float GroundSnapDistance = 0.1f;


        // ==================== 检测精度 ====================
        /// <summary>
        /// SphereCast 半径缩放系数。
        /// 用于避免检测到角色自身边缘或墙体。
        /// </summary>
        [Tooltip("SphereCast 半径缩放系数（避免碰到自身/墙体）")]
        [Range(0.8f, 0.99f)] public float RadiusScale = 0.9f;

        /// <summary>
        /// 皮肤宽度（安全边距）。
        /// 用于防止角色与地面产生数值级穿透或抖动。
        /// </summary>
        [Tooltip("皮肤宽度（米），用于避免数值穿透/抖动")]
        [Range(0.01f, 0.05f)] public float SkinWidth = 0.02f;


        // ==================== 稳定性与缓冲 ====================
        /// <summary>
        /// 状态切换所需的最小连续帧数。
        /// 用于防止 Grounded / Airborne 状态在临界情况下抖动。
        /// </summary>
        [Tooltip("状态切换所需最小连续帧数（防抖）")]
        [Range(1, 5)] public int StateChangeFrameThreshold = 2;

        /// <summary>
        /// 宽容时间（Coyote Time）。
        /// 角色离开地面后，仍可在该时间内视为可跳跃。
        /// 通常只对 WalkOff 生效。
        /// </summary>
        [Tooltip("Coyote Time（秒），离地后仍可跳跃的宽容时间")]
        public float CoyoteTime = 0.1f;

        /// <summary>
        /// 落地缓冲时间。
        /// 用于在落地瞬间维持 Landing 状态，
        /// 以便播放落地动画或处理技能取消。
        /// </summary>
        [Tooltip("落地缓冲时间（秒），用于落地动画/取消窗口")]
        public float LandingBufferTime = 0.1f;


        // ==================== 边缘检测 ====================
        /// <summary>
        /// 是否启用边缘检测。
        /// 用于识别角色是否站在平台边缘。
        /// </summary>
        [Tooltip("是否启用边缘检测")]
        public bool EnableEdgeDetection = true;

        /// <summary>
        /// 边缘检测射线数量。
        /// 数量越多，边缘判定越精确，但性能开销越高。
        /// </summary>
        [Tooltip("边缘检测射线数量（越多越精确，开销越大）")]
        [Range(4, 12)] public int EdgeRayCount = 8;

        /// <summary>
        /// 支撑比例阈值。
        /// 当射线命中比例低于该值时，视为处于边缘。
        /// </summary>
        [Tooltip("支撑比例阈值，低于此值判定为边缘")]
        [Range(0.3f, 0.7f)] public float EdgeSupportThreshold = 0.5f;

        /// <summary>
        /// 边缘检测间隔帧数。
        /// 用于降低边缘检测的频率，提升性能。
        /// </summary>
        [Tooltip("边缘检测间隔帧数（降低频率）")]
        [Range(1, 5)] public int EdgeCheckInterval = 3;


        // ==================== 性能优化 ====================
        /// <summary>
        /// 是否降低空中状态下的地面检测频率。
        /// 对高速下落或击飞状态尤为有效。
        /// </summary>
        [Tooltip("空中状态是否降频检测地面")]
        public bool ReduceAirborneCheckFrequency = true;

        /// <summary>
        /// 空中检测的帧间隔。
        /// 数值越大，检测越少，性能越好，但响应稍慢。
        /// </summary>
        [Tooltip("空中地检帧间隔（数值越大检测越少）")]
        [Range(2, 4)] public int AirborneCheckInterval = 2;
        
        // ==================== 预计算数据 ====================

        /// <summary>
        /// 最大斜坡角对应的余弦值。
        /// 用于与法线点乘结果比较，避免每帧计算 Cos。
        /// </summary>
        [NonSerialized] public float CosMaxSlopeAngle;


        /// <summary>
        /// 初始化配置。
        /// 需在运行前调用一次，用于预计算常用数值。
        /// </summary>
        public void Initialize()
        {
            CosMaxSlopeAngle = Mathf.Cos(MaxSlopeAngle * Mathf.Deg2Rad);
        }

        /// <summary>
        /// 克隆一份配置实例。
        /// 常用于：
        /// - 角色独立参数
        /// - 运行时临时修改
        /// </summary>
        public GroundDetectorConfig Clone()
        {
            var clone = (GroundDetectorConfig)MemberwiseClone();
            clone.Initialize();
            return clone;
        }
    }
}
