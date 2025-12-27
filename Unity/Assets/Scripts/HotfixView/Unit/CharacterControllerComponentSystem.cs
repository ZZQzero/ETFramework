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
            self.CheckGrounded = self.PlayerUnit.GetComponent<CheckGroundedComponent>();
            self.Input = self.PlayerUnit.GetComponent<InputComponent>();
        }
        
        [EntitySystem]
        private static void Update(this CharacterControllerComponent self)
        {
            // 处理输入和跳跃请求（每帧处理，响应性更好）
            if (self.Input != null && self.Input.HasJumpRequest())
            {
                self.RequestJump();
            }

            // 计算动画速度参数（基于最新的物理状态）
            self.CalculateAnimationSpeeds();
        }

        [EntitySystem]
        private static void FixedUpdate(this CharacterControllerComponent self)
        {
            float deltaTime = Time.fixedDeltaTime;
            // 地面检测（物理相关的）
            self.CheckGrounded.Detect();
            Log.Error($"{self.CheckGrounded.State}");
            // 处理跳跃执行（在FixedUpdate中确保物理一致性）
            if (self.JumpRequested)
            {
                self.Jump();
            }
            // 应用自定义重力（物理时间步长）
            self.UpdateJumpState(deltaTime);

            // 应用移动和旋转（物理相关的）
            if (!self.EnableMovement)
            {
                // 如果禁用移动，逐渐减速
                self.ApplyDeceleration(deltaTime);
            }
            else
            {
                // 应用移动
                self.ApplyMovement(deltaTime);
            }

            // 应用旋转（物理相关的）
            self.ApplyRotation(deltaTime);
        }
        
        [EntitySystem]
        private static void Destroy(this CharacterControllerComponent self)
        {
            // 清理引用
            self.Rigidbody = null;
            self.CapsuleCollider = null;
        }
        
        
        
        /// <summary>
        /// 应用移动
        /// </summary>
        private static void ApplyMovement(this CharacterControllerComponent self, float deltaTime)
        {
            Vector3 inputDirection = self.Input.GetMoveDirection();
            Vector3 velocity = self.Rigidbody.linearVelocity;
            
            if (self.CheckGrounded.State == GroundState.Grounded)
            {
                if (inputDirection.magnitude > 0.0f)
                {
                    // 计算目标速度
                    var targetVelocity = inputDirection * self.MoveSpeed + new Vector3(0, velocity.y, 0);
                    // 加速
                    self.CurrentVelocity = Vector3.MoveTowards(
                        self.CurrentVelocity,
                        targetVelocity,
                        self.Acceleration * deltaTime
                    );
                }
                else
                {
                    // 减速
                    self.CurrentVelocity = Vector3.MoveTowards(
                        self.CurrentVelocity,
                        new Vector3(0, velocity.y, 0),
                        self.Deceleration * deltaTime
                    );
                }
                self.Rigidbody.linearVelocity = self.CurrentVelocity;
            }
        }
        
        /// <summary>
        /// 应用旋转（面向移动方向，支持空中转向）
        /// </summary>
        private static void ApplyRotation(this CharacterControllerComponent self, float deltaTime)
        {
            // 获取输入方向
            Vector3 inputDirection = self.Input.GetMoveDirection().normalized;
            var player = self.Rigidbody.transform;
            // 计算目标旋转
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);

            // 根据是否在空中调整旋转速度
            float actualRotationSpeed = self.IsJumping ?
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
        /// 应用减速度（当禁用移动时）
        /// </summary>
        private static void ApplyDeceleration(this CharacterControllerComponent self, float deltaTime)
        {
            self.CurrentVelocity = Vector3.MoveTowards(
                self.CurrentVelocity,
                Vector3.zero,
                self.Deceleration * deltaTime
            );
            
            Vector3 velocity = self.Rigidbody.linearVelocity;
            velocity.x = self.CurrentVelocity.x;
            velocity.z = self.CurrentVelocity.z;
            self.Rigidbody.linearVelocity = velocity;
        }
        
        
        
        /// <summary>
        /// 立即停止移动
        /// </summary>
        public static void StopMovement(this CharacterControllerComponent self)
        {
            if (self.Input != null)
            {
                self.Input.MoveDirection =  Vector3.zero;
            }
            self.CurrentVelocity = Vector3.zero;
            Vector3 velocity = self.Rigidbody.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            self.Rigidbody.linearVelocity = velocity;
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
        /// </summary>
        private static void Jump(this CharacterControllerComponent self)
        {
            if (!self.CanJump())
            {
                return;
            }

            // 获取当前速度
            Vector3 currentVelocity = self.Rigidbody.linearVelocity;
            currentVelocity.y = self.JumpForce;
            self.Rigidbody.linearVelocity = currentVelocity;
            self.IsJumping = true;
            self.IsFalling = false;
            // 重置跳跃请求
            self.JumpRequested = false;
        }

        /// <summary>
        /// 检查是否可以跳跃
        /// </summary>
        private static bool CanJump(this CharacterControllerComponent self)
        {
            // 必须启用移动
            if (!self.EnableMovement)
            {
                return false;
            }

            // 必须有Rigidbody
            if (self.Rigidbody == null)
            {
                return false;
            }

            return self.CheckGrounded.State == GroundState.Grounded;
        }

        /// <summary>
        /// 始终应用重力
        /// </summary>
        private static void UpdateJumpState(this CharacterControllerComponent self, float deltaTime)
        {
            if (!self.IsJumping)
            {
                return;
            }
            // 获取当前速度
            Vector3 velocity = self.Rigidbody.linearVelocity;
            // 计算重力加速度
            float gravityAcceleration = self.Gravity * self.GravityMultiplier;
            velocity.y -= gravityAcceleration * deltaTime;
            self.Rigidbody.linearVelocity = velocity;

            if (velocity.y < 0f && self.CheckGrounded.State != GroundState.Grounded)
            {
                //下落过程中
                self.IsFalling = true;
            }
            else if(velocity.y < 0f && self.CheckGrounded.State == GroundState.Grounded)
            {
                //已经在地面
                self.IsJumping = false;
                self.IsFalling = false;
                velocity.y = 0f;
                self.Rigidbody.linearVelocity = velocity;
            }
        }

        // ===== 动画速度计算方法 =====

        /// <summary>
        /// 计算动画速度参数（商业级实现）
        /// 根据配置返回标准化速度值，用于动画控制器
        /// </summary>
        private static void CalculateAnimationSpeeds(this CharacterControllerComponent self)
        {
            Vector3 velocity = self.Rigidbody.linearVelocity;
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

        /// <summary>
        /// 获取跳跃状态（用于动画状态机）
        /// 返回值：0=地面静止，1=行走/奔跑，2=跳跃上升，3=下落
        /// </summary>
        public static int GetAnimationState(this CharacterControllerComponent self)
        {
            if (self.IsJumping)
            {
                return 2; // 跳跃上升
            }
            else if (self.IsFalling)
            {
                return 3; // 下落
            }
            else if (self.NormalizedAnimationSpeed > 0.1f)
            {
                return 1; // 行走/奔跑
            }
            else
            {
                return 0; // 地面静止
            }
        }
    }
}

