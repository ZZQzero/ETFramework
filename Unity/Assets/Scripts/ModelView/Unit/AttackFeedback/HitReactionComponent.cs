using System;
using Animancer;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 受击状态
    /// </summary>
    public enum HitState : byte
    {
        None = 0,
        /// <summary>地面受击（站立/行走时被打中）。</summary>
        GroundedHit = 1,
        /// <summary>空中受击（被击飞/浮空中）。</summary>
        AirborneHit = 2,
        /// <summary>倒地中受击。</summary>
        KnockdownHit = 3,
        /// <summary>起身中受击。</summary>
        GetUpHit = 4,
    }
    
    /// <summary>
    /// 受击表现类型
    /// </summary>
    public enum HitReactionType : byte
    {
        None = 0,

        LightHit,    // 轻击（地面）
        HeavyHit,    // 重击（地面）
        GroundToAir,     // 击飞表现（进入空中）
        AirCombo,   // 空中受击（非终结）
        AirToGround,   // 砸地表现(从空中落地)
        Knockdown,  // 躺地，在地面击倒
        Pull,       // 拉拽
        GetUp,       // 起身
    }

    /// <summary>
    /// 受击反应组件
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class HitReactionComponent : Entity, IAwake<Transform>, IDestroy, IUpdate
    {
        public Transform Owner {get; set;}
        
        public Unit OwnerUnit {get; set;}
        
        private ComponentRef<CombatConfigComponent> _combatConfigRef;
        private ComponentRef<CheckGroundedComponent> _groundedRef;
        private ComponentRef<LocomotionIntentComponent> _locomotionIntentRef;
        private ComponentRef<HitStopComponent> _hitStopRef;
        private ComponentRef<AirComboComponent> _airComboRef;
        private ComponentRef<AttackComponent> _attackRef;

        public void InitComponentRefs(Unit unit)
        {
            _combatConfigRef = new ComponentRef<CombatConfigComponent>(unit);
            _groundedRef = new ComponentRef<CheckGroundedComponent>(unit);
            _locomotionIntentRef = new ComponentRef<LocomotionIntentComponent>(unit);
            _attackRef = new ComponentRef<AttackComponent>(unit);
            _hitStopRef = new ComponentRef<HitStopComponent>(unit);
            _airComboRef = new ComponentRef<AirComboComponent>(unit);
        }

        public HitStopComponent HitStop => _hitStopRef.Get();
        public CheckGroundedComponent Ground => _groundedRef.Get();
        public LocomotionIntentComponent LocomotionIntent => _locomotionIntentRef.Get();
        public AirComboComponent AirCombo => _airComboRef.Get();
        public CombatConfigComponent CombatConfig => _combatConfigRef.Get();
        public AttackComponent Attack => _attackRef.Get();
        /// <summary>
        /// 进入受击时是否取消攻击（用于“被打断”）。
        /// </summary>
        public bool CancelAttackOnHit { get; set; } = true;
        
        #region 运行时数据
        
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
        /// 是否已抑制地检空中降频，保证空中受击期间每帧检测。
        /// </summary>
        public bool GroundFrequencyInhibited { get; set; }

        /// <summary>
        /// 本单位当前“允许播放哪些受击表现分组”（运行时缓存，来自 ProfileLibrary/配置）。
        /// </summary>
        public HitReactionGroup AllowedReactionGroups { get; set; } = HitReactionGroup.All;

        /// <summary>
        /// 本单位当前“允许播放哪些受击状态动画”（运行时缓存，来自 ProfileLibrary/配置）。
        /// </summary>
        public HitStateVisualMask AllowedStateVisuals { get; set; } = HitStateVisualMask.All;

        public float FirstUpForce;
        
        #endregion

        #region 状态

        /// <summary>
        /// 当前受击“规则状态”（Gameplay State）。
        /// - 规则先算并生效：该状态决定锁定/倒地/起身/空中等逻辑
        /// - 不等价于“是否播放动画”（见 <see cref="VisualState"/>）
        /// </summary>
        public HitState CurrentHitState { get; set; } = HitState.None;

        /// <summary>
        /// 当前受击“原始反应类型”（Desired ReactionType）。
        /// - 该值代表本次命中希望表达的结果
        /// </summary>
        public HitReactionType CurrentReactionType { get; set; } = HitReactionType.None;
        /// <summary>当前物理运动类型</summary>
        public HitMotionType CurrentMotionType { get; set; }
        
        /// <summary>当前动画状态</summary>
        public bool CurrentAnimEnd { get; set; }
        
        /// <summary>硬直结束时间，==>当前时间 + 攻击时间 + 硬值时间</summary>
        public long HitStunEndTimeMs { get; set; }
        
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

        /// <summary>首次击飞时的地面高度（世界 Y），用于计算空中绝对高度上限。</summary>
        public float AirborneOriginHeight { get; set; }

        /// <summary>
        /// 击飞保护帧计数器。
        /// EnterAirborneFromGrounded 后物理尚未将角色抬离地面，需屏蔽 N 帧的落地判定，
        /// 避免 Ground.Detect() 在同帧仍检测到地面而误判立即落地。
        /// </summary>
        public int AirborneGraceFramesLeft { get; set; }

        /// <summary>空中绝对高度上限（世界 Y），超过此高度时抑制向上冲量。</summary>
        public float MaxAirborneHeight { get; set; }

        /// <summary>拴系锚点：最后一次命中时的攻击者 XZ 位置（地面+空中共用）</summary>
        public Vector3 TetherAnchorPos { get; set; }

        /// <summary>攻击者 Transform 引用（NormalHit 时用于每帧实时跟踪锚点位置）</summary>
        public Transform TetherAnchorTransform { get; set; }

        #endregion
        
        #region 属性
        
        /// <summary>是否处于受击状态</summary>
        public bool IsInHitReaction => CurrentHitState != HitState.None;
        
        /// <summary>是否在空中</summary>
        public bool IsAirborne => CurrentHitState == HitState.AirborneHit;
        
        /// <summary>是否倒地</summary>
        public bool IsKnockdown => CurrentHitState == HitState.KnockdownHit;
        
        #endregion

        #region 事件
        
        /// <summary>受击开始事件</summary>
        public Action<HitState,HitReactionType> OnHitReactionStart;
        
        /// <summary>受击结束事件</summary>
        public Action OnHitReactionEnd;
        
        #endregion
    }
}