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
            self.Unit = self.GetParent<Unit>();
            self.Ground = self.Unit.GetComponent<CheckGroundedComponent>();
            self.LocomotionIntent = self.Unit.GetComponent<LocomotionIntentComponent>();
            self.Attack = self.Unit.GetComponent<AttackComponent>();
            self.HitReaction = self.Unit.GetComponent<HitReactionComponent>();
            self.HitStop = self.Unit.GetComponent<HitStopComponent>();
            self.AirCombo = self.Unit.GetComponent<AirComboComponent>();

            // 运动配置：只消费最终参数，不关心来源（玩家/怪物/BUFF/数值）
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
            // 处理跳跃请求（意图层边沿触发，响应性更好）
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
            if (self.Attack == null)
            {
                self.Attack = self.Unit?.GetComponent<AttackComponent>();
            }
            if (self.HitReaction == null)
            {
                self.HitReaction = self.Unit?.GetComponent<HitReactionComponent>();
            }
            if (self.HitStop == null)
            {
                self.HitStop = self.Unit?.GetComponent<HitStopComponent>();
            }
            if (self.AirCombo == null)
            {
                self.AirCombo = self.Unit?.GetComponent<AirComboComponent>();
            }

            // HitStop（顿帧）期间：冻结运动与 RootMotion，避免停顿时“偷偷滑动/掉落”。
            // - 保留 CurrentVelocity（用于顿帧结束后恢复）
            // - 强制 Rigidbody 当前速度为 0，确保物理步不继续积分
            if (self.HitStop != null && self.HitStop.IsHitStopActive)
            {
                if (self.Rigidbody != null)
                {
                    switch (self.HitStop.FreezeMode)
                    {
                        case HitStopFreezeMode.None:
                        case HitStopFreezeMode.FreezeAnimationOnly:
                            // 不冻结运动：继续走后续逻辑（移动/攻击/RootMotion等），统一在函数末尾计算与同步
                            break;
                        case HitStopFreezeMode.FreezeXZOnly:
                            // 冻结水平：保留 Y（重力/上抛继续），XZ 置 0
                            self.Rigidbody.linearVelocity = new Vector3(0f, self.CurrentVelocity.y, 0f);
                            self.CalculateAnimationSpeeds();
                            self.SyncUnitTransformFromRigidbody();
                            return;
                        default:
                            // FreezeAll：完全冻结
                            self.Rigidbody.linearVelocity = Vector3.zero;
                            self.CalculateAnimationSpeeds();
                            self.SyncUnitTransformFromRigidbody();
                            return;
                    }
                }
            }

            // 1. 基础运动合成 (Locomotion/Attack/Deceleration)
            bool isMoveAllowed = self.LocomotionIntent != null && self.LocomotionIntent.IsMoveAllowed;

            if (self.Attack != null && self.Attack.IsMovementActive)
            {
                self.UpdateAttackMovement();
                // 攻击位移期间，水平速度由 UpdateAttackMovement 控制，逻辑速度层只需保留垂直速度
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

            // 2. 叠加外部水平目标速度 (如受击曲线位移)
            if (self.LocomotionIntent != null && self.LocomotionIntent.ExternalTargetVelocity.sqrMagnitude > 0.0001f)
            {
                Vector2 targetVel = self.LocomotionIntent.ExternalTargetVelocity;
                
                // 受击滑行：无论当前是否允许移动，都强制覆盖 XZ 速度（保证位移精度）
                self.CurrentVelocity = new Vector3(targetVel.x, self.CurrentVelocity.y, targetVel.y);
            }

            // 3. 消费外部 3D 瞬时冲量 (如爆炸、击飞、砸地等)
            // 放在目标速度之后处理，确保冲量能叠加在受击位移之上，而不会被覆盖
            if (self.LocomotionIntent != null && self.LocomotionIntent.ExternalImpulse.sqrMagnitude > 0.0001f)
            {
                Vector3 impulse = self.LocomotionIntent.ExternalImpulse;
                Vector3 v = self.CurrentVelocity;
                
                if (impulse.y > 0.001f) // 向上力：取最大值以支持浮空叠加，XZ 叠加
                {
                    v.x += impulse.x;
                    v.z += impulse.z;
                    // Y轴权限：空中连段 Active 时，不再通过“不断上抛”维持空中（由 ComboPhysics 接管）
                    if (self.AirCombo != null && self.AirCombo.Active)
                    {
                        // 允许进入空中后仍保持向上速度（例如刚起跳/刚被击飞的上升段），但不再被后续命中抬高
                        // 因此这里不提升 v.y，只保留原本的 v.y
                    }
                    else
                    {
                        v.y = Mathf.Max(v.y, impulse.y);
                    }
                    
                    // 强制脱离地面：优先保留受击系统设置的特殊原因（Launched/Juggled/Knockdown）
                    // 如果当前没有原因或是 WalkOff（掉落），则统一视为被击飞 (Launched)
                    if (self.Ground != null)
                    {
                        var currentReason = self.Ground.AirborneReason;
                        if (currentReason == AirborneReason.None || currentReason == AirborneReason.WalkOff)
                        {
                            currentReason = AirborneReason.Launched;
                        }
                        self.Ground.ForceBreakGround(currentReason);
                    }
                }
                else // 纯水平或向下力（砸地）
                {
                    v += impulse;
                }
                
                self.CurrentVelocity = v;
                self.LocomotionIntent.ExternalImpulse = Vector3.zero; // 消费即焚
            }

            // 3. 应用最终速度到物理引擎
            if (self.Rigidbody != null)
            {
                self.Rigidbody.linearVelocity = self.CurrentVelocity;
            }
            
            // 4. 应用朝向旋转
            bool canRotate = self.LocomotionIntent == null || self.LocomotionIntent.IsRotateAllowed;
            if (canRotate && (self.Attack == null || !self.Attack.IsAttacking))
            {
                self.ApplyRotation(deltaTime);
            }
            
            // 5. 计算动画速度参数
            self.CalculateAnimationSpeeds();
            
            // 6. 处理 Root Motion（手动应用 Animator.deltaPosition）
            // 关键：某些攻击/技能段依赖 Root Motion 推进位置，否则会出现“播完被拉回原位”。
            bool allowRootMotionInAttack =
                self.Attack != null &&
                self.Attack.IsInAttack &&
                !self.Attack.IsMovementActive &&
                self.Attack.CurrentSegment?.Movement != null &&
                self.Attack.CurrentSegment.Movement.UseRootMotion;

            // Root Motion 门槛只看水平速度：
            // - 允许空中（有 Y 速度）时仍能应用动画位移，避免“空中突进/空中斩”被误挡导致回弹。
            float horizontalSpeedSqr = self.CurrentVelocity.x * self.CurrentVelocity.x + self.CurrentVelocity.z * self.CurrentVelocity.z;

            // RootMotion 与 HitStop 的明确策略：
            // - FreezeAnimationOnly/FreezeAll：动画图暂停，禁止消费 RootMotion，避免恢复时瞬移/滑步
            bool blockRootMotion = self.HitStop != null && self.HitStop.IsHitStopActive && self.HitStop.FreezeMode != HitStopFreezeMode.None;

            if (!blockRootMotion &&
                (self.Attack == null || (!self.Attack.IsMovementActive && (!self.Attack.IsInAttack || allowRootMotionInAttack))) &&
                horizontalSpeedSqr <= 0.0001f * 0.0001f &&
                self.Animator != null && self.Animator.deltaPosition.magnitude > 0.0001f)
            {
                self.Rigidbody.MovePosition(self.Rigidbody.position + self.Animator.deltaPosition);
            }

            self.SyncUnitTransformFromRigidbody();
        }

        [EntitySystem]
        private static void FixedUpdate(this CharacterControllerComponent self)
        {
            float deltaTime = Time.fixedDeltaTime;

            if (self.AirCombo == null)
            {
                self.AirCombo = self.Unit?.GetComponent<AirComboComponent>();
            }

            // HitStop.FreezeAll：冻结窗口内不更新 Ground（Prev/State/timers），避免假落地/状态抖动
            if (self.HitStop != null && self.HitStop.IsHitStopActive && self.HitStop.FreezeMode == HitStopFreezeMode.FreezeAll)
            {
                if (self.Rigidbody != null)
                {
                    self.Rigidbody.linearVelocity = Vector3.zero;
                }
                self.SyncUnitTransformFromRigidbody();
                return;
            }

            self.Ground.Detect();
            
            if (self.Attack == null)
            {
                self.Attack = self.Unit?.GetComponent<AttackComponent>();
            }

            if (self.JumpRequested && (self.Attack == null || !self.Attack.IsInAttack))
            {
                // 检查能力权限：是否允许跳跃
                bool canJump = self.LocomotionIntent == null || self.LocomotionIntent.IsJumpAllowed;
                if (canJump)
                {
                    self.Jump();
                }
                self.JumpRequested = false;
            }

            // HitStop（顿帧）期间：不推进重力/不改变速度，确保顿帧期间角色不下坠/不位移。
            if (self.HitStop != null && self.HitStop.IsHitStopActive)
            {
                if (self.Rigidbody != null)
                {
                    switch (self.HitStop.FreezeMode)
                    {
                        case HitStopFreezeMode.None:
                        case HitStopFreezeMode.FreezeAnimationOnly:
                            // 不冻结运动：继续走重力推进
                            break;
                        case HitStopFreezeMode.FreezeXZOnly:
                            // 冻结水平：确保 XZ 为 0，但允许 Y 继续由重力系统推进
                            self.Rigidbody.linearVelocity = new Vector3(0f, self.Rigidbody.linearVelocity.y, 0f);
                            break;
                        default:
                            // FreezeAll：完全冻结（不推进重力）
                            self.Rigidbody.linearVelocity = Vector3.zero;
                            self.SyncUnitTransformFromRigidbody();
                            return;
                    }
                }
            }

            // ComboPhysics：空中连段接管垂直规则（重力/下落速度/高度夹持）
            if (self.AirCombo != null && self.AirCombo.Active)
            {
                // fail-safe：若已回到地面但组件仍 Active，连续数帧后强制结束，避免“永远悬空”
                if (self.Ground != null)
                {
                    self.AirCombo.FailSafeTick(self.Ground.IsGrounded(self.Ground.State));
                }

                // 延迟捕获 EnteredHeight：确保 ForceBreakGround 生效后再记录，避免地面漂移影响
                if (self.Ground != null && self.Ground.IsAirborne(self.Ground.State))
                {
                    self.AirCombo.CaptureEnteredHeightIfNeeded(self.Rigidbody != null ? self.Rigidbody.position.y : self.Unit.Position.y);
                }

                long nowCombatMs = self.HitStop != null ? self.HitStop.NowCombatMs() : TimeInfo.Instance.ClientFrameTime();
                float gScale = self.AirCombo.GetCurrentGravityScale(nowCombatMs);
                gScale = Mathf.Clamp01(gScale);

                // 应用缩放重力（Y 轴）
                if (self.CurrentVelocity.y > -1000f) // 防御性
                {
                    float g = self.Gravity * self.GravityMultiplier * gScale;
                    self.CurrentVelocity += Vector3.down * g * deltaTime;
                }

                // 下落速度下限（不允许无限下落）
                float minFall = self.AirCombo.MinFallSpeed; // 负数
                if (self.CurrentVelocity.y < minFall)
                {
                    self.CurrentVelocity = new Vector3(self.CurrentVelocity.x, minFall, self.CurrentVelocity.z);
                }

                // 高度夹持（使用 MovePosition，避免穿模/爆震）
                if (self.Rigidbody != null)
                {
                    Vector3 pos = self.Rigidbody.position;
                    float clampedY = Mathf.Clamp(pos.y, self.AirCombo.ComboMinHeight, self.AirCombo.ComboMaxHeight);
                    if (!Mathf.Approximately(pos.y, clampedY))
                    {
                        // 触顶：不允许继续向上
                        if (pos.y > clampedY && self.CurrentVelocity.y > 0f)
                        {
                            self.CurrentVelocity = new Vector3(self.CurrentVelocity.x, 0f, self.CurrentVelocity.z);
                        }
                        pos.y = clampedY;
                        self.Rigidbody.MovePosition(pos);
                    }
                }

                // 退出完成：不再接管（交给自然重力）
                if (self.AirCombo.IsExitCompleted(nowCombatMs))
                {
                    self.AirCombo.ForceEnd();
                }
            }
            else
            {
                self.ApplyGravity(deltaTime);
            }

            // Ground/重力只在 FixedUpdate 推进，这里也同步一次，避免只动 Y 时 Unit 不更新
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
        /// 将 Rigidbody 的最终结果同步回 Unit（作为逻辑/事件系统的权威位置）。
        /// 注意：避免每帧无意义 Publish，通过阈值过滤抖动。
        /// </summary>
        private static void SyncUnitTransformFromRigidbody(this CharacterControllerComponent self)
        {
            if (self.Unit == null || self.Rigidbody == null)
            {
                return;
            }

            Vector3 pos = self.Rigidbody.position;
            Quaternion rot = self.Rigidbody.rotation;

            // Position
            var unitPos3 = self.Unit.Position;
            var unitPos = new Vector3(unitPos3.x, unitPos3.y, unitPos3.z);
            if ((unitPos - pos).sqrMagnitude > 0.000001f)
            {
                self.Unit.Position = pos;
            }

            // Rotation（只同步水平旋转）
            Vector3 fwd = rot * Vector3.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
            {
                fwd.Normalize();
                var unitFwd3 = self.Unit.Forward;
                var unitFwd = new Vector3(unitFwd3.x, unitFwd3.y, unitFwd3.z);
                if ((unitFwd - fwd).sqrMagnitude > 0.0001f)
                {
                    self.Unit.Forward = fwd;
                }
            }
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
            Vector3 inputDirection = self.LocomotionIntent != null ? self.LocomotionIntent.MoveDirection : Vector3.zero;
            
            // 斜坡投影：在地面（含稳定斜坡/边缘）时，把移动方向投影到地面法线所在平面，避免“上坡顶不动/下坡漂”。
            if (self.Ground != null && self.Ground.IsGrounded(self.Ground.State))
            {
                inputDirection = self.Ground.GetSlopeDirection(inputDirection);
            }
            
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
            // 获取期望朝向：优先 FaceDirection，回退 MoveDirection
            Vector3 inputDirection = Vector3.zero;
            if (self.LocomotionIntent != null)
            {
                inputDirection = self.LocomotionIntent.FaceDirection;
                if (inputDirection.sqrMagnitude < 0.0001f)
                {
                    inputDirection = self.LocomotionIntent.MoveDirection;
                }
            }
            
            // 只有当输入方向有效时才旋转，否则保持当前旋转
            if (inputDirection.magnitude < 0.01f)
            {
                return;
            }
            
            inputDirection = inputDirection.normalized;
            var player = self.Rigidbody.transform;
            // 计算目标旋转
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
                // 根据是否在空中调整旋转速度
                actualRotationSpeed = self.Ground.IsAirborne(self.Ground.State) ?
                    self.RotationSpeed * 1.2f : // 空中旋转稍微快一点
                    self.RotationSpeed;
            }
            // 平滑旋转
            player.rotation = Quaternion.RotateTowards(
                player.rotation,
                targetRotation,
                actualRotationSpeed * deltaTime
            );
            // Unit 同步由 SyncUnitTransformFromRigidbody 统一负责
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
            if (self.LocomotionIntent != null)
            {
                self.LocomotionIntent.MoveDirection = Vector3.zero;
                self.LocomotionIntent.FaceDirection = Vector3.zero;
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

