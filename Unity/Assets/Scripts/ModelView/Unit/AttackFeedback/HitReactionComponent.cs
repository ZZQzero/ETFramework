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
        /// <summary>地面受击（站立/行走时被打中）。</summary>
        Grounded = 1,
        /// <summary>空中（被击飞/浮空中）。</summary>
        Airborne = 2,
        /// <summary>空中硬直/终结（比普通空中更“终结态”）。</summary>
        AirFinisher = 3,
        /// <summary>倒地中。</summary>
        Knockdown = 4,
        /// <summary>起身中。</summary>
        GetUp = 5,
    }

    /// <summary>
    /// 空中受击会话的落地分流语义（不依赖 Ground.AirborneReason）。
    /// 说明：
    /// - Ground.AirborneReason 在落地事件触发后会被重置为 None（FixedUpdate 内），
    ///   用它在 Update 中做落地分流会产生时序竞态与不稳定表现。
    /// - 因此落地分流语义必须在“语义产生点”（击飞/砸地）固化，并在落地时单次消费。
    /// </summary>
    public enum PendingLandOutcome : byte
    {
        None = 0,
        Grounded = 1,
        Knockdown = 2,
    }

    /// <summary>
    /// 空中受击落地会话数据结构，封装落地语义相关字段。
    /// 职责：管理"空中→落地"的状态流转语义，避免多个分散字段导致的状态管理复杂度。
    /// </summary>
    public struct AirborneLandSession
    {
        /// <summary>本次空中会话的落地分流语义。</summary>
        public PendingLandOutcome Outcome;
        
        /// <summary>普通落地硬直（ms, combat-time）。</summary>
        public int LandingStunMs;
        
        /// <summary>落地事实已到达（由 Ground.OnLanded 事件置位）。</summary>
        public bool LandQueued;
        
        /// <summary>本次落地已被消费处理（确保只处理一次）。</summary>
        public bool LandHandled;
        
        /// <summary>落地发生时刻（combat-time），用于调试与一致性校验。</summary>
        public long LandCombatMs;
        
        /// <summary>重置会话状态。</summary>
        public void Reset()
        {
            this.Outcome = PendingLandOutcome.None;
            this.LandingStunMs = 0;
            this.LandQueued = false;
            this.LandHandled = false;
            this.LandCombatMs = 0;
        }
        
        /// <summary>
        /// 尝试消费落地事件。
        /// </summary>
        /// <returns>如果成功消费返回 true，否则返回 false。</returns>
        public bool TryConsumeLanding(out PendingLandOutcome outcome)
        {
            if (!this.LandQueued || this.LandHandled)
            {
                outcome = PendingLandOutcome.None;
                return false;
            }
            this.LandHandled = true;
            outcome = this.Outcome;
            return true;
        }
        
        /// <summary>落地已消费后清理语义（保留 LandHandled=true）。</summary>
        public void ClearAfterConsumed()
        {
            this.Outcome = PendingLandOutcome.None;
            this.LandingStunMs = 0;
            this.LandCombatMs = 0;
        }
    }

    /// <summary>
    /// 受击反应组件
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class HitReactionComponent : Entity, IAwake<Transform>, IDestroy, IUpdate
    {
        public Transform Owner {get; set;}
        
        public Unit OwnerUnit {get; set;}
        
        private ComponentRef<CombatContextComponent> combatContextRef;
        private ComponentRef<MovementContextComponent> movementContextRef;

        public void InitComponentRefs(Unit unit)
        {
            this.combatContextRef = new ComponentRef<CombatContextComponent>(unit);
            this.movementContextRef = new ComponentRef<MovementContextComponent>(unit);
        }

        public HitStopComponent HitStop => this.combatContextRef.Get()?.HitStop;
        public CheckGroundedComponent Ground => this.movementContextRef.Get()?.Ground;
        public LocomotionIntentComponent LocomotionIntent => this.movementContextRef.Get()?.LocomotionIntent;
        public AirComboComponent AirCombo => this.combatContextRef.Get()?.AirCombo;
        /// <summary>
        /// 进入受击时是否取消攻击（用于“被打断”）。
        /// </summary>
        public bool CancelAttackOnHit { get; set; } = true;
        
        #region 运行时数据

        #region 空中受击落地语义（会话级）

        /// <summary>
        /// 本次空中受击会话的落地分流语义。
        /// - 在 EnterAirborne/BeginAirSlam 等“语义产生点”写入
        /// - 在落地时单次消费（避免重复触发）
        /// </summary>
        public AirborneLandSession LandSession;

        /// <summary>
        /// Ground.OnLanded 订阅回调句柄（用于 Destroy 退订）。
        /// </summary>
        public Action GroundOnLandedHandler;

        /// <summary>
        /// AirCombo 事件订阅句柄（用于 Destroy 退订）。
        /// </summary>
        public Action<bool> AirComboGroundDetectHandler;

        public Action AirComboExitCompletedHandler;

        public bool AirComboEventsBound;

        /// <summary>
        /// 退出/下落期临时强制地检高频（引用计数）是否已加持。
        /// </summary>
        public bool GroundDetectBoosted;

        #endregion
        
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
        /// 本单位当前“允许播放哪些受击表现分组”（运行时缓存，来自 ProfileLibrary/配置）。
        /// </summary>
        public HitReactionGroup AllowedReactionGroups { get; set; } = HitReactionGroup.All;

        /// <summary>
        /// 本单位当前“允许播放哪些受击状态动画”（运行时缓存，来自 ProfileLibrary/配置）。
        /// </summary>
        public HitStateVisualMask AllowedStateVisuals { get; set; } = HitStateVisualMask.All;
        
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
        /// - 该值代表本次命中希望表达的结果语义
        /// - 最终是否播放/如何降级由 <see cref="VisualReactionType"/> 决定
        /// </summary>
        public HitReactionType CurrentReactionType { get; set; } = HitReactionType.None;

        /// <summary>
        /// 当前受击“视觉状态”（Visual State）。
        /// - 可能与 <see cref="CurrentHitState"/> 不一致：当配置禁播某些状态动画时，规则继续但视觉不播
        /// </summary>
        public HitState VisualState { get; set; } = HitState.None;

        /// <summary>
        /// 当前受击“视觉反应类型”（Visual ReactionType）。
        /// - 可能从 <see cref="CurrentReactionType"/> 降级（Control→Major→Minor→None）
        /// </summary>
        public HitReactionType VisualReactionType { get; set; } = HitReactionType.None;
        
        /// <summary>当前动画状态</summary>
        public bool CurrentAnimEnd { get; set; }
        
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
        public bool IsInHitReaction => CurrentHitState != HitState.None;

        /// <summary>
        /// 是否需要播放受击动画（视觉层）。
        /// - VisualState=None：完全不播受击动画
        /// - Grounded 且 VisualReactionType=None：不播地面受击动画（继续 Locomotion）
        /// </summary>
        public bool IsInHitVisual =>
            this.VisualState != HitState.None &&
            (this.VisualState != HitState.Grounded || this.VisualReactionType != HitReactionType.None);
        
        /// <summary>是否可以被攻击</summary>
        public bool CanBeHit => CurrentHitState != HitState.GetUp;
        
        /// <summary>是否在空中</summary>
        public bool IsAirborne => CurrentHitState == HitState.Airborne || CurrentHitState == HitState.AirFinisher;
        
        /// <summary>是否倒地</summary>
        public bool IsKnockdown => CurrentHitState == HitState.Knockdown;
        
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