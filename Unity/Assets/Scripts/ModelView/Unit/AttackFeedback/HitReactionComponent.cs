using System;
using Animancer;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 受击状态
    /// </summary>
    public enum HitState
    {
        None = 0,
        Stun = 1,       // 硬直
        Knockback = 2,  // 击退中
        Airborne = 3,   // 浮空中
        Falling = 4,    // 下落中
        Knockdown = 5,  // 倒地中
        GetUp = 6,      // 起身中
    }

    /// <summary>
    /// 受击反应组件
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class HitReactionComponent : Entity, IAwake<Transform>, IDestroy, IUpdate
    {
        public Transform Owner {get; set;}
        
        public Unit OwnerUnit {get; set;}
        
        public HitStopComponent HitStop {get; set;}

        /// <summary>
        /// 地面检测组件（建议作为“是否落地/地面高度”的唯一事实来源）。
        /// </summary>
        public CheckGroundedComponent Ground { get; set; }

        /// <summary>
        /// 移动意图（用于注入受击冲量）。
        /// </summary>
        public LocomotionIntentComponent LocomotionIntent { get; set; }

        #region 驱动/配置（可扩展）

        /// <summary>
        /// 动画淡入淡出时长（秒）。商业级：统一入口，可由配置覆盖。
        /// </summary>
        public float AnimationFadeSec { get; set; } = 0.05f;

        /// <summary>
        /// 击退衰减（每帧乘系数，combat-time）。
        /// </summary>
        public float KnockbackDamping { get; set; } = 0.90f;

        /// <summary>
        /// 空中水平衰减（每帧乘系数，combat-time）。
        /// </summary>
        public float AirborneHorizontalDamping { get; set; } = 0.95f;

        /// <summary>
        /// 进入受击时是否禁用角色移动（玩家通常需要）。
        /// </summary>
        public bool DisableMovementOnHit { get; set; } = true;

        /// <summary>
        /// 进入受击时是否取消攻击（用于“被打断”）。
        /// </summary>
        public bool CancelAttackOnHit { get; set; } = true;

        #endregion
        #region 运行时数据
        
        // 重力/贴地/落地高度由 CharacterControllerComponent + CheckGroundedComponent 统一负责。

        /// <summary>
        /// 起身(GetUp)兜底超时(ms, combat-time)。
        /// </summary>
        public int GetUpTimeoutMs { get; set; } = 1500;

        /// <summary>
        /// 受击会话：外部能力锁是否已 acquire。
        /// 约束：一次“受击会话”（从 None 进入任意受击状态，到回到 None）只允许 acquire 一次，结束时 release 一次。
        /// </summary>
        public bool HitSessionLocksAcquired { get; set; }

        /// <summary>
        /// 受击会话：是否已对 Ground 做过临时配置增强（用于空中落地判定更及时）。
        /// 同样是会话级别 acquire/release，避免多次受击叠加导致 InhibitReduceFrequencyCount 残留。
        /// </summary>
        public bool HitSessionGroundBoosted { get; set; }
        
        #endregion

        #region 状态
        
        /// <summary>当前视觉受击状态</summary>
        public HitState CurrentState { get; set; } = HitState.None;

        /// <summary>当前受击反应类型（用于动画合成）</summary>
        public HitReactionType CurrentReactionType { get; set; } = HitReactionType.None;
        
        /// <summary>当前动画状态</summary>
        public AnimancerState CurrentAnimState { get; set; }
        
        /// <summary>硬直结束时间</summary>
        public long StunEndTime { get; set; }
        
        /// <summary>当前物理运动类型</summary>
        public HitMotionType CurrentMotionType { get; set; }
        
        /// <summary>当前运动力度/速度</summary>
        public float CurrentMotionSpeed { get; set; }

        /// <summary>运动起始力度（用于曲线缩放基准）</summary>
        public float MotionBaseForce { get; set; }
        
        /// <summary>当前运动曲线</summary>
        public AnimationCurve CurrentMotionCurve { get; set; }
        
        /// <summary>运动方向</summary>
        public Vector3 MotionDirection { get; set; }
        
        /// <summary>运动开始时间 (combat-time)</summary>
        public long MotionStartTime { get; set; }
        
        /// <summary>运动结束时间 (combat-time)</summary>
        public long MotionEndTime { get; set; }
        
        /// <summary>倒地时间</summary>
        public long KnockdownEndTime { get; set; }
        
        /// <summary>倒地持续时间（毫秒）</summary>
        public int KnockdownDurationMs { get; set; } = 1000;

        /// <summary>进入起身状态的时间点（combat-time）。</summary>
        public long GetUpStartTime { get; set; }
        
        #endregion
        
        #region 属性
        
        /// <summary>是否处于受击状态</summary>
        public bool IsInHitReaction => CurrentState != HitState.None;
        
        /// <summary>是否可以被攻击</summary>
        public bool CanBeHit => CurrentState != HitState.GetUp;
        
        /// <summary>是否在空中</summary>
        public bool IsAirborne => CurrentState == HitState.Airborne || CurrentState == HitState.Falling;
        
        /// <summary>是否倒地</summary>
        public bool IsKnockdown => CurrentState == HitState.Knockdown;
        
        #endregion

        #region 事件
        
        /// <summary>受击开始事件</summary>
        public Action<HitState> OnHitReactionStart;
        
        /// <summary>受击结束事件</summary>
        public Action OnHitReactionEnd;
        
        /// <summary>落地事件</summary>
        public Action OnLanded;
        
        #endregion
    }
}