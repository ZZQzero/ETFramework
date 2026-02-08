using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 攻击组件 - 负责管理攻击连击逻辑
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class AttackComponent : Entity, IAwake, IDestroy, IUpdate, IFixedUpdate
    {
        private ComponentRef<CombatContextComponent> combatContextRef;
        private ComponentRef<MovementContextComponent> movementContextRef;
        private ComponentRef<LocomotionIntentComponent> locomotionIntentRef;
        private ComponentRef<GameObjectComponent> gameObjectRef;
        private ComponentRef<AnimatorComponent> animatorRef;
        private ComponentRef<AttackCommandComponent> attackCommandRef;
        private ComponentRef<AttackCatalogComponent> attackCatalogRef;
        private ComponentRef<HitStopComponent> hitStopRef;
        private ComponentRef<CameraFollowComponent> cameraFollowRef;
        private ComponentRef<TimerComponent> timerRef;

        public void InitComponentRefs(Unit unit, Entity root)
        {
            this.combatContextRef = new ComponentRef<CombatContextComponent>(unit);
            this.movementContextRef = new ComponentRef<MovementContextComponent>(unit);
            this.locomotionIntentRef = new ComponentRef<LocomotionIntentComponent>(unit);
            this.gameObjectRef = new ComponentRef<GameObjectComponent>(unit);
            this.animatorRef = new ComponentRef<AnimatorComponent>(unit);
            this.attackCommandRef = new ComponentRef<AttackCommandComponent>(unit);
            this.attackCatalogRef = new ComponentRef<AttackCatalogComponent>(unit);
            this.hitStopRef = new ComponentRef<HitStopComponent>(unit);
            this.cameraFollowRef = new ComponentRef<CameraFollowComponent>(root);
            this.timerRef = new ComponentRef<TimerComponent>(root);
        }

        public CombatContextComponent CombatContext => this.combatContextRef.Get();
        public MovementContextComponent MovementContext => this.movementContextRef.Get();
        public LocomotionIntentComponent LocomotionIntent => this.MovementContext?.LocomotionIntent ?? this.locomotionIntentRef.Get();

        public GameObjectComponent GameObjectComponent => this.gameObjectRef.Get();
        public AnimatorComponent AnimatorComponent => this.animatorRef.Get();

        public AttackCommandComponent AttackCommand => this.CombatContext?.AttackCommand ?? this.attackCommandRef.Get();
        public AttackCatalogComponent AttackCatalog => this.CombatContext?.AttackCatalog ?? this.attackCatalogRef.Get();
        public HitStopComponent HitStop => this.CombatContext?.HitStop ?? this.hitStopRef.Get();
        public CameraFollowComponent CameraFollow => this.cameraFollowRef.Get();
        public TimerComponent TimerComponent => this.timerRef.Get();

        public GameObject EffectRoot { get; set; }
        
        public Unit Unit { get; set; }
        
        /// <summary>当前已加载的技能ID（对应 AttackCatalog/命令的 SkillId）</summary>
        public int LoadedSkillId { get; set; }
        
        /// <summary>配置加载中（用于命令到来时的兜底）</summary>
        public bool IsLoadingConfig { get; set; }

        /// <summary>等待配置就绪后执行的一条命令（最小可用：避免 Update 中 await）</summary>
        public bool HasPendingCommand { get; set; }
        public AttackCommandComponent.AttackCommand PendingCommand;
        #region 配置数据
        
        /// <summary>攻击配置</summary>
        public AttackConfig Config { get; set; }
        
        #endregion

        #region 运行时状态
        
        /// <summary>当前攻击状态</summary>
        public AttackState State { get; set; } = AttackState.Idle;
        
        /// <summary>当前攻击段索引</summary>
        public int CurrentSegmentIndex { get; set; } = -1;
        
        /// <summary>当前攻击段数据</summary>
        public AttackSegmentData CurrentSegment { get; set; }
        
        /// <summary>当前动画状态</summary>
        public AnimancerState CurrentAnimState { get; set; }
        
        /// <summary>连击计数</summary>
        public int ComboCount { get; set; }

        /// <summary>连击超时定时器ID</summary>
        public long ComboTimeoutAtCombatMs { get; set; }

        /// <summary>
        /// 进入 Recovery 后会启动一次性“combat-time 截止点”，到期后淡出攻击层露出 Layer0 的 Move/Idle。
        /// 若接段/重起手/退出攻击，会重置/清空该截止点。
        /// </summary>
        public long AttackLayerFadeOutAtCombatMs { get; set; }

        /// <summary>当前攻击段是否已经自然结束（避免 ExitAttackState 重复触发 OnAttackEnd）</summary>
        public bool CurrentSegmentEnded { get; set; }

        /// <summary>输入缓冲窗口是否已打开（由 AnimancerEvent 或轮询兜底驱动）</summary>
        public bool IsInputBufferWindowOpen { get; set; }

        /// <summary>取消窗口是否已打开（由 AnimancerEvent 或轮询兜底驱动）</summary>
        public bool IsCancelWindowOpen { get; set; }
        
        #endregion

        #region 输入缓冲
        
        /// <summary>是否有缓冲输入</summary>
        public bool HasBufferedInput { get; set; }
        
        /// <summary>缓冲输入类型</summary>
        public ComboInputType BufferedInputType { get; set; } = ComboInputType.None;

        /// <summary>缓冲输入时间（用于 InputBufferWindowMs 过期）</summary>
        public long BufferedInputTime { get; set; }
        
        /// <summary>最后输入时间</summary>
        public long LastInputTime { get; set; }
        
        #endregion

        #region 命中检测
        
        /// <summary>当前攻击段是否已命中</summary>
        public bool HasHitThisSegment { get; set; }
        
        /// <summary>本次攻击已命中的目标ID集合</summary>
        public HashSet<GameObject> HitTargetsThisSegment { get; set; } = new HashSet<GameObject>();
        
        /// <summary>连击期间累计命中数</summary>
        public int TotalHitCount { get; set; }

        /// <summary>
        /// HitBox 激活标记（运行时状态，位掩码）：
        /// - 每段攻击的 HitBox 数量上限非常小（<=16），用位掩码比数组更轻量
        /// - bit i = 1 表示当前段第 i 个 HitBox 处于激活窗口
        /// - 由 Animancer Events 在窗口开始/结束时置位/清位
        /// - 命中检测在 FixedUpdate 中读取
        /// 说明：运行时状态必须放在组件中，避免污染配置对象。
        /// </summary>
        public ulong HitBoxActiveMask;
        
        #endregion

        #region AttachedActives（运行时显隐控制）

        /// <summary>
        /// Active 轨道：记录被本次攻击流程“接管过”的对象初始 activeSelf，用于退出攻击/切段时恢复。
        /// </summary>
        public Dictionary<GameObject, bool> AttachedActiveOriginalStates { get; set; } = new Dictionary<GameObject, bool>();

        /// <summary>
        /// Active 轨道：引用计数（支持同一对象多个区间重叠）。
        /// - Start：+1
        /// - End：-1
        /// - Count>0：在区间内显示；Count==0：区间外隐藏（但退出攻击会恢复到 OriginalStates）
        /// </summary>
        public Dictionary<GameObject, int> AttachedActiveRefCounts { get; set; } = new Dictionary<GameObject, int>();

        #endregion

        #region 目标锁定

        /// <summary>攻击锁定目标（整个连击会话共享，段切换时不清空）</summary>
        public Transform LockedTarget { get; set; }

        /// <summary>锁定目标对应的 Unit（用于有效性检查）</summary>
        public Unit LockedTargetUnit { get; set; }

        #endregion

        #region 位移控制
        
        /// <summary>位移开始位置</summary>
        public Vector3 MovementStartPosition { get; set; }
        
        /// <summary>位移目标位置</summary>
        public Vector3 MovementTargetPosition { get; set; }
        
        /// <summary>位移是否激活</summary>
        public bool IsMovementActive { get; set; }
        
        /// <summary>追踪目标</summary>
        public Transform TrackTarget { get; set; }

        /// <summary>
        /// 单位视图根节点（用于位移基准、挂点、VFX/SFX、命中方向等）。
        /// 注意：这是“OwnerTransform”，不是“Player”，避免语义误导。
        /// </summary>
        public Transform OwnerTransform;
        #endregion

        #region 便捷属性
        
        /// <summary>是否正在攻击</summary>
        public bool IsAttacking => State == AttackState.Attacking;

        /// <summary>是否处于攻击流程中（包含后摇/顿帧），用于动画/移动系统判定</summary>
        public bool IsInAttack => State != AttackState.Idle;
        
        /// <summary>是否可以输入缓冲</summary>
        public bool CanBufferInput
        {
            get
            {
                if (CurrentAnimState == null || CurrentSegment == null)
                {
                    return false;
                }
                return CurrentAnimState.HasEvents
                    ? IsInputBufferWindowOpen
                    : CurrentAnimState.NormalizedTime >= (CurrentSegment.TimeWindow != null ? CurrentSegment.TimeWindow.GetInputBufferStart01() : 0f);
            }
        }
        
        /// <summary>是否可以取消攻击</summary>
        public bool CanCancelAttack
        {
            get
            {
                if (CurrentAnimState == null || CurrentSegment == null)
                {
                    return false;
                }
                return CurrentAnimState.HasEvents
                    ? IsCancelWindowOpen
                    : CurrentAnimState.NormalizedTime >= (CurrentSegment.TimeWindow != null ? CurrentSegment.TimeWindow.GetCancelableTime01() : 0f);
            }
        }
        
        /// <summary>当前动画归一化时间</summary>
        public float CurrentNormalizedTime
        {
            get
            {
                if (CurrentAnimState == null)
                {
                    return 0f;
                }
                return CurrentAnimState.NormalizedTime;
            }
        }
        
        #endregion

        #region 事件

        /// <summary>攻击开始事件</summary>
        public Action<int> OnAttackStart;
        
        /// <summary>攻击结束事件</summary>
        public Action<int> OnAttackEnd;
        
        /// <summary>命中事件</summary>
        public Action<GameObject, AttackSegmentData> OnHit;
        
        /// <summary>连击重置事件</summary>
        public Action OnComboReset;
        
        /// <summary>连击数变化事件</summary>
        public Action<int> OnComboCountChanged;
        
        #endregion
    }
}