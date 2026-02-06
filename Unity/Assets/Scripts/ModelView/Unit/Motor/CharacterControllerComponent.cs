using UnityEngine;

namespace ET
{
    /// <summary>
    /// 角色运动执行组件（Motor）
    /// - 上层（玩家输入/AI/回放）只写入 Intent
    /// - 本组件只负责运动学/物理执行，并把结果同步回 Unit.Position/Rotation
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class CharacterControllerComponent: Entity, IAwake<GameObject>, IUpdate,IFixedUpdate,IOnAnimatorMove,IDestroy
    {
        /// <summary>
        /// Unity Rigidbody组件引用
        /// </summary>
        public Rigidbody Rigidbody { get; set; }
        public CapsuleCollider CapsuleCollider { get; set; }
        private ComponentRef<MovementContextComponent> movementContextRef;
        public Unit Unit { get; set; }
        public Animator Animator { get; set; }
        private ComponentRef<CombatContextComponent> combatContextRef;
        /// <summary>
        /// 受击系统引用：用于在受击期间由受击系统接管速度（避免 Motor 自己减速/改写速度）。
        /// </summary>
        public void InitComponentRefs(Unit unit)
        {
            this.movementContextRef = new ComponentRef<MovementContextComponent>(unit);
            this.combatContextRef = new ComponentRef<CombatContextComponent>(unit);
        }

        public CheckGroundedComponent Ground => this.movementContextRef.Get()?.Ground;
        public LocomotionIntentComponent LocomotionIntent => this.movementContextRef.Get()?.LocomotionIntent;
        public AttackComponent Attack => this.combatContextRef.Get()?.Attack;
        public HitReactionComponent HitReaction => this.combatContextRef.Get()?.HitReaction;
        public HitStopComponent HitStop => this.combatContextRef.Get()?.HitStop;
        public AirComboComponent AirCombo => this.combatContextRef.Get()?.AirCombo;

        /// <summary>
        /// 移动速度（米/秒）
        /// </summary>
        public float MoveSpeed { get; set; } = 5f;
        /// <summary>
        /// 加速度（米/秒²）
        /// </summary>
        public float Acceleration { get; set; } = 20f;
        /// <summary>
        /// 减速度（米/秒²）
        /// </summary>
        public float Deceleration { get; set; } = 25f;
        /// <summary>
        /// 旋转速度（度/秒）
        /// </summary>
        public float RotationSpeed { get; set; } = 720f;
        /// <summary>
        /// 当前速度（用于平滑加速/减速）
        /// </summary>
        public Vector3 CurrentVelocity { get; set; }
        /// <summary>
        /// 是否启用移动（[已过时] 请优先使用 LocomotionIntent 的 Inhibitors 控制）
        /// </summary>
        public bool EnableMovement { get; set; } = true;
        // ===== 跳跃相关属性 =====
        //重力
        public float Gravity { get; set; } = 9.81f;
        /// <summary>
        /// 跳跃力（向上初速度，米/秒）
        /// </summary>
        public float JumpForce { get; set; } = 10f;
        /// <summary>
        /// 重力倍数（相对于标准物理重力的倍数，1.0 = 9.81 m/s²）
        /// </summary>
        public float GravityMultiplier { get; set; } = 1.5f;
        /// <summary>
        /// 跳跃请求标记（用于外部调用）
        /// </summary>
        public bool JumpRequested { get; set; }

        /// <summary>
        /// 上一帧是否由 ExternalTargetVelocity 驱动 XZ 速度。
        /// 用于在外部驱动结束时立即清零 CurrentVelocity 的 XZ，避免残留速度导致减速滑行。
        /// </summary>
        public bool WasDrivenByExternalVelocity { get; set; }

        // ===== 动画速度相关属性 =====

        /// <summary>
        /// 动画速度标准化值
        /// </summary>
        public float NormalizedAnimationSpeed { get; set; }
        /// <summary>
        /// 垂直动画速度（用于跳跃/下落动画）
        /// </summary>
        public float VerticalAnimationSpeed { get; set; }
    }
}

