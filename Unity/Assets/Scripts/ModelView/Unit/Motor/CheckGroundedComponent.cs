using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    #region 数据结构
    
    public enum GroundState : byte
    {
        Grounded,         // 稳定站立
        OnStableSlope,    // 可站立斜坡
        OnUnstableSlope,  // 滑落斜坡
        OnEdge,           // 平台边缘
        Airborne,         // 空中（上升或平飞）
        Falling,          // 下落
        Landing           // 落地缓冲
    }

    /// <summary>
    /// 角色处于空中状态（Airborne）时的成因类型。
    /// 用于区分“主动 / 被动 / 战斗行为”，
    /// 直接影响：
    /// - 动画播放
    /// - 技能可用性
    /// - 受击 / 取消规则
    /// - 落地硬直与CoyoteTime
    /// </summary>
    public enum AirborneReason : byte
    {
        /// <summary>
        /// 未处于空中，或空中原因无效
        /// - Grounded 状态默认值
        /// - 状态重置占位
        /// </summary>
        None,

        /// <summary>
        /// 主动跳跃
        /// 触发方式：
        /// - 玩家输入 Jump
        /// 
        /// 行为特征：
        /// - 允许空中技能
        /// - 允许二段跳（如果有）
        /// - 落地通常为“软着陆”
        /// 
        /// 常见用途：
        /// - JumpStart / JumpLoop / JumpLand 动画
        /// - 禁用 CoyoteTime
        /// </summary>
        Jump,

        /// <summary>
        /// 行走 / 奔跑中离开地面（掉下平台）
        /// 触发方式：
        /// - 地面支撑消失（Edge / Gap）
        /// 
        /// 行为特征：
        /// - 通常允许 CoyoteTime
        /// - 初始无向上速度
        /// - 动画多为失足 / 下坠
        /// 
        /// 常见用途：
        /// - 防误操作补偿（宽容判定）
        /// - EdgeJump / 悬崖跳
        /// </summary>
        WalkOff,

        /// <summary>
        /// 被技能或外力击飞（上抛）
        /// 触发方式：
        /// - 技能命中产生正向 Y 速度
        /// 
        /// 行为特征：
        /// - 强制空中状态
        /// - 通常禁止主动跳跃
        /// - 可进入受击 / 浮空连段
        /// 
        /// 常见用途：
        /// - 击飞动画
        /// - 空中追击判定
        /// - 浮空高度与时间计算
        /// </summary>
        Launched,

        /// <summary>
        /// 空中被连续攻击（浮空连段中）
        /// 触发方式：
        /// - Launched 后再次受击
        /// - 空中追击技能命中
        /// 
        /// 行为特征：
        /// - 通常禁止所有主动输入
        /// - 受击动画可被刷新
        /// - 落地往往带有硬直
        /// 
        /// 常见用途：
        /// - 空中连段维持
        /// - 防止空中翻滚 / 跳跃
        /// </summary>
        Juggled,

        /// <summary>
        /// 被击倒 / 砸地型空中状态
        /// 触发方式：
        /// - 下砸技能
        /// - 强制倒地效果
        /// 
        /// 行为特征：
        /// - 空中阶段不可操作
        /// - 落地必定进入倒地 / 硬直
        /// - 常伴随震屏、特效
        /// 
        /// 常见用途：
        /// - DownFall / Slam 动画
        /// - 起身保护时间
        /// </summary>
        Knockdown
    }


    public enum GroundSurfaceType : byte
    {
        Default,
        Ice,//冰面
        Sand,//沙地
        Water,//水面/浅水区域
        Mud,//泥地/沼泽
        Bounce//弹跳地面
    }


    /// <summary>
    /// 地面命中信息。
    /// 描述角色当前与地面的“接触关系快照”，
    /// 是地面检测系统对外输出的核心数据结构。
    ///
    /// 该结构通常被用于：
    /// - GroundState 判定
    /// - 角色移动修正（贴地 / 斜坡）
    /// - 动画状态切换（落地 / 滑行 / 边缘）
    /// - 技能规则判断（是否可释放 / 是否可取消）
    /// </summary>
    [Serializable]
    public struct GroundHitInfo
    {
        /// <summary>
        /// 是否检测到有效地面。
        /// false 表示当前视为完全离地状态。
        /// </summary>
        public bool HasGround;

        /// <summary>
        /// 当前地面是否稳定、可站立。
        /// 通常由斜坡角度、法线方向等综合判定。
        /// </summary>
        public bool IsStable;

        /// <summary>
        /// 地面接触点（世界坐标）。
        /// 常用于贴地、对齐角色位置或播放落地特效。
        /// </summary>
        public Vector3 Point;

        /// <summary>
        /// 地面法线（世界坐标）。
        /// 用于：
        /// - 斜坡判定
        /// - 移动方向投影
        /// - 角色朝向修正
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// 角色到地面的垂直距离。
        /// 用于判断是否需要 Snap 到地面。
        /// </summary>
        public float Distance;

        /// <summary>
        /// 当前地面的斜坡角（度）。
        /// 由法线与世界 Up 计算得出。
        /// </summary>
        public float SlopeAngle;

        /// <summary>
        /// 地面支撑比例（0 ~ 1）。
        /// 表示角色脚底被地面支撑的覆盖程度。
        /// 常用于边缘判定。
        /// </summary>
        public float SupportRatio;

        /// <summary>
        /// 是否处于平台或地形边缘。
        /// 当支撑比例低于阈值时为 true。
        /// </summary>
        public bool IsOnEdge;

        /// <summary>
        /// 边缘方向（世界坐标）。
        /// 指向“离开地面支撑”的方向，
        /// 常用于边缘滑落、边缘吸附或修正移动。
        /// </summary>
        public Vector3 EdgeDirection;


        // ==================== 引用数据（运行时） ====================

        /// <summary>
        /// 命中的地面 Collider。
        /// 仅用于运行时逻辑，不参与序列化。
        /// </summary>
        [NonSerialized] public Collider Collider;

        /// <summary>
        /// 地面 Transform。
        /// 通常用于移动平台或旋转平台的相对运动计算。
        /// </summary>
        [NonSerialized] public Transform GroundTransform;

        /// <summary>
        /// 地面所在的 Layer。
        /// 可用于快速区分普通地面、平台、特殊地形。
        /// </summary>
        public int GroundLayer;

        /// <summary>
        /// 地面表面类型。
        /// 用于影响角色移动、技能和特效表现。
        /// </summary>
        public GroundSurfaceType SurfaceType;


        // ==================== 移动平台支持 ====================

        /// <summary>
        /// 角色在地面 Transform 下的本地坐标。
        /// 用于计算平台移动时角色的相对位移。
        /// </summary>
        public Vector3 LocalPositionOnGround;

        /// <summary>
        /// 上一帧地面的位置（世界坐标）。
        /// 用于检测地面位移或计算平台速度。
        /// </summary>
        public Vector3 PrevGroundPosition;

        /// <summary>
        /// 上一帧地面的旋转（世界坐标）。
        /// 用于旋转平台的补偿计算。
        /// </summary>
        public Quaternion PrevGroundRotation;


        /// <summary>
        /// 重置命中信息为默认状态。
        /// 通常在每帧检测前调用。
        /// </summary>
        public void Reset()
        {
            HasGround = false;
            IsStable = false;
            Point = Vector3.zero;
            Normal = Vector3.up;
            Distance = float.MaxValue;
            SlopeAngle = 0f;
            SupportRatio = 1f;
            IsOnEdge = false;
            EdgeDirection = Vector3.zero;

            Collider = null;
            GroundTransform = null;

            GroundLayer = 0;
            SurfaceType = GroundSurfaceType.Default;
        }
    }
    
    #endregion

    #region 组件

    /// <summary>
    /// 地面状态上下文（按状态分组）。
    /// </summary>
    [Serializable]
    public struct GroundStateContext
    {
        public GroundState State;
        public GroundState PrevState;
        public AirborneReason AirborneReason;
        public int ConsecutiveAirborneFrames;
        public int ConsecutiveGroundedFrames;
    }

    /// <summary>
    /// 地面时间上下文（按时间分组）。
    /// </summary>
    [Serializable]
    public struct GroundTimingContext
    {
        public float TimeLeftGround;
        public float TimeLanded;
        public float GroundedDuration;
        public float AirborneDuration;
        public bool InCoyoteTime;
    }

    /// <summary>
    /// 地面检测组件（Grounded Check）。
    /// 
    /// 职责：
    /// - 维护角色当前与地面的接触状态
    /// - 提供稳定、可预测的“是否在地面”判断
    /// - 支撑跳跃、下落、边缘、斜坡、平台等 ACT 行为
    ///
    /// 该组件只负责“状态与数据”，
    /// 不直接驱动移动或动画（由上层系统消费）。
    /// </summary>
    public class CheckGroundedComponent : Entity, IAwake<GameObject>, IDestroy
    {
        /// <summary>
        /// 地检外部请求类型（token kind）。
        /// 约定：仅由 <see cref="ET.CheckGroundedComponentSystem"/> 写入/读取。
        /// </summary>
        public const byte GroundDetectRequest_Disable = 1;
        public const byte GroundDetectRequest_Boost = 2;

        // ==================== 引用组件 ====================

        /// <summary>
        /// 单位视图根节点（通常是角色根节点）。
        /// 所有检测与位移计算的空间参考。
        /// </summary>
        public Transform OwnerTransform;

        /// <summary>
        /// 角色的 CapsuleCollider。
        /// 用于地面检测、忽略平台、碰撞体尺寸计算。
        /// </summary>
        public CapsuleCollider Capsule;

        /// <summary>
        /// 角色 Rigidbody。
        /// 用于获取垂直速度、判断下落状态等。
        /// </summary>
        public Rigidbody Rigidbody;


        // ==================== 配置 ====================

        /// <summary>
        /// 地面检测配置。
        /// 描述“怎么检测地面”，而不是“当前检测结果”。
        /// </summary>
        public GroundDetectorConfig Config;


        // ==================== 状态 ====================

        public GroundStateContext StateContext;

        /// <summary>
        /// 当前帧的地面命中信息。
        /// 是地面检测系统的核心输出。
        /// </summary>
        public GroundHitInfo GroundHit;


        // ==================== 时间相关 ====================

        public GroundTimingContext TimingContext;


        // ==================== 下跳 / 穿透平台 ====================

        /// <summary>
        /// 当前被忽略碰撞的可穿透平台 Collider。
        /// </summary>
        public Collider IgnoredPlatform;

        /// <summary>
        /// 忽略平台碰撞的截止时间。
        /// 超过该时间后会恢复碰撞。
        /// </summary>
        public float IgnorePlatformUntil;


        // ==================== 内部运行状态 ====================

        /// <summary>
        /// Capsule 半径缓存。
        /// 避免每帧访问 Collider 属性。
        /// </summary>
        public float CapsuleRadius;

        /// <summary>
        /// Capsule 高度缓存。
        /// </summary>
        public float CapsuleHeight;

        /// <summary>
        /// 最近一次“稳定站立”时的位置。
        /// 用于计算跌落高度。
        /// </summary>
        public Vector3 LastGroundedPosition;

        /// <summary>
        /// 抑制地检降频的计数器（引用计数）。
        /// 只要该值 > 0，即使 Config.ReduceAirborneCheckFrequency 为 true 也会强制每帧检测。
        /// 用于受击浮空等需要高频响应的场景。
        /// </summary>
        public int InhibitReduceFrequencyCount;

        // ==================== 外部请求（地检开关/频率） ====================
        //
        // 设计目标：
        // - 多来源可叠加（受击、空连、技能、Buff...）
        // - 由 Ground 自己维护引用计数与状态计算，调用方只持有 token 负责归还
        // - 避免多个系统直接写 Enable / InhibitReduceFrequencyCount 造成竞态与泄漏

        /// <summary>地检被外部请求“禁用”的引用计数。</summary>
        public int GroundDetectDisableCount;

        /// <summary>地检请求 token 自增序号。</summary>
        public long GroundDetectRequestSeq;

        /// <summary>
        /// 活跃请求表：token -> kind（1=Disable, 2=Boost）。
        /// 用于防止重复释放/错误释放造成计数错乱。
        /// </summary>
        public Dictionary<long, byte> GroundDetectRequests;

        /// <summary>
        /// 边缘检测计数器。
        /// 用于降低边缘检测的调用频率。
        /// </summary>
        public int EdgeCheckCounter;

        /// <summary>
        /// 总帧计数器。
        /// 常用于降频检测或调试。
        /// </summary>
        public int FrameCounter;
        
        /// <summary>
        ///  是否启用地面检测
        /// </summary>
        public bool Enable = true;
        // ==================== 预分配缓存（性能） ====================

        /// <summary>
        /// Overlap 检测用 Collider 缓存。
        /// 避免 GC。
        /// </summary>
        public readonly Collider[] OverlapBuffer = new Collider[8];

        /// <summary>
        /// SphereCast 检测结果缓存。
        /// </summary>
        public readonly RaycastHit[] SphereCastBuffer = new RaycastHit[8];

        /// <summary>
        /// Raycast 检测结果缓存。
        /// </summary>
        public readonly RaycastHit[] RaycastBuffer = new RaycastHit[8];

        /// <summary>
        /// 多点 Raycast 的起点缓存。
        /// </summary>
        public readonly Vector3[] RayOriginBuffer = new Vector3[5];


        // ==================== 事件 ====================

        /// <summary>
        /// 落地事件。
        /// 通常用于播放落地动画、音效、震屏等。
        /// </summary>
        public event Action OnLanded;

        /// <summary>
        /// 离开地面事件。
        /// 通常用于切换到空中动画或状态。
        /// </summary>
        public event Action OnLeftGround;

        /// <summary>
        /// 跌落伤害事件。
        /// 参数为跌落高度或等效伤害值。
        /// </summary>
        public event Action<float> OnFallDamage;


        // ==================== 事件触发封装 ====================

        public void InvokeLanded() => OnLanded?.Invoke();
        public void InvokeLeftGround() => OnLeftGround?.Invoke();
        public void InvokeFallDamage(float h) => OnFallDamage?.Invoke(h);

        /// <summary>
        /// 清理所有事件监听。
        /// 通常在组件销毁时调用，防止悬挂引用。
        /// </summary>
        public void ClearEvents()
        {
            OnLanded = null;
            OnLeftGround = null;
            OnFallDamage = null;
        }

    }

    #endregion
}