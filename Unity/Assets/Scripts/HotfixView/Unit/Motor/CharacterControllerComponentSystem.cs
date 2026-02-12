using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ET
{
    public static partial class CharacterControllerComponentSystem
    {

        [EntitySystem]
        private static void Awake(this CharacterControllerComponent self, GameObject player)
        {
            self.PlayerTransform = player.transform;
            self.Unit = self.GetParent<Unit>();
            self.InitComponentRefs(self.Unit);

            // 读取移动配置
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

            // CapsuleCollider：作为 Sweep 的形状参考，不参与物理碰撞响应
            self.CapsuleCollider = player.GetComponent<CapsuleCollider>();
            if (self.CapsuleCollider == null)
            {
                Log.Warning($"CharacterControllerComponent 缺少 CapsuleCollider，GameObject: {player.name}");
                self.CapsuleRadius = 0.3f;
                self.CapsuleHeight = 1.8f;
            }
            else
            {
                self.CapsuleRadius = self.CapsuleCollider.radius;
                self.CapsuleHeight = self.CapsuleCollider.height;
            }

            // 移除 Rigidbody（如果存在）：完全不依赖物理引擎
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Object.Destroy(rb);
            }

            // 碰撞掩码：使用 Ground 检测的配置，或默认排除自身
            var ground = self.Ground;
            if (ground != null && ground.Config != null)
            {
                self.CollisionMask = ground.Config.GroundMask | ground.Config.PlatformMask;
            }
            else
            {
                self.CollisionMask = ~LayerMask.GetMask("Player", "Ignore Raycast");
            }
        }

        /// <summary>
        /// OnAnimatorMove：纯数据采集，仅累积 Root Motion delta
        /// </summary>
        [EntitySystem]
        private static void OnAnimatorMove(this CharacterControllerComponent self)
        {
            if (self.Animator != null)
            {
                if (self.LastRootMotionFrame != Time.frameCount)
                {
                    self.LastRootMotionFrame = Time.frameCount;
                    self.RootMotionDelta = self.Animator.deltaPosition; // 本帧第一次：赋值
                }
                else
                {
                    self.RootMotionDelta += self.Animator.deltaPosition; // 本帧多次：累加
                    Log.Error("同一帧 OnAnimatorMove 被调用多次，可能存在嵌套 Animator 或多个 Animator 组件。");
                }
            }
        }

        /// <summary>
        /// Update：运动管线（Pipeline）
        /// 感知 → 决策 → 约束 → 执行 → 同步，每阶段职责单一
        /// </summary>
        [EntitySystem]
        private static void Update(this CharacterControllerComponent self)
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 完全冻结：跳过所有运动计算
            if (self.IsFullyFrozen())
            {
                self.RootMotionDelta = Vector3.zero;
                self.SyncOutput();
                return;
            }

            bool freezeXZ = self.IsXZFrozen();

            // ── 感知 ──
            self.Sense();

            // ── 决策 ──
            self.ResolveJump();
            self.ApplyGravity(dt);
            Vector3 displacement = self.ComputeDisplacement(dt, freezeXZ);

            // ── 约束 ──
            displacement = self.ApplyConstraints(displacement);

            // ── 执行 ──
            self.SweepMove(displacement);
            self.ResolveRotation(dt, freezeXZ);

            // ── 同步 ──
            self.SyncOutput();
        }

        [EntitySystem]
        private static void Destroy(this CharacterControllerComponent self)
        {
            self.CapsuleCollider = null;
            self.PlayerTransform = null;
        }

        // ==================== 管线阶段 ====================

        private static bool IsFullyFrozen(this CharacterControllerComponent self)
        {
            return self.HitStop != null
                   && self.HitStop.IsHitStopActive
                   && self.HitStop.FreezeMode == HitStopFreezeMode.FreezeAll;
        }

        private static bool IsXZFrozen(this CharacterControllerComponent self)
        {
            return self.HitStop != null
                   && self.HitStop.IsHitStopActive
                   && self.HitStop.FreezeMode == HitStopFreezeMode.FreezeXZOnly;
        }

        /// <summary>
        /// 感知：采集地面状态
        /// </summary>
        private static void Sense(this CharacterControllerComponent self)
        {
            if (self.Ground == null) return;
            self.Ground.CurrentVerticalSpeed = self.CurrentVelocity.y;
            self.Ground.Detect();
        }

        /// <summary>
        /// 决策：处理跳跃请求
        /// Attacking 阶段不可跳，Recovery 阶段跳跃可取消后摇
        /// </summary>
        private static void ResolveJump(this CharacterControllerComponent self)
        {
            if (self.LocomotionIntent != null
                && self.LocomotionIntent.ConsumeJumpRequest()
                && (self.Attack == null || !self.Attack.IsAttacking))
            {
                self.JumpRequested = true;
            }

            if (!self.JumpRequested) return;

            bool canJump = (self.Attack == null || !self.Attack.IsAttacking)
                           && (self.LocomotionIntent == null || self.LocomotionIntent.IsJumpAllowed);
            if (canJump)
            {
                // 后摇中跳跃 → 取消后摇
                if (self.Attack != null && self.Attack.State == AttackState.Recovery)
                {
                    self.Attack.ExitAttackState();
                }
                self.Jump();
            }
            self.JumpRequested = false;
        }

        /// <summary>
        /// 决策：合成本帧位移（攻击曲线位移 / 速度+冲量+RootMotion+Drift）
        /// </summary>
        private static Vector3 ComputeDisplacement(this CharacterControllerComponent self, float dt, bool freezeXZ)
        {
            Vector3 currentPos = self.PlayerTransform.position;

            // 攻击曲线位移（突刺/前冲等大幅度配置位移）
            bool isAttackMovement = !freezeXZ && self.Attack != null && self.Attack.IsMovementActive;
            if (isAttackMovement)
            {
                Vector3 attackXZ = self.ComputeAttackTargetXZ(currentPos);
                float yDelta = self.CurrentVelocity.y * dt;
                self.CurrentVelocity = new Vector3(0f, self.CurrentVelocity.y, 0f);
                return new Vector3(attackXZ.x - currentPos.x, yDelta, attackXZ.z - currentPos.z);
            }

            // 常规位移：速度 + 冲量 + RootMotion
            if (!freezeXZ)
            {
                self.ComputeHorizontalVelocity(dt);
            }
            else
            {
                self.CurrentVelocity = new Vector3(0f, self.CurrentVelocity.y, 0f);
            }

            self.ApplyImpulse();
            Vector3 rootMotion = self.ConsumeRootMotion(dt);
            Vector3 displacement = self.CurrentVelocity * dt + rootMotion;

            // Drift：Attacking 阶段无曲线位移时，叠加方向键微位移
            if (!freezeXZ)
            {
                displacement += self.ComputeDrift(dt);
            }

            return displacement;
        }

        /// <summary>
        /// 约束：对位移施加限制（空连高度夹持等）
        /// </summary>
        private static Vector3 ApplyConstraints(this CharacterControllerComponent self, Vector3 displacement)
        {
            if (self.AirCombo == null || self.AirCombo.IsExiting || !self.AirCombo.HeightClampInitialized)
                return displacement;

            float currentY = self.PlayerTransform.position.y;
            float targetY = currentY + displacement.y;
            float clampedY = Mathf.Clamp(targetY, self.AirCombo.ComboMinHeight, self.AirCombo.ComboMaxHeight);

            if (targetY > clampedY)
            {
                self.CurrentVelocity = new Vector3(self.CurrentVelocity.x, 0f, self.CurrentVelocity.z);
            }

            displacement.y = clampedY - currentY;
            return displacement;
        }

        /// <summary>
        /// 执行：旋转
        /// Attacking 阶段不可转，Recovery 阶段允许旋转
        /// </summary>
        private static void ResolveRotation(this CharacterControllerComponent self, float dt, bool freezeXZ)
        {
            if (freezeXZ) return;

            bool canRotate = self.LocomotionIntent == null || self.LocomotionIntent.IsRotateAllowed;
            if (canRotate && (self.Attack == null || !self.Attack.IsAttacking))
            {
                self.ApplyRotation(dt);
            }
        }

        /// <summary>
        /// 同步：位置/旋转写回 Unit + 更新动画参数
        /// </summary>
        private static void SyncOutput(this CharacterControllerComponent self)
        {
            if (self.Unit != null && self.PlayerTransform != null)
            {
                self.Unit.Position = self.PlayerTransform.position;
                self.Unit.Rotation = self.PlayerTransform.rotation;
            }
            self.CalculateAnimationSpeeds();
        }

        // ==================== Sweep & Slide 碰撞解算 ====================

        /// <summary>
        /// CapsuleCast 检测碰撞，碰到障碍物沿表面滑动
        /// </summary>
        private static void SweepMove(this CharacterControllerComponent self, Vector3 displacement)
        {
            if (self.PlayerTransform == null) return;
            if (displacement.sqrMagnitude < 0.000001f) return;
            float skinWidth = self.SkinWidth;
            Vector3 remaining = displacement;

            for (int i = 0; i < self.MaxSweepIterations; i++)
            {
                float moveDist = remaining.magnitude;
                if (moveDist < 0.0001f) break;

                Vector3 moveDir = remaining / moveDist;

                if (PhysicsHelper.CapsuleCastClosest(
                        self.PlayerTransform.position,
                        self.CapsuleRadius,
                        self.CapsuleHeight,
                        moveDir,
                        moveDist + skinWidth,
                        self.CollisionMask,
                        self.SweepHitBuffer,
                        self.PlayerTransform,
                        self.CapsuleCollider,
                        self.SkinWidth,
                        out RaycastHit hit))
                {
                    float safeDist = Mathf.Max(0f, hit.distance - skinWidth);
                    self.PlayerTransform.position += moveDir * safeDist;

                    remaining -= moveDir * safeDist;
                    remaining = Vector3.ProjectOnPlane(remaining, hit.normal);
                }
                else
                {
                    self.PlayerTransform.position += remaining;
                    break;
                }
            }
        }

        // ==================== 速度计算 ====================

        private static void ComputeHorizontalVelocity(this CharacterControllerComponent self, float dt)
        {
            bool isMoveAllowed = self.LocomotionIntent != null && self.LocomotionIntent.IsMoveAllowed;
            var attack = self.Attack;

            if (!isMoveAllowed)
            {
                self.ApplyDeceleration(dt);
            }
            else if (attack != null && attack.IsAttacking)
            {
                // Attacking 阶段：减速（Drift 微位移在 ComputeDisplacement 中单独处理）
                self.ApplyDeceleration(dt);
            }
            else if (attack != null && attack.State == AttackState.Recovery)
            {
                // Recovery 阶段：有移动输入 → 取消后摇，恢复正常移动
                Vector3 moveDir = self.LocomotionIntent?.MoveDirection ?? Vector3.zero;
                if (moveDir.sqrMagnitude > 0.01f)
                {
                    attack.ExitAttackState();
                    self.ApplyMovement(dt);
                }
                else
                {
                    self.ApplyDeceleration(dt);
                }
            }
            else
            {
                self.ApplyMovement(dt);
            }

            // 外部目标速度覆盖
            if (self.LocomotionIntent == null) return;

            Vector2 targetVel = self.LocomotionIntent.ExternalTargetVelocity;
            if (targetVel.sqrMagnitude > 0.0001f)
            {
                self.CurrentVelocity = new Vector3(targetVel.x, self.CurrentVelocity.y, targetVel.y);
                self.WasDrivenByExternalVelocity = true;
            }
            else if (self.WasDrivenByExternalVelocity)
            {
                self.WasDrivenByExternalVelocity = false;
                self.CurrentVelocity = new Vector3(0f, self.CurrentVelocity.y, 0f);
            }
        }

        /// <summary>
        /// 攻击中微位移（Drift）：按住方向键时叠加小幅度位移，用于追敌/微调站位。
        /// 仅在当前攻击段配置 AllowDrift=true 时生效。
        /// </summary>
        private static Vector3 ComputeDrift(this CharacterControllerComponent self, float dt)
        {
            var attack = self.Attack;
            if (attack == null || !attack.IsAttacking) return Vector3.zero;

            var movement = attack.CurrentSegment?.Movement;
            if (movement == null || !movement.AllowDrift) return Vector3.zero;

            Vector3 moveDir = self.LocomotionIntent != null ? self.LocomotionIntent.MoveDirection : Vector3.zero;
            if (moveDir.sqrMagnitude <= 0.01f) return Vector3.zero;

            float driftSpeed = self.MoveSpeed * movement.DriftSpeedRatio;
            Vector3 drift = moveDir.normalized * (driftSpeed * dt);
            return drift;
        }

        private static void ApplyImpulse(this CharacterControllerComponent self)
        {
            if (self.LocomotionIntent == null) return;

            Vector3 impulse = self.LocomotionIntent.ExternalImpulse;
            if (impulse.sqrMagnitude <= 0.0001f) return;

            self.CurrentVelocity += impulse;
            self.LocomotionIntent.ExternalImpulse = Vector3.zero;
        }

        private static Vector3 ConsumeRootMotion(this CharacterControllerComponent self, float dt)
        {
            Vector3 delta = self.RootMotionDelta;
            self.RootMotionDelta = Vector3.zero;

            // HitStop 期间阻断 Root Motion
            if (self.HitStop != null && self.HitStop.IsHitStopActive
                && self.HitStop.FreezeMode != HitStopFreezeMode.None)
            {
                return Vector3.zero;
            }

            // 攻击中无外部位移时允许 Root Motion
            if (self.Attack != null && self.Attack.IsInAttack && !self.Attack.IsMovementActive)
            {
                float hSqr = self.CurrentVelocity.x * self.CurrentVelocity.x
                            + self.CurrentVelocity.z * self.CurrentVelocity.z;
                if (hSqr < 0.001f)
                {
                    return delta;
                }
            }

            // 受击期（怪物）：忽略受击动画 RootMotion，避免“动画自带位移”导致异常后退距离。
            // 位移应由 HitReaction 的物理轨道（ExternalTargetVelocity/Impulse）控制。
            if (self.HitReaction != null && self.HitReaction.IsInHitReaction)
            {
                bool isPlayer = self.Unit != null && self.Unit.UnitType() == UnitType.Player;
                if (!isPlayer)
                {
                    bool hasExternal = self.LocomotionIntent != null
                                       && self.LocomotionIntent.ExternalTargetVelocity.sqrMagnitude > 0.0001f;
                    if (!hasExternal)
                    {
                        return delta;
                    }
                }
            }

            return Vector3.zero;
        }

        // ==================== 攻击位移 ====================

        private static Vector3 ComputeAttackTargetXZ(this CharacterControllerComponent self, Vector3 currentPos)
        {
            if (self.Attack.State != AttackState.Attacking) return currentPos;
            if (self.Attack.CurrentSegment?.Movement == null || !self.Attack.CurrentSegment.Movement.EnableMovement) return currentPos;

            var movement = self.Attack.CurrentSegment.Movement;
            float normalizedTime = self.Attack.CurrentNormalizedTime;

            if (movement.NormalizedEnd - movement.NormalizedStart <= 0) return currentPos;
            if (normalizedTime < movement.NormalizedStart) return currentPos;
            if (normalizedTime > movement.NormalizedEnd || movement.NormalizedEnd == 0) return currentPos;

            // 追踪目标时动态更新目标位置
            if (movement.TrackTarget && self.Attack.LockedTarget != null)
            {
                Vector3 dir = self.Attack.LockedTarget.position - self.Attack.MovementStartPosition;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.Normalize();
                    float optimalDist = self.Attack.Config?.OptimalCombatDistance ?? 0.8f;
                    float distToTarget = Vector3.Distance(
                        new Vector3(currentPos.x, 0f, currentPos.z),
                        new Vector3(self.Attack.LockedTarget.position.x, 0f, self.Attack.LockedTarget.position.z));
                    float moveDist = Mathf.Min(movement.Distance, Mathf.Max(0f, distToTarget - optimalDist));
                    self.Attack.MovementTargetPosition = self.Attack.MovementStartPosition + dir * moveDist;
                }
            }

            float moveProgress = Mathf.Clamp01(
                (normalizedTime - movement.NormalizedStart) / (movement.NormalizedEnd - movement.NormalizedStart));
            float curveValue = movement.MoveCurve.Evaluate(moveProgress);

            return Vector3.Lerp(
                self.Attack.MovementStartPosition,
                self.Attack.MovementTargetPosition,
                curveValue);
        }

        // ==================== 移动/减速 ====================

        private static void ApplyMovement(this CharacterControllerComponent self, float dt)
        {
            Vector3 inputDirection = self.LocomotionIntent != null ? self.LocomotionIntent.MoveDirection : Vector3.zero;

            if (self.Ground != null && self.Ground.IsGrounded(self.Ground.StateContext.State))
            {
                inputDirection = self.Ground.GetSlopeDirection(inputDirection);
            }

            if (inputDirection.magnitude > 0.01f)
            {
                var targetVelocity = inputDirection * self.MoveSpeed + new Vector3(0, self.CurrentVelocity.y, 0);
                self.CurrentVelocity = Vector3.MoveTowards(self.CurrentVelocity, targetVelocity, self.Acceleration * dt);
            }
            else
            {
                self.ApplyDeceleration(dt);
            }
        }

        private static void ApplyDeceleration(this CharacterControllerComponent self, float dt)
        {
            self.CurrentVelocity = Vector3.MoveTowards(
                self.CurrentVelocity,
                new Vector3(0f, self.CurrentVelocity.y, 0f),
                self.Deceleration * dt);
        }

        public static void StopMovement(this CharacterControllerComponent self)
        {
            if (self.LocomotionIntent != null)
            {
                self.LocomotionIntent.MoveDirection = Vector3.zero;
                self.LocomotionIntent.FaceDirection = Vector3.zero;
            }
            self.CurrentVelocity = new Vector3(0f, self.CurrentVelocity.y, 0f);
        }

        // ==================== 旋转 ====================

        private static void ApplyRotation(this CharacterControllerComponent self, float dt)
        {
            Vector3 inputDirection = Vector3.zero;
            if (self.LocomotionIntent != null)
            {
                inputDirection = self.LocomotionIntent.FaceDirection;
                if (inputDirection.sqrMagnitude < 0.0001f)
                {
                    inputDirection = self.LocomotionIntent.MoveDirection;
                }
            }
            if (inputDirection.magnitude < 0.01f) return;

            inputDirection = inputDirection.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);

            float actualRotationSpeed;
            if (self.Attack != null && self.Attack.State == AttackState.Recovery)
            {
                float angle = Quaternion.Angle(self.PlayerTransform.rotation, targetRotation);
                float boost = Mathf.Lerp(2.5f, 4.5f, Mathf.Clamp01(angle / 180f));
                actualRotationSpeed = self.RotationSpeed * boost;
            }
            else
            {
                actualRotationSpeed = self.Ground != null && self.Ground.IsAirborne(self.Ground.StateContext.State)
                    ? self.RotationSpeed * 1.2f
                    : self.RotationSpeed;
            }

            self.PlayerTransform.rotation = Quaternion.RotateTowards(
                self.PlayerTransform.rotation,
                targetRotation,
                actualRotationSpeed * dt);
        }

        // ==================== 跳跃/重力 ====================

        public static void RequestJump(this CharacterControllerComponent self)
        {
            self.JumpRequested = true;
        }

        private static void Jump(this CharacterControllerComponent self)
        {
            if (self.Ground == null || !self.Ground.CanJump()) return;

            self.Ground.Jump();
            self.CurrentVelocity = new Vector3(self.CurrentVelocity.x, self.JumpForce, self.CurrentVelocity.z);
        }

        private static void ApplyGravity(this CharacterControllerComponent self, float dt)
        {
            if (self.Ground == null) return;

            if (self.Ground.IsAirborne(self.Ground.StateContext.State))
            {
                float gravityAcceleration = self.Gravity * self.GravityMultiplier;
                self.CurrentVelocity -= new Vector3(0f, gravityAcceleration * dt, 0f);
            }
            else if (self.CurrentVelocity.y < 0f)
            {
                self.CurrentVelocity = new Vector3(self.CurrentVelocity.x, 0f, self.CurrentVelocity.z);
            }
        }

        // ==================== 动画参数 ====================

        private static void CalculateAnimationSpeeds(this CharacterControllerComponent self)
        {
            float hx = self.CurrentVelocity.x;
            float hz = self.CurrentVelocity.z;
            float horizontalSpeed = Mathf.Sqrt(hx * hx + hz * hz);

            if (self.HitReaction != null && self.HitReaction.IsInHitReaction)
            {
                self.NormalizedAnimationSpeed = horizontalSpeed / self.MoveSpeed * self.HitReaction.FirstUpForce;
            }
            else
            {
                self.NormalizedAnimationSpeed = horizontalSpeed / self.MoveSpeed * 10f;
            }
            self.VerticalAnimationSpeed = self.CurrentVelocity.y;
        }

        public static float GetNormalizedAnimationSpeed(this CharacterControllerComponent self)
        {
            return self.NormalizedAnimationSpeed;
        }

        public static float GetVerticalAnimationSpeed(this CharacterControllerComponent self)
        {
            return self.VerticalAnimationSpeed;
        }
    }
}
