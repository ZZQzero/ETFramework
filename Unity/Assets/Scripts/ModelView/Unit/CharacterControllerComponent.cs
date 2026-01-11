using UnityEngine;

namespace ET
{
    /// <summary>
    /// 角色移动控制组件
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class CharacterControllerComponent: Entity, IAwake<GameObject>, IUpdate,IFixedUpdate,IOnAnimatorMove,IDestroy
    {
        /// <summary>
        /// Unity Rigidbody组件引用
        /// </summary>
        public Rigidbody Rigidbody { get; set; }
        public CapsuleCollider CapsuleCollider { get; set; }
        public CheckGroundedComponent  Ground { get; set; }
        public InputComponent Input { get; set; }
        public Unit PlayerUnit { get; set; }

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
        /// 是否启用移动（可以通过设置这个来禁用移动）
        /// </summary>
        public bool EnableMovement { get; set; } = true;

        /// <summary>
        /// 外部运动驱动（例如攻击位移/冲刺），用于与常规移动解耦，避免在FixedUpdate里互相覆盖。
        /// </summary>
        public bool ExternalMotorActive { get; set; }

        /// <summary>
        /// 外部运动速度（世界空间），仅使用XZ分量（Y由重力/跳跃系统控制）。
        /// </summary>
        public Vector3 ExternalMotorVelocity { get; set; }
        
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

