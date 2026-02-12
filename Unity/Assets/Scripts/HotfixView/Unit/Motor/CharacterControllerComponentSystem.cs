using UnityEngine;
using Unity.Mathematics;

namespace ET
{
    public static partial class CharacterControllerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CharacterControllerComponent self, GameObject player)
        {
            // 获取或添加 Rigidbody
            self.Rigidbody = player.GetComponent<Rigidbody>();
            if (self.Rigidbody == null)
            {
                self.Rigidbody = player.AddComponent<Rigidbody>();
            }

            // Rigidbody
            self.Rigidbody.isKinematic = false;
            self.Rigidbody.useGravity = false; // 使用自定义重力
            self.Rigidbody.freezeRotation = true; // 锁定旋转避免倾倒
            self.Rigidbody.angularDamping = 5f;
            self.Rigidbody.linearDamping = 0f; // 线性阻尼保持 0，避免自动减速
            self.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // 插值平滑
            self.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // 连续碰撞检测
            // 清空速度避免残留
            self.Rigidbody.linearVelocity = Vector3.zero;
            self.Rigidbody.angularVelocity = Vector3.zero;
            self.Unit = self.GetParent<Unit>();
            self.InitComponentRefs(self.Unit);

            // 读取移动配置（装备/BUFF/配置表）
            var moveConfig = self.Unit.GetComponent<MovementConfigComponent>();
            if (moveConfig != null)
            {
                self.MoveSpeed = moveConfig.MoveSpeed;
                self.Acceleration = moveConfig.Acceleration;
                self.Deceleration = moveConfig.Deceleration;
                self.RotationSpeed = moveConfig.RotationSpeed;
                self.Gravity = moveConfig.Gravity;
                self.JumpForce = moveConfig.JumpForce;
                self.GravityMultiplier = moveConfig.GravityMultiplier;
            }

            self.CapsuleCollider = player.GetComponent<CapsuleCollider>();
            if (self.CapsuleCollider == null)
            {
                Log.Warning($"CharacterControllerComponent 缺少 CapsuleCollider，GameObject: {player.name}");
            }
            else
            {
                // 初始化物理材质，减少粘地/卡滞
                // Collider 可能已挂材质，这里仅做兜底
                PhysicsMaterial physicsMaterial = self.CapsuleCollider.material;
                if (physicsMaterial == null)
                {
                    physicsMaterial = new PhysicsMaterial("PlayerPhysicsMaterial");
                    self.CapsuleCollider.material = physicsMaterial;
                }
                
                physicsMaterial.dynamicFriction = 0.1f;
                physicsMaterial.staticFriction = 0.2f;
                physicsMaterial.bounciness = 0f;
                physicsMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
                physicsMaterial.bounceCombine = PhysicsMaterialCombine.Average;
            }
        }


        [EntitySystem]
        private static void Update(this CharacterControllerComponent self)
        {
            // 消费跳跃请求
            if (self.LocomotionIntent != null &&
                self.LocomotionIntent.ConsumeJumpRequest() &&
                (self.Attack == null || !self.Attack.IsInAttack))
            {
                self.RequestJump();
            }
        }

        [EntitySystem]
        private static void OnAnimatorMove(this CharacterControllerComponent self)
        {
            float deltaTime = Time.deltaTime;
            // 使用缓存引用，减少 GetComponent

            // HitStop 冻结处理（影响 RootMotion）
            if (self.HitStop != null && self.HitStop.IsHitStopActive)
            {
                if (self.Rigidbody != null)
                {
                    switch (self.HitStop.FreezeMode)
                    {
                        case HitStopFreezeMode.None:
                        case HitStopFreezeMode.FreezeAnimationOnly:
                            // 仅冻结动画，不冻结物理
                            break;
                        case HitStopFreezeMode.FreezeXZOnly:
                            // 冻结 XZ
                            self.Rigidbody.linearVelocity = new Vector3(0f, self.CurrentVelocity.y, 0f);
                            self.CalculateAnimationSpeeds();
                            self.SyncUnitTransformFromRigidbody();
                            return;
                        default:
                            // 完全冻结
                            self.Rigidbody.linearVelocity = Vector3.zero;
                            self.CalculateAnimationSpeeds();
                            self.SyncUnitTransformFromRigidbody();
                            return;
                    }
                }
            }

            // 1. 计算水平速度（移动/攻击/减速）
            bool isMoveAllowed = self.LocomotionIntent != null && self.LocomotionIntent.IsMoveAllowed;

            if (self.Attack != null && self.Attack.IsMovementActive)
            {
                self.UpdateAttackMovement();
                // 攻击位移后清空 XZ 速度
                self.CurrentVelocity = Vector3.up * self.CurrentVelocity.y;
            }
            else if (!isMoveAllowed || (self.Attack != null && self.Attack.IsInAttack))
            {
                self.ApplyDeceleration(deltaTime);
            }
            else
            {
                self.ApplyMovement(deltaTime);
            }

            // 2. 外部目标速度覆盖
            if (self.LocomotionIntent != null)
            {
                Vector2 targetVel = self.LocomotionIntent.ExternalTargetVelocity;
                if (targetVel.sqrMagnitude > 0.0001f)
                {
                    // 外部驱动中：直接覆盖 XZ 速度
                    self.CurrentVelocity = new Vector3(targetVel.x, self.CurrentVelocity.y, targetVel.y);
                    self.WasDrivenByExternalVelocity = true;
                }
                else if (self.WasDrivenByExternalVelocity)
                {
                    // 外部驱动刚结束：立即清零 XZ，避免残留速度导致减速滑行
                    self.WasDrivenByExternalVelocity = false;
                    self.CurrentVelocity = new Vector3(0f, self.CurrentVelocity.y, 0f);
                }
            }

            // 3. 同步速度到 Rigidbody
            if (self.Rigidbody != null)
            {
                self.Rigidbody.linearVelocity = self.CurrentVelocity;
            }
            
            // 4. 旋转
            bool canRotate = self.LocomotionIntent == null || self.LocomotionIntent.IsRotateAllowed;
            if (canRotate && !self.Attack.IsInAttack)
            {
                self.ApplyRotation(deltaTime);
            }
            
            // 5. 计算动画速度
            self.CalculateAnimationSpeeds();
            
            // 6. Root Motion 处理
            bool allowRootMotionInAttack =
                self.Attack != null &&
                self.Attack.IsInAttack &&
                !self.Attack.IsMovementActive;

            // 当前水平速度平方
            float horizontalSpeedSqr = self.CurrentVelocity.x * self.CurrentVelocity.x + self.CurrentVelocity.z * self.CurrentVelocity.z;

            // HitStop 期间阻断 RootMotion
            // - FreezeAnimationOnly/FreezeAll 会阻断 RootMotion
            bool blockRootMotion = self.HitStop != null && self.HitStop.IsHitStopActive && self.HitStop.FreezeMode != HitStopFreezeMode.None;

            if (!blockRootMotion && allowRootMotionInAttack && horizontalSpeedSqr < 0.001f)
            {
                Vector3 delta = self.Animator.deltaPosition;
                self.Rigidbody.MovePosition(self.Rigidbody.transform.position + delta);
            }

            self.SyncUnitTransformFromRigidbody();
        }

        [EntitySystem]
        private static void FixedUpdate(this CharacterControllerComponent self)
        {
            float deltaTime = Time.fixedDeltaTime;

            // HitStop 冻结处理（FixedUpdate）
            if (self.HitStop != null && self.HitStop.IsHitStopActive)
            {
                if (self.Rigidbody != null)
                {
                    switch (self.HitStop.FreezeMode)
                    {
                        case HitStopFreezeMode.None:
                        case HitStopFreezeMode.FreezeAnimationOnly:
                            // 仅冻结动画，不冻结物理
                            break;
                        case HitStopFreezeMode.FreezeXZOnly:
                            // 冻结 XZ，保留 Y
                            self.Rigidbody.linearVelocity = new Vector3(0f, self.CurrentVelocity.y, 0f);
                            break;
                        case HitStopFreezeMode.FreezeAll:
                            if (self.Rigidbody != null)
                            {
                                // 完全冻结时清零速度
                                self.Rigidbody.linearVelocity = Vector3.zero;
                            }
                            self.SyncUnitTransformFromRigidbody();
                            return;
                    }
                }
            }

            // 处理外部冲量（3D）
            // - 只在 FixedUpdate 应用
            // - Ground.Detect 依赖 Y 速度判断落地
            if (self.LocomotionIntent != null && self.LocomotionIntent.ExternalImpulse.sqrMagnitude > 0.0001f)
            {
                Vector3 impulse = self.LocomotionIntent.ExternalImpulse;
                Vector3 velocity = new Vector3(self.CurrentVelocity.x, 0, self.CurrentVelocity.z) + impulse;
                self.CurrentVelocity = velocity;
                // Ground.Detect 依赖 Rigidbody 的 Y 速度
                if (self.Rigidbody != null)
                {
                    self.Rigidbody.linearVelocity = self.CurrentVelocity;
                }

                self.LocomotionIntent.ExternalImpulse = Vector3.zero; // 清空一次性冲量
            }

            self.Ground.Detect();

            if (self.JumpRequested && (self.Attack == null || !self.Attack.IsInAttack))
            {
                // 处理跳跃
                bool canJump = self.LocomotionIntent == null || self.LocomotionIntent.IsJumpAllowed;
                if (canJump)
                {
                    self.Jump();
                }
                self.JumpRequested = false;
            }

            // 空连物理
            /*if (self.AirCombo != null && self.AirCombo.Active && !self.AirCombo.IsExiting)
            {
                long nowCombatMs = self.HitStop != null ? self.HitStop.NowCombatMs() : TimeInfo.Instance.ClientFrameTime();
                float gScale = self.AirCombo.GetCurrentGravityScale(nowCombatMs);
                gScale = Mathf.Clamp01(gScale);
                
                // 应用空连重力
                if (self.CurrentVelocity.y > -1000f) 
                {
                    float g = self.Gravity * self.GravityMultiplier * gScale;
                    self.CurrentVelocity += Vector3.down * g * deltaTime;
                }

                // 下落速度下限
                float minFall = self.AirCombo.MinFallSpeed; // 最小下落速度
                if (self.CurrentVelocity.y < minFall)
                {
                    self.CurrentVelocity = new Vector3(self.CurrentVelocity.x, minFall, self.CurrentVelocity.z);
                }

                
            }*/
          
            self.ApplyGravity(deltaTime);

            // 高度夹持：仅在维持期生效，退出期重力已恢复，应允许自然下落
            if (self.Rigidbody != null && !self.AirCombo.IsExiting && self.AirCombo.HeightClampInitialized)
            {
                Vector3 pos = self.Rigidbody.position;
                float clampedY = Mathf.Clamp(pos.y, self.AirCombo.ComboMinHeight, self.AirCombo.ComboMaxHeight);
                if (!Mathf.Approximately(pos.y, clampedY))
                {
                    // 上冲到顶时清零 Y
                    if (pos.y > clampedY)
                    {
                        self.CurrentVelocity = new Vector3(self.CurrentVelocity.x, 0f, self.CurrentVelocity.z);
                        self.Rigidbody.linearVelocity = self.CurrentVelocity;
                    }
                    pos.y = clampedY;
                    self.Rigidbody.MovePosition(pos);
                }
                
                /*float minFall = self.AirCombo.MinFallSpeed; // 最小下落速度
                if (self.CurrentVelocity.y < -5)
                {
                    self.CurrentVelocity = new Vector3(self.CurrentVelocity.x, -5, self.CurrentVelocity.z);
                }*/
            }
            
            // 空中高度安全网：防止残留上升速度导致超过天花板
            /*if (self.HitReaction != null 
                && self.HitReaction.MaxAirborneHeight > self.HitReaction.AirborneOriginHeight
                && self.Rigidbody != null)
            {
                float maxY = self.HitReaction.MaxAirborneHeight;
                float currentY = self.Rigidbody.position.y;
                if (currentY > maxY)
                {
                    Vector3 pos = self.Rigidbody.position;
                    pos.y = maxY;
                    self.Rigidbody.MovePosition(pos);
                    // 清零向上速度，保留水平速度
                    if (self.CurrentVelocity.y > 0f)
                    {
                        self.CurrentVelocity = new Vector3(self.CurrentVelocity.x, 0f, self.CurrentVelocity.z);
                    }
                }
            }*/
            
            if (self.Rigidbody != null)
            {
                if (self.HitStop != null && self.HitStop.IsHitStopActive && self.HitStop.FreezeMode == HitStopFreezeMode.FreezeXZOnly)
                {
                    self.Rigidbody.linearVelocity = new Vector3(0f, self.CurrentVelocity.y, 0f);
                }
                else
                {
                    self.Rigidbody.linearVelocity = self.CurrentVelocity;
                }
            }

            // 同步 Unit 位置与朝向
            self.SyncUnitTransformFromRigidbody();
        }
        
        [EntitySystem]
        private static void Destroy(this CharacterControllerComponent self)
        {
            // 清理引用
            self.Rigidbody = null;
            self.CapsuleCollider = null;
        }

        /// <summary>
        /// 将 Rigidbody 的位置/朝向同步到 Unit
        /// 仅在变化时写回，避免多余同步
        ///  </summary>
        private static void SyncUnitTransformFromRigidbody(this CharacterControllerComponent self)
        {
            if (self.Unit == null || self.Rigidbody == null)
            {
                return;
            }
            self.Unit.Position = self.Rigidbody.transform.position;
            self.Unit.Rotation = self.Rigidbody.transform.rotation;
        }


        /// <summary>
        /// 攻击位移（仅 XZ）
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
            
            // 时间窗检查
            if (movement.NormalizedEnd - movement.NormalizedStart <= 0)
            {
                return;
            }
            
            // 未到窗口
            if (normalizedTime < movement.NormalizedStart)
            {
                return;
            }
            
            if (normalizedTime > movement.NormalizedEnd || movement.NormalizedEnd == 0)
            {
                return;
            }
            
            // 动态更新目标位置：如果有锁定目标且启用了追踪，每帧跟随目标
            if (movement.TrackTarget && self.Attack.LockedTarget != null)
            {
                Vector3 dir = self.Attack.LockedTarget.position - self.Attack.MovementStartPosition;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.Normalize();
                    float optimalDist = self.Attack.Config?.OptimalCombatDistance ?? 0.8f;
                    float distToTarget = Vector3.Distance(
                        new Vector3(self.Rigidbody.position.x, 0f, self.Rigidbody.position.z),
                        new Vector3(self.Attack.LockedTarget.position.x, 0f, self.Attack.LockedTarget.position.z));
                    float moveDist = Mathf.Min(movement.Distance, Mathf.Max(0f, distToTarget - optimalDist));
                    self.Attack.MovementTargetPosition = self.Attack.MovementStartPosition + dir * moveDist;
                }
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
                // 只改 XZ，Y 由 FixedUpdate 处理
                Vector3 newPos = new Vector3(targetPos.x, currentPos.y, targetPos.z);
                self.Rigidbody.MovePosition(newPos);
            }
        }
        
        /// <summary>
        /// 基于输入移动（OnAnimatorMove，XZ）
        /// Y 由 FixedUpdate 处理
        /// </summary>
        private static void ApplyMovement(this CharacterControllerComponent self, float deltaTime)
        {
            Vector3 inputDirection = self.LocomotionIntent != null ? self.LocomotionIntent.MoveDirection : Vector3.zero;
            
            // 贴地修正方向
            if (self.Ground != null && self.Ground.IsGrounded(self.Ground.StateContext.State))
            {
                inputDirection = self.Ground.GetSlopeDirection(inputDirection);
            }
            
            // 有输入时加速
            if (inputDirection.magnitude > 0.01f)
            {
                // 目标速度
                var targetVelocity = inputDirection * self.MoveSpeed + new Vector3(0, self.CurrentVelocity.y, 0);
                // 加速
                self.CurrentVelocity = Vector3.MoveTowards(
                    self.CurrentVelocity,
                    targetVelocity,
                    self.Acceleration * deltaTime
                );
            }
            else
            {
                // 无输入时减速
                self.CurrentVelocity = Vector3.MoveTowards(
                    self.CurrentVelocity,
                    new Vector3(0, self.CurrentVelocity.y, 0),
                    self.Deceleration * deltaTime
                );
            }
        }
        
        /// <summary>
        /// 处理转向（OnAnimatorMove）
        /// </summary>
        private static void ApplyRotation(this CharacterControllerComponent self, float deltaTime)
        {
            // 取朝向输入
            Vector3 inputDirection = Vector3.zero;
            if (self.LocomotionIntent != null)
            {
                inputDirection = self.LocomotionIntent.FaceDirection;
                if (inputDirection.sqrMagnitude < 0.0001f)
                {
                    inputDirection = self.LocomotionIntent.MoveDirection;
                }
            }
            
            // 无方向则返回
            if (inputDirection.magnitude < 0.01f)
            {
                return;
            }
            
            inputDirection = inputDirection.normalized;
            var player = self.Rigidbody.transform;
            // 目标朝向
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);

            float actualRotationSpeed;
            if (self.Attack != null && self.Attack.State == AttackState.Recovery)
            {
                float angle = Quaternion.Angle(player.rotation, targetRotation);
                float boost = Mathf.Lerp(2.5f, 4.5f, Mathf.Clamp01(angle / 180f));
                actualRotationSpeed = self.RotationSpeed * boost;
            }
            else
            {
                // 空中转向稍快
                actualRotationSpeed = self.Ground.IsAirborne(self.Ground.StateContext.State) ?
                    self.RotationSpeed * 1.2f : 
                    self.RotationSpeed;
            }
            // 旋转插值
            player.rotation = Quaternion.RotateTowards(
                player.rotation,
                targetRotation,
                actualRotationSpeed * deltaTime
            );
        }

        /// <summary>
        /// 将 Root Motion deltaPosition 按与锁定目标的距离进行钳位（XZ 平面，地面/空中通用）。
        /// - 前进分量：不超过 (distance - optimalDist)
        /// - 后退/侧移分量：保留
        /// </summary>
        private static Vector3 ClampRootMotionToTarget(
            Vector3 delta,
            Vector3 selfPos,
            Vector3 targetPos,
            float optimalDist)
        {
            Vector3 toTarget = targetPos - selfPos;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (dist < 0.01f) return Vector3.zero; // 重叠保护

            Vector3 dir = toTarget / dist;
            float forwardDelta = Vector3.Dot(delta, dir);

            if (forwardDelta <= 0f) return delta; // 后退方向不钳位

            float remaining = dist - optimalDist;
            if (remaining <= 0f)
            {
                // 已在最佳距离内：完全去除前进分量
                return delta - dir * forwardDelta;
            }

            // 钳位前进分量到剩余距离
            float clamped = Mathf.Min(forwardDelta, remaining);
            return delta + dir * (clamped - forwardDelta);
        }
        
        /// <summary>
        /// 水平减速（OnAnimatorMove，XZ）
        /// </summary>
        private static void ApplyDeceleration(this CharacterControllerComponent self, float deltaTime)
        {
            // 水平减速
            self.CurrentVelocity = Vector3.MoveTowards(
                self.CurrentVelocity,
                new Vector3(0f, self.CurrentVelocity.y, 0f),
                self.Deceleration * deltaTime
            );
        }
        
        /// <summary>
        /// 立即停止移动并同步速度
        /// 同步 CurrentVelocity 与 Rigidbody
        /// </summary>
        public static void StopMovement(this CharacterControllerComponent self)
        {
            if (self.LocomotionIntent != null)
            {
                self.LocomotionIntent.MoveDirection = Vector3.zero;
                self.LocomotionIntent.FaceDirection = Vector3.zero;
            }
            // 清空 XZ
            Vector3 velocity = self.CurrentVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            self.CurrentVelocity = velocity;
            // 写回 Rigidbody
            self.Rigidbody.linearVelocity = self.CurrentVelocity;
        }

        // ===== 跳跃 =====

        /// <summary>
        /// 请求跳跃
        /// </summary>
        public static void RequestJump(this CharacterControllerComponent self)
        {
            self.JumpRequested = true;
        }

        /// <summary>
        /// 执行跳跃（设置 Y 速度）
        /// FixedUpdate 负责同步到 Rigidbody
        /// </summary>
        private static void Jump(this CharacterControllerComponent self)
        {
            if (!self.Ground.CanJump())
            {
                return;
            }
            self.Ground.Jump();
            // 设置向上速度
            Vector3 currentVelocity = self.CurrentVelocity;
            currentVelocity.y = self.JumpForce;
            self.CurrentVelocity = currentVelocity;
        }

        /// <summary>
        /// 应用重力
        /// FixedUpdate 负责同步到 Rigidbody
        /// </summary>
        private static void ApplyGravity(this CharacterControllerComponent self, float deltaTime)
        {
            // 空中施加重力
            if (self.Ground.IsAirborne(self.Ground.StateContext.State))
            {
                Vector3 velocity = self.CurrentVelocity;
                float gravityAcceleration = self.Gravity * self.GravityMultiplier;
                velocity.y -= gravityAcceleration * deltaTime;
                self.CurrentVelocity = velocity;
            }
            else
            {
                // 地面时清零下落速度
                Vector3 velocity = self.CurrentVelocity;
                if (velocity.y < 0f)
                {
                    velocity.y = 0f;
                    self.CurrentVelocity = velocity;
                }
            }
        }

        // ===== 动画参数 =====

        /// <summary>
        /// 计算动画速度参数
        /// 由当前速度驱动动画
        /// </summary>
        private static void CalculateAnimationSpeeds(this CharacterControllerComponent self)
        {
            // 水平速度
            Vector3 horizontalVelocity = new Vector3(self.CurrentVelocity.x, 0f, self.CurrentVelocity.z);
            float horizontalSpeed = horizontalVelocity.magnitude;
            float normalizedSpeed = horizontalSpeed / self.MoveSpeed * 10f;
            self.NormalizedAnimationSpeed = normalizedSpeed;
            // 垂直速度
            self.VerticalAnimationSpeed = self.CurrentVelocity.y;
        }

        /// <summary>
        /// 获取水平动画速度
        /// </summary>
        public static float GetNormalizedAnimationSpeed(this CharacterControllerComponent self)
        {
            return self.NormalizedAnimationSpeed;
        }

        /// <summary>
        /// 获取垂直动画速度
        /// </summary>
        public static float GetVerticalAnimationSpeed(this CharacterControllerComponent self)
        {
            return self.VerticalAnimationSpeed;
        }
    }
}

