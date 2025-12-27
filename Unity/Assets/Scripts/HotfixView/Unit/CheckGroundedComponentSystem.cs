using UnityEngine;

namespace ET
{
    public static partial class CheckGroundedComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CheckGroundedComponent self, GameObject go)
        {
            self.Transform = go.transform;
            self.Capsule = go.GetComponent<CapsuleCollider>();
            self.Rigidbody = go.GetComponent<Rigidbody>();

            // 配置初始化
            self.Config ??= new GroundDetectorConfig();
            self.Config.Initialize();
            self.Config.GroundMask = LayerMask.GetMask("Map");

            if (self.Capsule != null)
            {
                self.CapsuleRadius = self.Capsule.radius;
                self.CapsuleHeight = self.Capsule.height;
            }
            else
            {
                Log.Error("[CheckGroundedComponent] CapsuleCollider not found!");
                self.CapsuleRadius = 0.5f;
                self.CapsuleHeight = 2f;
            }

            self.State = GroundState.Airborne;
            self.PrevState = GroundState.Airborne;
            self.GroundHit.Reset();
        }

        [EntitySystem]
        private static void Destroy(this CheckGroundedComponent self)
        {
            self.ClearEvents();
            
            // 恢复忽略的碰撞
            if (self.IgnoredPlatform != null && self.Capsule != null)
            {
                Physics.IgnoreCollision(self.Capsule, self.IgnoredPlatform, false);
            }
        }

        /// <summary>
        /// 主检测入口 - FixedUpdate
        /// </summary>
        public static void Detect(this CheckGroundedComponent self)
        {
            if (self.Capsule == null) return;

            var config = self.Config;
            self.FrameCounter++;

            // 更新忽略平台状态
            self.UpdateIgnoredPlatform();

            // 性能优化：空中降频
            if (config.ReduceAirborneCheckFrequency &&
                IsAirborne(self.State) &&
                self.FrameCounter % config.AirborneCheckInterval != 0)
            {
                self.UpdateTimers(Time.fixedDeltaTime);
                return;
            }

            self.PrevState = self.State;
            self.PerformGroundCheck();
            self.UpdateGroundState();
            self.UpdateTimers(Time.fixedDeltaTime);
            self.HandleStateTransition();
        }

        private static void PerformGroundCheck(this CheckGroundedComponent self)
        {
            self.GroundHit.Reset();

            var config = self.Config;
            Vector3 position = self.Transform.position;

            float checkRadius = self.CapsuleRadius * config.RadiusScale;
            float skinWidth = config.SkinWidth;

            // 自适应检测距离
            float checkDistance = IsGrounded(self.State)
                ? config.GroundCheckDistance
                : config.AirborneCheckDistance;

            // 防穿透
            if (self.Rigidbody != null)
            {
                float fallSpeed = Mathf.Max(0, -self.Rigidbody.linearVelocity.y);
                checkDistance = Mathf.Max(checkDistance, fallSpeed * Time.fixedDeltaTime * 2f);
            }

            Vector3 sphereCenter = new Vector3(
                position.x,
                position.y + self.CapsuleRadius + skinWidth,
                position.z
            );

            // 合并 GroundMask 和 PlatformMask，但排除当前忽略的平台
            LayerMask combinedMask = config.GroundMask | config.PlatformMask;

            // 阶段1: SphereCast
            if (self.SphereCastGround(sphereCenter, checkRadius, checkDistance + skinWidth, combinedMask, out RaycastHit bestHit))
            {
                self.GroundHit.HasGround = true;
                self.GroundHit.Point = bestHit.point;
                self.GroundHit.Normal = bestHit.normal;
                self.GroundHit.Distance = Mathf.Max(0, bestHit.distance - skinWidth);
                self.GroundHit.SlopeAngle = Vector3.Angle(bestHit.normal, Vector3.up);
                self.GroundHit.Collider = bestHit.collider;
                self.GroundHit.GroundTransform = bestHit.transform;
                self.GroundHit.GroundLayer = bestHit.collider.gameObject.layer;
                self.GroundHit.IsStable = bestHit.normal.y >= config.CosMaxSlopeAngle;
                
                // 获取地面类型
                var surface = bestHit.collider.GetComponent<IGroundSurface>();
                self.GroundHit.SurfaceType = surface?.SurfaceType ?? GroundSurfaceType.Default;
            }

            // 阶段2: Raycast 精修
            if (self.GroundHit.HasGround || IsGrounded(self.State))
            {
                self.RaycastRefine(position, checkDistance + skinWidth, combinedMask);
            }

            // 阶段3: 边缘检测（降频）
            if (config.EnableEdgeDetection && self.GroundHit.HasGround)
            {
                self.EdgeCheckCounter++;
                if (self.EdgeCheckCounter >= config.EdgeCheckInterval ||
                    self.State == GroundState.OnEdge)
                {
                    self.EdgeCheckCounter = 0;
                    self.DetectEdge(position, checkRadius, checkDistance, combinedMask);
                }
            }

            // 记录移动平台数据
            if (self.GroundHit.HasGround && self.GroundHit.GroundTransform != null)
            {
                self.GroundHit.LocalPositionOnGround = 
                    self.GroundHit.GroundTransform.InverseTransformPoint(position);
                self.GroundHit.PrevGroundPosition = self.GroundHit.GroundTransform.position;
                self.GroundHit.PrevGroundRotation = self.GroundHit.GroundTransform.rotation;
            }
        }

        private static bool SphereCastGround(
            this CheckGroundedComponent self,
            Vector3 origin,
            float radius,
            float distance,
            LayerMask mask,
            out RaycastHit bestHit)
        {
            bestHit = default;
            var config = self.Config;

            int hitCount = Physics.SphereCastNonAlloc(
                origin, radius, Vector3.down,
                self.SphereCastBuffer, distance,
                mask, QueryTriggerInteraction.Ignore
            );

            if (hitCount == 0) return false;

            float bestScore = float.MinValue;
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                ref RaycastHit hit = ref self.SphereCastBuffer[i];

                // 忽略自身
                if (hit.collider.transform.IsChildOf(self.Transform)) continue;
                
                // 忽略下跳穿透的平台
                if (hit.collider == self.IgnoredPlatform) continue;

                // 忽略背面
                if (hit.normal.y <= 0.01f) continue;

                // 评分
                float normalScore = hit.normal.y;  // 用 y 分量代替 Angle 计算
                float distanceScore = 1f - (hit.distance / distance);
                float score = distanceScore * 0.6f + normalScore * 0.4f;

                // 可站立地面加分
                if (hit.normal.y >= config.CosMaxSlopeAngle)
                {
                    score += 10f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestHit = hit;
                    found = true;
                }
            }

            return found;
        }

        private static void RaycastRefine(
            this CheckGroundedComponent self, 
            Vector3 position, 
            float distance,
            LayerMask mask)
        {
            var config = self.Config;
            float offset = self.CapsuleRadius * 0.5f;
            Vector3 forward = self.Transform.forward;
            Vector3 right = self.Transform.right;
            float rayStartY = position.y + config.SkinWidth * 2;

            // 使用预分配数组
            var origins = self.RayOriginBuffer;
            origins[0] = new Vector3(position.x, rayStartY, position.z);
            origins[1] = new Vector3(position.x + forward.x * offset, rayStartY, position.z + forward.z * offset);
            origins[2] = new Vector3(position.x - forward.x * offset, rayStartY, position.z - forward.z * offset);
            origins[3] = new Vector3(position.x + right.x * offset, rayStartY, position.z + right.z * offset);
            origins[4] = new Vector3(position.x - right.x * offset, rayStartY, position.z - right.z * offset);

            Vector3 normalSum = Vector3.zero;
            int validCount = 0;
            float closestDistance = float.MaxValue;
            RaycastHit closestHit = default;

            for (int i = 0; i < 5; i++)
            {
                if (Physics.Raycast(origins[i], Vector3.down, out RaycastHit hit,
                    distance + 0.1f, mask, QueryTriggerInteraction.Ignore))
                {
                    // 忽略下跳平台
                    if (hit.collider == self.IgnoredPlatform) continue;
                    
                    if (hit.normal.y >= config.CosMaxSlopeAngle)
                    {
                        normalSum += hit.normal;
                        validCount++;

                        if (hit.distance < closestDistance)
                        {
                            closestDistance = hit.distance;
                            closestHit = hit;
                        }
                    }
                }
            }

            if (validCount > 0)
            {
                self.GroundHit.Normal = (normalSum / validCount).normalized;
                self.GroundHit.SlopeAngle = Vector3.Angle(self.GroundHit.Normal, Vector3.up);

                float adjustedDist = closestDistance - config.SkinWidth * 2;
                if (adjustedDist < self.GroundHit.Distance)
                {
                    self.GroundHit.Distance = Mathf.Max(0, adjustedDist);
                    self.GroundHit.Point = closestHit.point;
                }

                self.GroundHit.HasGround = true;
                self.GroundHit.IsStable = self.GroundHit.Normal.y >= config.CosMaxSlopeAngle;
            }
        }

        private static void DetectEdge(
            this CheckGroundedComponent self,
            Vector3 position,
            float radius,
            float distance,
            LayerMask mask)
        {
            var config = self.Config;
            int rayCount = config.EdgeRayCount;
            float checkRadius = radius * 0.8f;
            float rayStartY = position.y + config.SkinWidth * 2;

            int supportedRays = 0;
            Vector3 unsupportedDirection = Vector3.zero;
            float angleStep = 360f / rayCount * Mathf.Deg2Rad;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = angleStep * i;
                float offsetX = Mathf.Cos(angle) * checkRadius;
                float offsetZ = Mathf.Sin(angle) * checkRadius;

                Vector3 rayOrigin = new Vector3(
                    position.x + offsetX,
                    rayStartY,
                    position.z + offsetZ
                );

                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    distance + 0.1f, mask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != self.IgnoredPlatform &&
                        hit.normal.y >= config.CosMaxSlopeAngle)
                    {
                        supportedRays++;
                    }
                    else
                    {
                        unsupportedDirection.x += offsetX;
                        unsupportedDirection.z += offsetZ;
                    }
                }
                else
                {
                    unsupportedDirection.x += offsetX;
                    unsupportedDirection.z += offsetZ;
                }
            }

            self.GroundHit.SupportRatio = (float)supportedRays / rayCount;
            self.GroundHit.IsOnEdge = self.GroundHit.SupportRatio < config.EdgeSupportThreshold;

            if (self.GroundHit.IsOnEdge && 
                (unsupportedDirection.x * unsupportedDirection.x + 
                 unsupportedDirection.z * unsupportedDirection.z) > 0.01f)
            {
                self.GroundHit.EdgeDirection = unsupportedDirection.normalized;
            }
        }

        private static void UpdateGroundState(this CheckGroundedComponent self)
        {
            var config = self.Config;
            ref var hit = ref self.GroundHit;

            GroundState newState;

            if (!hit.HasGround || hit.Distance > config.GroundCheckDistance)
            {
                // 空中
                newState = (self.Rigidbody != null && self.Rigidbody.linearVelocity.y < -0.5f)
                    ? GroundState.Falling
                    : GroundState.Airborne;
            }
            else if (!hit.IsStable)
            {
                // 滑落斜坡
                newState = GroundState.OnUnstableSlope;
            }
            else if (hit.SlopeAngle > 5f)
            {
                // 稳定斜坡
                newState = GroundState.OnStableSlope;
            }
            else if (hit.IsOnEdge)
            {
                newState = GroundState.OnEdge;
            }
            else
            {
                newState = GroundState.Grounded;
            }

            // 状态防抖
            self.ApplyStateWithDebounce(newState, config);
        }

        private static void ApplyStateWithDebounce(
            this CheckGroundedComponent self, 
            GroundState newState,
            GroundDetectorConfig config)
        {
            if (newState == self.State)
            {
                self.ConsecutiveAirborneFrames = 0;
                self.ConsecutiveGroundedFrames = 0;
                return;
            }

            bool toAirborne = IsAirborne(newState);
            bool fromAirborne = IsAirborne(self.State);

            if (toAirborne && !fromAirborne)
            {
                // 地面 -> 空中
                self.ConsecutiveAirborneFrames++;
                self.ConsecutiveGroundedFrames = 0;

                if (self.ConsecutiveAirborneFrames >= config.StateChangeFrameThreshold)
                {
                    self.State = newState;
                    self.AirborneReason = AirborneReason.WalkOff;
                }
            }
            else if (!toAirborne && fromAirborne)
            {
                // 空中 -> 地面
                self.ConsecutiveGroundedFrames++;
                self.ConsecutiveAirborneFrames = 0;

                if (self.ConsecutiveGroundedFrames >= config.StateChangeFrameThreshold)
                {
                    self.State = GroundState.Landing;
                    self.TimeLanded = Time.time;
                }
            }
            else
            {
                // 同类状态切换（如 Grounded -> OnSlope）
                self.State = newState;
            }

            // Landing 过渡
            if (self.State == GroundState.Landing &&
                Time.time - self.TimeLanded > config.LandingBufferTime)
            {
                self.State = newState;
            }
        }

        private static void UpdateTimers(this CheckGroundedComponent self, float deltaTime)
        {
            if (IsGrounded(self.State))
            {
                self.GroundedDuration += deltaTime;
                self.AirborneDuration = 0;
                self.LastGroundedPosition = self.Transform.position;
            }
            else
            {
                self.AirborneDuration += deltaTime;
                self.GroundedDuration = 0;
            }

            // Coyote Time: 从地面状态离开，且不是下落状态
            self.InCoyoteTime = IsAirborne(self.State) &&
                               self.AirborneDuration < self.Config.CoyoteTime &&
                               self.AirborneReason == AirborneReason.WalkOff;
        }

        private static void HandleStateTransition(this CheckGroundedComponent self)
        {
            bool wasGrounded = IsGrounded(self.PrevState);
            bool isGrounded = IsGrounded(self.State);

            if (wasGrounded && !isGrounded)
            {
                self.TimeLeftGround = Time.time;
                self.InvokeLeftGround();
            }

            if (!wasGrounded && isGrounded)
            {
                float fallHeight = self.LastGroundedPosition.y - self.Transform.position.y;
                self.InvokeLanded();
                self.AirborneReason = AirborneReason.None;
            }
        }

        private static void UpdateIgnoredPlatform(this CheckGroundedComponent self)
        {
            if (self.IgnoredPlatform == null) return;

            if (Time.time > self.IgnorePlatformUntil ||
                self.Transform.position.y < self.IgnoredPlatform.bounds.min.y - 0.1f)
            {
                if (self.Capsule != null)
                {
                    Physics.IgnoreCollision(self.Capsule, self.IgnoredPlatform, false);
                }
                self.IgnoredPlatform = null;
            }
        }

        // ==================== 公共接口 ====================

        public static bool IsGrounded(GroundState state)
        {
            return state == GroundState.Grounded ||
                   state == GroundState.OnStableSlope ||
                   state == GroundState.OnEdge ||
                   state == GroundState.Landing;
        }

        public static bool IsAirborne(GroundState state)
        {
            return state == GroundState.Airborne || 
                   state == GroundState.Falling ||
                   state == GroundState.OnUnstableSlope;
        }

        public static bool CanJump(this CheckGroundedComponent self)
        {
            return IsGrounded(self.State) || self.InCoyoteTime;
        }

        public static void Jump(this CheckGroundedComponent self)
        {
            self.State = GroundState.Airborne;
            self.AirborneReason = AirborneReason.Jump;
            self.TimeLeftGround = Time.time;
            self.LastGroundedPosition = self.Transform.position;
            self.InCoyoteTime = false;
        }

        public static void StartDropThrough(this CheckGroundedComponent self)
        {
            if (!self.GroundHit.HasGround) return;
            if (self.GroundHit.Collider == null) return;

            var config = self.Config;
            int layer = self.GroundHit.Collider.gameObject.layer;
            
            // 检查是否是可穿透平台
            if ((config.PlatformMask.value & (1 << layer)) == 0) return;

            self.IgnoredPlatform = self.GroundHit.Collider;
            self.IgnorePlatformUntil = Time.time + 0.5f;

            if (self.Capsule != null)
            {
                Physics.IgnoreCollision(self.Capsule, self.IgnoredPlatform, true);
            }
        }

        public static Vector3 GetPlatformDelta(this CheckGroundedComponent self)
        {
            ref var hit = ref self.GroundHit;
            if (!hit.HasGround || hit.GroundTransform == null) return Vector3.zero;

            Vector3 currentPos = hit.GroundTransform.TransformPoint(hit.LocalPositionOnGround);
            Vector3 prevPos = hit.PrevGroundRotation * hit.LocalPositionOnGround + hit.PrevGroundPosition;
            
            return currentPos - prevPos;
        }

        public static Vector3 GetSlopeDirection(this CheckGroundedComponent self, Vector3 moveDir)
        {
            if (!self.GroundHit.HasGround) return moveDir;
            return Vector3.ProjectOnPlane(moveDir, self.GroundHit.Normal).normalized;
        }

        public static Vector3 GetSlideDirection(this CheckGroundedComponent self)
        {
            if (!self.GroundHit.HasGround) return Vector3.zero;
            return Vector3.ProjectOnPlane(Vector3.down, self.GroundHit.Normal).normalized;
        }
    }
    
    #region 辅助接口
    /// <summary>
    /// 地面表面类型接口 - 挂载到地面物体上
    /// </summary>
    public interface IGroundSurface
    {
        GroundSurfaceType SurfaceType { get; }
    }
    #endregion
}
