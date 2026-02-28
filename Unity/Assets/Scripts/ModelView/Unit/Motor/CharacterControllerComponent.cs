using UnityEngine;

namespace ET
{
    /// <summary>
    /// 角色运动执行组件（Motor）
    /// - 上层（玩家输入/AI/回放）只写入 Intent
    /// - 本组件只负责运动学执行，并把结果同步回 Unit.Position/Rotation
    /// - 不依赖 Rigidbody，使用 CapsuleCast Sweep & Slide 做碰撞解算
    /// - OnAnimatorMove 仅采集 Root Motion delta，所有运动逻辑在 Update 中统一执行
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class CharacterControllerComponent: Entity, IAwake<GameObject>, IUpdate, IOnAnimatorMove, IDestroy
    {
        public CapsuleCollider CapsuleCollider { get; set; }
        public Transform PlayerTransform { get; set; }
        public Unit Unit { get; set; }
        public Animator Animator { get; set; }
        
        private ComponentRef<CheckGroundedComponent> _groundedRef;
        private ComponentRef<LocomotionIntentComponent> _locomotionIntentRef;
        private ComponentRef<AttackComponent> _attackRef;
        private ComponentRef<HitReactionComponent> _hitReactionRef;
        private ComponentRef<HitStopComponent> _hitStopRef;
        private ComponentRef<AirComboComponent> _airComboRef;

        public void InitComponentRefs(Unit unit)
        {
            _groundedRef = new ComponentRef<CheckGroundedComponent>(unit);
            _locomotionIntentRef = new ComponentRef<LocomotionIntentComponent>(unit);
            _attackRef = new ComponentRef<AttackComponent>(unit);
            _hitReactionRef = new ComponentRef<HitReactionComponent>(unit);
            _hitStopRef = new ComponentRef<HitStopComponent>(unit);
            _airComboRef = new ComponentRef<AirComboComponent>(unit);
        }

        public CheckGroundedComponent Ground => _groundedRef.Get();
        public LocomotionIntentComponent LocomotionIntent => _locomotionIntentRef.Get();
        public AttackComponent Attack => _attackRef.Get();
        public HitReactionComponent HitReaction => _hitReactionRef.Get();
        public HitStopComponent HitStop => _hitStopRef.Get();
        public AirComboComponent AirCombo => _airComboRef.Get();

        // ===== 移动参数 =====
        
        public float MoveSpeed { get; set; } = 5f;
        public float Acceleration { get; set; } = 20f;
        public float Deceleration { get; set; } = 25f;
        public float RotationSpeed { get; set; } = 720f;
        
        /// <summary>
        /// 当前逻辑速度（包含 XZ 移动 + Y 重力/跳跃）
        /// </summary>
        public Vector3 CurrentVelocity { get; set; }

        // ===== 跳跃/重力 =====
        
        public float Gravity { get; set; } = 9.81f;
        public float JumpForce { get; set; } = 10f;
        public float GravityMultiplier { get; set; } = 1.5f;
        public bool JumpRequested { get; set; }

        // ===== Root Motion =====
        
        /// <summary>
        /// OnAnimatorMove 中累积的 Root Motion 位移（每帧在 Update 中清零）
        /// </summary>
        public Vector3 RootMotionDelta { get; set; }

        public int LastRootMotionFrame;

        // ===== 运行状态 =====
        
        public bool WasDrivenByExternalVelocity { get; set; }

        /// <summary>
        /// Sweep & Slide 最大迭代次数（撞墙→滑动→再撞角落）
        /// </summary>
        public int MaxSweepIterations = 3;
        // ===== Sweep 碰撞解算 =====
        
        /// <summary>
        /// Sweep 碰撞皮肤宽度（防止贴脸穿透）
        /// </summary>
        public float SkinWidth { get; set; } = 0.02f;
        
        /// <summary>
        /// CapsuleCast 预分配缓存（避免 GC）
        /// </summary>
        public readonly RaycastHit[] SweepHitBuffer = new RaycastHit[8];

        /// <summary>
        /// 碰撞检测 LayerMask（排除自身层）
        /// </summary>
        public LayerMask CollisionMask { get; set; }
        
        /// <summary>
        /// Capsule 半径缓存
        /// </summary>
        public float CapsuleRadius { get; set; }
        
        /// <summary>
        /// Capsule 高度缓存
        /// </summary>
        public float CapsuleHeight { get; set; }

        // ===== 动画参数 =====
        
        public float NormalizedAnimationSpeed { get; set; }
        public float VerticalAnimationSpeed { get; set; }
    }
}

