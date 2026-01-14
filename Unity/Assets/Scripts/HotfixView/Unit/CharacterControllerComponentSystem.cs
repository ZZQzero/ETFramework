using UnityEngine;
using Unity.Mathematics;

namespace ET
{
    public static partial class CharacterControllerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CharacterControllerComponent self,GameObject player)
        {
            // 获取或添加Rigidbody组件
            self.Rigidbody = player.GetComponent<Rigidbody>();
            if (self.Rigidbody == null)
            {
                self.Rigidbody = player.AddComponent<Rigidbody>();
            }
            
            // Rigidbody
            self.Rigidbody.isKinematic = false;
            self.Rigidbody.useGravity = false; // 我们自己控制重力
            self.Rigidbody.freezeRotation = true; // 禁止物理翻滚
            self.Rigidbody.angularDamping = 5f;
            self.Rigidbody.linearDamping = 0f; // 我们自己计算摩擦力，设为0防止物理引擎干扰
            self.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // 消除视觉抖动关键
            self.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // 防止穿墙
            // 初始化速度为零，防止启动时有初始速度导致角色移动
            self.Rigidbody.linearVelocity = Vector3.zero;
            self.Rigidbody.angularVelocity = Vector3.zero;
            self.PlayerUnit = self.GetParent<Unit>();
            self.Ground = self.PlayerUnit.GetComponent<CheckGroundedComponent>();
            self.Input = self.PlayerUnit.GetComponent<InputComponent>();
            self.Attack = self.PlayerUnit.GetComponent<AttackComponent>();
            self.CapsuleCollider = player.GetComponent<CapsuleCollider>();
            self.CameraFollow = self.Root().GetComponent<CameraFollowComponent>();
            if (self.CameraFollow == null)
            {
                Log.Error("没有找到CameraFollowComponent组件");
            }
            if (self.CapsuleCollider == null)
            {
                Log.Warning($"CharacterControllerComponent需要CapsuleCollider组件，GameObject: {player.name}");
            }
            else
            {
                // 配置物理材质：低摩擦力，让角色贴着墙也能跳起来
                // 如果Collider已经有物理材质，使用现有的；否则创建新的
                PhysicsMaterial physicsMaterial = self.CapsuleCollider.material;
                if (physicsMaterial == null)
                {
                    physicsMaterial = new PhysicsMaterial("PlayerPhysicsMaterial");
                    self.CapsuleCollider.material = physicsMaterial;
                }
                
                // 设置低摩擦力参数（关键：让角色不会被墙"粘住"）
                physicsMaterial.dynamicFriction = 0.1f;  // 动态摩擦力：0.1（默认0.6太高，会粘墙）
                physicsMaterial.staticFriction = 0.2f;   // 静态摩擦力：0.2（默认0.6太高）
                physicsMaterial.bounciness = 0f;         // 弹性：0（不需要弹跳）
                physicsMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;  // 摩擦力组合：取最小值（更滑）
                physicsMaterial.bounceCombine = PhysicsMaterialCombine.Average;     // 弹性组合：平均值
            }
        }
        
        [EntitySystem]
        private static void Update(this CharacterControllerComponent self)
        {
            // 处理输入和跳跃请求（每帧处理，响应性更好）
            if (self.Input != null && self.Input.HasJumpRequest())
            {
                self.RequestJump();
            }
        }

        [EntitySystem]
        private static void OnAnimatorMove(this CharacterControllerComponent self)
        {
            float deltaTime = Time.deltaTime;
            
            if (self.Attack.IsMovementActive)
            {
                self.UpdateAttackMovement();
                self.CurrentVelocity = Vector3.up * self.CurrentVelocity.y;
                self.Rigidbody.linearVelocity = self.CurrentVelocity;
            }
            else if (!self.EnableMovement || self.Attack.IsInAttack)
            {
                self.ApplyDeceleration(deltaTime);
                self.Rigidbody.linearVelocity = self.CurrentVelocity;
            }
            else
            {
                self.ApplyMovement(deltaTime);
                self.Rigidbody.linearVelocity = self.CurrentVelocity;
            }

            // 应用旋转（使用可变时间步，响应输入）
            self.ApplyRotation(deltaTime);
            
            // 计算动画速度参数
            self.CalculateAnimationSpeeds();
            
            // 处理Root Motion
            if (!self.Attack.IsMovementActive && self.CurrentVelocity.magnitude <= 0.0001f && 
                self.Animator != null && self.Animator.deltaPosition.magnitude > 0.0001f)
            {
                self.Rigidbody.MovePosition(self.Rigidbody.position + self.Animator.deltaPosition);
            }
        }

        [EntitySystem]
        private static void FixedUpdate(this CharacterControllerComponent self)
        {
            float deltaTime = Time.fixedDeltaTime;
            
            self.Ground.Detect();
            
            if (self.JumpRequested)
            {
                self.Jump();
                self.JumpRequested = false;
            }

            self.ApplyGravity(deltaTime);
        }
        
        [EntitySystem]
        private static void Destroy(this CharacterControllerComponent self)
        {
            // 清理引用
            self.Rigidbody = null;
            self.CapsuleCollider = null;
        }


        /// <summary>
        /// 更新攻击位移
        /// </summary>
        private static void UpdateAttackMovement(this CharacterControllerComponent self)
        {
            if (self.Attack.State != AttackState.Attacking)
            {
                return;
            }

            if (self.Attack.CurrentSegment?.Movement == null || !self.Attack.CurrentSegment.Movement.EnableMovement)
            {
                return;
            }
            
            var movement = self.Attack.CurrentSegment.Movement;
            float normalizedTime = self.Attack.CurrentNormalizedTime;
            
            // 检查时间范围有效性
            if (movement.NormalizedEnd - movement.NormalizedStart <= 0)
            {
                return;
            }
            
            // 检查是否在位移时间范围内
            if (normalizedTime < movement.NormalizedStart)
            {
                return;
            }
            
            if (normalizedTime > movement.NormalizedEnd || movement.NormalizedEnd == 0)
            {
                return;
            }
            
            float moveProgress = (normalizedTime - movement.NormalizedStart) / (movement.NormalizedEnd - movement.NormalizedStart);
            moveProgress = Mathf.Clamp01(moveProgress);
            
            float curveValue = movement.MoveCurve.Evaluate(moveProgress);

            Vector3 targetPos = Vector3.Lerp(
                self.Attack.MovementStartPosition, 
                self.Attack.MovementTargetPosition, 
                curveValue
            );
            
            if (self.Rigidbody != null)
            {
                Vector3 currentPos = self.Rigidbody.position;
                // 保持Y轴不变（由FixedUpdate中的重力系统控制），只移动XZ
                Vector3 newPos = new Vector3(targetPos.x, currentPos.y, targetPos.z);
                self.Rigidbody.MovePosition(newPos);
            }
        }
        
        /// <summary>
        /// 应用移动（OnAnimatorMove中调用，使用Time.deltaTime）
        /// 只影响XZ轴速度，Y轴由FixedUpdate中的重力计算控制
        /// </summary>
        private static void ApplyMovement(this CharacterControllerComponent self, float deltaTime)
        {
            Vector3 inputDirection = self.Input.GetMoveDirection();
            
            // 在地面时才能应用移动（包括稳定斜坡、边缘等状态）
            if (inputDirection.magnitude > 0.01f)
            {
                // 计算目标速度（保持Y轴不变，只修改XZ）
                var targetVelocity = inputDirection * self.MoveSpeed + new Vector3(0, self.CurrentVelocity.y, 0);
                // 加速（使用可变时间步，响应输入）
                self.CurrentVelocity = Vector3.MoveTowards(
                    self.CurrentVelocity,
                    targetVelocity,
                    self.Acceleration * deltaTime
                );
            }
            else
            {
                // 减速（使用可变时间步，响应输入）
                self.CurrentVelocity = Vector3.MoveTowards(
                    self.CurrentVelocity,
                    new Vector3(0, self.CurrentVelocity.y, 0),
                    self.Deceleration * deltaTime
                );
            }
        }
        
        /// <summary>
        /// 应用旋转（OnAnimatorMove中调用，使用Time.deltaTime）
        /// 使用可变时间步，响应输入
        /// </summary>
        private static void ApplyRotation(this CharacterControllerComponent self, float deltaTime)
        {
            // 获取输入方向
            Vector3 inputDirection = self.Input.GetMoveDirection();
            
            // 只有当输入方向有效时才旋转，否则保持当前旋转
            if (inputDirection.magnitude < 0.01f)
            {
                return;
            }
            
            inputDirection = inputDirection.normalized;
            var player = self.Rigidbody.transform;
            // 计算目标旋转
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);

            // 根据是否在空中调整旋转速度
            float actualRotationSpeed = self.Ground.IsAirborne(self.Ground.State) ?
                self.RotationSpeed * 1.2f : // 空中旋转稍微快一点
                self.RotationSpeed;

            // 平滑旋转
            player.rotation = Quaternion.RotateTowards(
                player.rotation,
                targetRotation,
                actualRotationSpeed * deltaTime
            );
            // 同步到Unit的Rotation
            self.PlayerUnit.Rotation = player.rotation;
        }
        
        /// <summary>
        /// 应用减速度（当禁用移动时，OnAnimatorMove中调用，使用Time.deltaTime）
        /// 只影响XZ轴速度，Y轴由FixedUpdate中的重力计算控制
        /// </summary>
        private static void ApplyDeceleration(this CharacterControllerComponent self, float deltaTime)
        {
            // 减速（使用可变时间步，响应输入）
            self.CurrentVelocity = Vector3.MoveTowards(
                self.CurrentVelocity,
                new Vector3(0f, self.CurrentVelocity.y, 0f),
                self.Deceleration * deltaTime
            );
        }
        
        /// <summary>
        /// 立即停止移动（特殊情况使用，如被击飞、强制停止等）
        /// 同时更新CurrentVelocity和Rigidbody.linearVelocity，确保一致性
        /// </summary>
        public static void StopMovement(this CharacterControllerComponent self)
        {
            if (self.Input != null)
            {
                self.Input.MoveDirection = Vector3.zero;
            }
            // 只停止水平移动，保持垂直速度（重力/跳跃）
            Vector3 velocity = self.CurrentVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            self.CurrentVelocity = velocity;
            // 同步到Rigidbody（特殊情况需要立即生效）
            self.Rigidbody.linearVelocity = self.CurrentVelocity;
        }

        // ===== 跳跃相关方法 =====

        /// <summary>
        /// 请求跳跃（可以被外部调用，如输入系统）
        /// </summary>
        public static void RequestJump(this CharacterControllerComponent self)
        {
            self.JumpRequested = true;
        }

        /// <summary>
        /// 执行跳跃（保持当前的水平速度，实现移动跳跃距离更远）
        /// 水平速度越快，跳得越远；水平速度为0，原地起跳
        /// FixedUpdate中调用，只更新CurrentVelocity，不设置Rigidbody.linearVelocity
        /// </summary>
        private static void Jump(this CharacterControllerComponent self)
        {
            if (!self.Ground.CanJump())
            {
                return;
            }
            self.Ground.Jump();
            // 保持当前水平速度，只修改垂直速度
            Vector3 currentVelocity = self.CurrentVelocity;
            currentVelocity.y = self.JumpForce;
            self.CurrentVelocity = currentVelocity;
        }

        /// <summary>
        /// 应用重力和处理落地（只在空中时应用重力）
        /// FixedUpdate中调用，只更新CurrentVelocity，不设置Rigidbody.linearVelocity
        /// </summary>
        private static void ApplyGravity(this CharacterControllerComponent self, float deltaTime)
        {
            // 只在空中时应用重力
            if (self.Ground.IsAirborne(self.Ground.State))
            {
                Vector3 velocity = self.CurrentVelocity;
                float gravityAcceleration = self.Gravity * self.GravityMultiplier;
                velocity.y -= gravityAcceleration * deltaTime;
                self.CurrentVelocity = velocity;
            }
            else
            {
                // 落地时，将垂直速度归零
                Vector3 velocity = self.CurrentVelocity;
                if (velocity.y < 0f)
                {
                    velocity.y = 0f;
                    self.CurrentVelocity = velocity;
                }
            }
        }

        // ===== 动画速度计算方法 =====

        /// <summary>
        /// 计算动画速度参数（商业级实现）
        /// 根据配置返回标准化速度值，用于动画控制器
        /// </summary>
        private static void CalculateAnimationSpeeds(this CharacterControllerComponent self)
        {
            // 计算水平速度（去掉y分量）
            Vector3 horizontalVelocity = new Vector3(self.CurrentVelocity.x, 0f, self.CurrentVelocity.z);
            float horizontalSpeed = horizontalVelocity.magnitude;
            float normalizedSpeed = horizontalSpeed / self.MoveSpeed * 10f;
            self.NormalizedAnimationSpeed = normalizedSpeed;
            // 垂直速度（用于跳跃/下落动画）
            self.VerticalAnimationSpeed = self.CurrentVelocity.y;
        }

        /// <summary>
        /// 获取当前水平动画速度（范围由AnimationSpeedOutputScale决定，默认0-10）
        /// </summary>
        public static float GetNormalizedAnimationSpeed(this CharacterControllerComponent self)
        {
            return self.NormalizedAnimationSpeed;
        }

        /// <summary>
        /// 获取当前垂直动画速度（可正可负，用于跳跃动画）
        /// </summary>
        public static float GetVerticalAnimationSpeed(this CharacterControllerComponent self)
        {
            return self.VerticalAnimationSpeed;
        }
    }
}

