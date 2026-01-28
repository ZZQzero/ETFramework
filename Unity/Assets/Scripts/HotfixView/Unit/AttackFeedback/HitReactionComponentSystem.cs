using Animancer;
using UnityEngine;

namespace ET
{
    public static partial class HitReactionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HitReactionComponent self,Transform player)
        {
            self.Owner = player;
            self.OwnerUnit = self.GetParent<Unit>();
            self.HitStop = self.OwnerUnit.GetComponent<HitStopComponent>();
            self.Ground = self.OwnerUnit.GetComponent<CheckGroundedComponent>();
            self.LocomotionIntent = self.OwnerUnit.GetComponent<LocomotionIntentComponent>();
            self.LoadAnimationsAsync().NoContext();
        }

        [EntitySystem]
        private static void Destroy(this HitReactionComponent self)
        {
            // 确保恢复移动开关（避免受击中销毁导致永久锁移动）
            self.RestoreMovementIfNeeded();
            self.RestoreGroundDetectConfigIfNeeded();

            self.CurrentState = HitState.None;
            self.CurrentAnimState = null;
            self.OnHitReactionStart = null;
            self.OnHitReactionEnd = null;
            self.OnLanded = null;
        }

        [EntitySystem]
        private static void Update(this HitReactionComponent self)
        {
            if (!self.IsInHitReaction)
                return;

            switch (self.CurrentState)
            {
                case HitState.Stun:
                    self.UpdateStun();
                    break;
                case HitState.Knockback:
                    self.UpdateKnockback();
                    break;
                case HitState.Airborne:
                case HitState.Falling:
                    self.UpdateAirborne();
                    break;
                case HitState.Knockdown:
                    self.UpdateKnockdown();
                    break;
                case HitState.GetUp:
                    self.UpdateGetUp();
                    break;
            }
        }

        /// <summary>
        /// 加载动画资源
        /// </summary>
        private static async ETTask LoadAnimationsAsync(this HitReactionComponent self)
        {
            // 加载受击动画（根据实际资源路径调整）
            //var asset = await ResourcesLoadManager.Instance.LoadAssetAsync<AnimationClipAsset>("HitLight");
            
            // ... 加载其他动画
        }

        #region 受击类型处理
        
        /// <summary>
        /// 播放轻度受击
        /// </summary>
        public static void PlayLightHit(this HitReactionComponent self, Vector3 direction, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Light,
                TargetStateMask.Any,
                direction,
                knockbackForce: 0f,
                knockupForce: 0f,
                hitStunMs: stunMs,
                in HitReactionRequest.FeedbackPayload.Default));
        }

        /// <summary>
        /// 播放中度受击
        /// </summary>
        public static void PlayMediumHit(this HitReactionComponent self, Vector3 direction, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Medium,
                TargetStateMask.Any,
                direction,
                knockbackForce: 0f,
                knockupForce: 0f,
                hitStunMs: stunMs,
                in HitReactionRequest.FeedbackPayload.Default));
        }

        /// <summary>
        /// 播放重度受击
        /// </summary>
        public static void PlayHeavyHit(this HitReactionComponent self, Vector3 direction, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Heavy,
                TargetStateMask.Any,
                direction,
                knockbackForce: 0f,
                knockupForce: 0f,
                hitStunMs: stunMs,
                in HitReactionRequest.FeedbackPayload.Default));
        }

        /// <summary>
        /// 播放击退
        /// </summary>
        public static void PlayKnockback(this HitReactionComponent self, Vector3 direction, float force, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Knockback,
                TargetStateMask.Any,
                direction,
                knockbackForce: force,
                knockupForce: 0f,
                hitStunMs: stunMs,
                in HitReactionRequest.FeedbackPayload.Default));
        }

        /// <summary>
        /// 播放击飞
        /// </summary>
        public static void PlayKnockup(this HitReactionComponent self, float upForce, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Knockup,
                TargetStateMask.Any,
                Vector3.zero,
                knockbackForce: 0f,
                knockupForce: upForce,
                hitStunMs: stunMs,
                in HitReactionRequest.FeedbackPayload.Default));
        }

        /// <summary>
        /// 播放击倒
        /// </summary>
        public static void PlayKnockdown(this HitReactionComponent self, Vector3 direction, float force, int stunMs)
        {
            // Knockdown 可能附带一个“轻微上挑”用于离地，默认给一个小值（保留旧行为 5f）。
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Knockdown,
                TargetStateMask.Any,
                direction,
                knockbackForce: force,
                knockupForce: 5f,
                hitStunMs: stunMs,
                in HitReactionRequest.FeedbackPayload.Default));
        }
        
        #endregion

        #region 统一入口

        /// <summary>
        /// 尝试对目标应用一次“受击请求”。
        /// - 负责 TargetState 过滤、互斥/打断、状态机启动、反馈触发（顿帧等）
        /// - 不依赖攻击系统具体实现（Attack 只需生成 HitReactionRequest）
        /// </summary>
        public static bool TryApplyHit(this HitReactionComponent self, in HitReactionRequest request)
        {
            HitProfileLibrary.Resolve(self?.OwnerUnit, out var rulesProfile, out var feedbackProfile);
            var result = HitRules.Evaluate(self, in request, in rulesProfile);
            if (!result.Accepted)
            {
                return false;
            }

            HitReactionRequest effective = result.Request;

            // 进入受击：必要时先打断攻击/禁用移动
            self.BeginExternalLocksOnHit();

            // 反馈：目标侧顿帧
            self.ApplyFeedback(in effective, in feedbackProfile);

            // 按规则决策执行
            switch (result.Mode)
            {
                case HitRules.ApplyMode.Replace:
                    self.StartHitReaction(in effective);
                    break;
                case HitRules.ApplyMode.Refresh:
                    self.RefreshHitReaction(in effective);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private static void BeginExternalLocksOnHit(this HitReactionComponent self)
        {
            if (self.CancelAttackOnHit)
            {
                self.OwnerUnit?.GetComponent<AttackComponent>()?.ForceCancel();
                // 核心改进：清空攻击指令队列，防止受击结束后积压的“僵尸指令”瞬间爆发
                self.OwnerUnit?.GetComponent<AttackCommandComponent>()?.Clear();
            }

            if (self.LocomotionIntent != null)
            {
                // 核心改进：清理边沿触发意图，并禁用受击期间的行为能力
                self.LocomotionIntent.JumpRequested = false;
                self.LocomotionIntent.Capabilities &= ~(ActionCapabilities.Attack | ActionCapabilities.Jump | ActionCapabilities.Move);
            }

            if (!self.DisableMovementOnHit)
            {
                return;
            }

            // 玩家移动系统：先做到“锁移动”，避免受击位移与输入/攻击位移抢写
            var cc = self.OwnerUnit?.GetComponent<CharacterControllerComponent>();
            if (cc != null)
            {
                if (!self.HasCachedMovementEnabled)
                {
                    self.CachedMovementEnabled = cc.EnableMovement;
                    self.HasCachedMovementEnabled = true;
                }

                cc.EnableMovement = false;
            }
        }

        private static void RestoreMovementIfNeeded(this HitReactionComponent self)
        {
            if (!self.HasCachedMovementEnabled)
            {
                return;
            }

            var cc = self.OwnerUnit?.GetComponent<CharacterControllerComponent>();
            if (cc != null)
            {
                cc.EnableMovement = self.CachedMovementEnabled;
            }

            self.HasCachedMovementEnabled = false;
        }

        private static void ApplyFeedback(this HitReactionComponent self, in HitReactionRequest request, in HitFeedbackProfile profile)
        {
            // 目标侧顿帧：用 HitStopComponent（combat-time 一致）
            if (profile.Option.AllowVictimHitStop && request.VictimHitStopMs > 0)
            {
                int ms = request.VictimHitStopMs;
                if (profile.Option.VictimHitStopScale != 1f)
                {
                    ms = Mathf.RoundToInt(ms * Mathf.Max(0f, profile.Option.VictimHitStopScale));
                }
                if (ms <= 0)
                {
                    return;
                }

                var hitStop = self.OwnerUnit?.GetComponent<HitStopComponent>();
                var anim = self.OwnerUnit?.GetComponent<AnimatorComponent>()?.Animancer;
                if (hitStop != null && anim != null)
                {
                    hitStop.RequestHitStop(ms, anim, HitStopFreezeMode.FreezeAll);
                }
            }

            // - request.ScreenShakeIntensity/ScreenShakeDurationMs
            // - request.TimeScale/TimeScaleDurationMs
        }

        private static void StartHitReaction(this HitReactionComponent self, in HitReactionRequest request)
        {
            // 统一清理部分状态（避免残留）
            self.CurrentAnimState = null;
            self.KnockbackDirection = request.HitDirection.sqrMagnitude > 0.0001f ? request.HitDirection.normalized : Vector3.zero;
            self.KnockbackSpeed = Mathf.Max(0f, request.KnockbackForce);
            self.CurrentReactionType = request.ReactionType;

            // 记录空中原因：便于统一规则（击飞/连段）在多系统中消费
            if (self.Ground != null)
            {
                switch (request.ReactionType)
                {
                    case HitReactionType.Knockup:
                        self.Ground.AirborneReason = AirborneReason.Launched;
                        break;
                    case HitReactionType.Knockdown:
                        self.Ground.AirborneReason = AirborneReason.Knockdown;
                        break;
                }
            }

            // 语义约束已在规则层 HitRules.Normalize 做完（非 Knockup/Knockdown 的 KnockupForce 会被归零）
            // 因此执行层只看“归一化后的 KnockupForce 是否 > 0”。
            if (request.KnockupForce > 0f)
            {
                self.BoostGroundDetectForHitAirborne();
                if (self.Ground != null && self.Ground.AirborneReason == AirborneReason.None)
                {
                    self.Ground.AirborneReason = AirborneReason.Launched;
                }
            }

            // 如果目标由 CharacterControllerComponent 驱动：把“击飞向上速度”直接注入到 CC（重力由 CC 统一推进）
            var cc = self.OwnerUnit?.GetComponent<CharacterControllerComponent>();
            if (cc != null && request.KnockupForce > 0f)
            {
                Vector3 v = cc.CurrentVelocity;
                v.y = Mathf.Max(v.y, request.KnockupForce);
                cc.CurrentVelocity = v;
            }

            // 硬直截止点（combat-time）
            int stunMs = Mathf.Max(0, request.HitStunMs);
            self.StunEndTime = self.GetCombatNowMs() + stunMs;

            // 状态选择
            HitState state;
            switch (request.ReactionType)
            {
                case HitReactionType.Light:
                case HitReactionType.Medium:
                case HitReactionType.Heavy:
                    state = HitState.Stun;
                    break;
                case HitReactionType.Knockback:
                    state = HitState.Knockback;
                    break;
                case HitReactionType.Knockup:
                    state = HitState.Airborne;
                    break;
                case HitReactionType.Knockdown:
                    // 击倒：先按击退启动，落地后进入倒地/起身流程
                    state = HitState.Knockback;
                    break;
                default:
                    state = HitState.Stun;
                    break;
            }

            self.CurrentState = state;
            self.OnHitReactionStart?.Invoke(state);
            // 进化方案：不再手动调用 PlayAnimation，动画由 AnimatorComponentSystem.Update 自动合成。
        }

        /// <summary>
        /// 同级/低级受击：不重播动画，只刷新计时/力度，避免抖动。
        /// </summary>
        private static void RefreshHitReaction(this HitReactionComponent self, in HitReactionRequest request)
        {
            // 刷新反应类型（供动画合成参考）
            self.CurrentReactionType = request.ReactionType;

            // 刷新硬直截止点：取更晚的那个
            long end = self.GetCombatNowMs() + Mathf.Max(0, request.HitStunMs);
            if (end > self.StunEndTime)
            {
                self.StunEndTime = end;
            }

            // 刷新力度：取更大力度（更强命中不会被弱命中覆盖）
            if (request.KnockbackForce > self.KnockbackSpeed)
            {
                self.KnockbackSpeed = request.KnockbackForce;
                self.KnockbackDirection = request.HitDirection.sqrMagnitude > 0.0001f ? request.HitDirection.normalized : self.KnockbackDirection;
            }
            
            // 刷新击飞：规则层已保证只有 Knockup/Knockdown 才会保留 KnockupForce
            if (request.KnockupForce > 0f)
            {
                var cc = self.OwnerUnit?.GetComponent<CharacterControllerComponent>();
                if (cc != null)
                {
                    Vector3 v = cc.CurrentVelocity;
                    v.y = Mathf.Max(v.y, request.KnockupForce);
                    cc.CurrentVelocity = v;
                }
            }

            // 空中连段：标记为 Juggled（如果当前处于空中）
            if (self.Ground != null && self.IsAirborne)
            {
                self.Ground.AirborneReason = AirborneReason.Juggled;
                self.BoostGroundDetectForHitAirborne();
            }
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 更新硬直状态
        /// </summary>
        private static void UpdateStun(this HitReactionComponent self)
        {
            long currentTime = self.GetCombatNowMs();
            if (currentTime >= self.StunEndTime)
            {
                self.EndHitReaction();
            }
        }

        /// <summary>
        /// 更新击退状态
        /// </summary>
        private static void UpdateKnockback(this HitReactionComponent self)
        {
            float deltaTime = self.GetCombatDeltaSeconds();
            if (deltaTime <= 0f)
            {
                return;
            }

            // 本项目 Player/Monster 都由 CharacterControllerComponent 统一驱动运动学与重力。
            // 进化方案：受击系统只负责往 LocomotionIntent 注入“外部冲量”，由 Motor 统一合成执行。
            var intent = self.LocomotionIntent;
            if (intent == null)
            {
                return;
            }

            // 水平击退：写入 Intent 的外部冲量通道
            {
                float speed = self.KnockbackSpeed;
                if (speed < 0.0001f) speed = 0f;

                // 注入受击速度（XZ 轴）
                intent.ExternalImpulse = new Vector3(
                    self.KnockbackDirection.x * speed,
                    0f, // Y 轴通常由 CC 的重力/初始冲击力控制，这里不重复注入持续力
                    self.KnockbackDirection.z * speed);

                self.KnockbackSpeed = speed * Mathf.Clamp01(self.KnockbackDamping);
            }

            // 若由于击飞导致离地：从 Knockback 切换到 Airborne
            if (self.Ground != null && self.Ground.IsAirborne(self.Ground.State) && self.CurrentState == HitState.Knockback)
            {
                self.CurrentState = HitState.Airborne;
            }

            // 检查硬直结束
            long currentTime = self.GetCombatNowMs();
            if (currentTime >= self.StunEndTime && self.KnockbackSpeed < 0.1f &&
                self.Ground != null && self.Ground.IsGrounded(self.Ground.State))
            {
                self.EndHitReaction();
            }
        }

        /// <summary>
        /// 更新浮空状态
        /// </summary>
        private static void UpdateAirborne(this HitReactionComponent self)
        {
            float deltaTime = self.GetCombatDeltaSeconds();
            if (deltaTime <= 0f)
            {
                return;
            }
            var intent = self.LocomotionIntent;
            if (intent == null)
            {
                return;
            }

            // 空中阶段：重力由 CC.FixedUpdate 推进；这里只处理水平击退意图注入
            {
                float speed = self.KnockbackSpeed;
                if (speed < 0.0001f) speed = 0f;

                // 注入受击速度（XZ 轴）
                intent.ExternalImpulse = new Vector3(
                    self.KnockbackDirection.x * speed,
                    0f,
                    self.KnockbackDirection.z * speed);

                self.KnockbackSpeed = speed * Mathf.Clamp01(self.AirborneHorizontalDamping);
            }

            var cc = self.OwnerUnit?.GetComponent<CharacterControllerComponent>();
            // 切换到下落状态：以 CC 的垂直速度为准
            if (cc != null && cc.CurrentVelocity.y < -0.01f && self.CurrentState == HitState.Airborne)
            {
                self.CurrentState = HitState.Falling;
            }

            // 落地：以地检状态为准（不在受击系统中贴地/校正）
            if (self.Ground != null && self.Ground.IsGrounded(self.Ground.State))
            {
                self.OnLand();
            }
        }

        /// <summary>
        /// 落地处理
        /// </summary>
        private static void OnLand(this HitReactionComponent self)
        {
            self.OnLanded?.Invoke();

            // 根据之前的状态决定落地后的行为
            if (self.CurrentState == HitState.Falling || self.CurrentState == HitState.Airborne)
            {
                // 浮空落地后进入倒地
                self.CurrentState = HitState.Knockdown;
                self.KnockdownEndTime = self.GetCombatNowMs() + self.KnockdownDurationMs;
            }
            else
            {
                self.EndHitReaction();
            }
        }

        /// <summary>
        /// 更新倒地状态
        /// </summary>
        private static void UpdateKnockdown(this HitReactionComponent self)
        {
            long currentTime = self.GetCombatNowMs();
            if (currentTime >= self.KnockdownEndTime)
            {
                // 开始起身
                self.CurrentState = HitState.GetUp;
                self.GetUpStartTime = currentTime;
            }
        }

        /// <summary>
        /// 更新起身状态
        /// </summary>
        private static void UpdateGetUp(this HitReactionComponent self)
        {
            long now = self.GetCombatNowMs();

            // GetUp 兜底：超时强制结束（避免动画不推进或被打断导致永久不可受击）
            if (self.GetUpTimeoutMs > 0 && self.GetUpStartTime > 0 && now - self.GetUpStartTime >= self.GetUpTimeoutMs)
            {
                self.EndHitReaction();
                return;
            }

            // 正常路径：动画进度达到阈值则结束
            if (self.CurrentAnimState != null && self.CurrentAnimState.NormalizedTime >= 0.9f)
            {
                self.EndHitReaction();
                return;
            }

            // 异常路径：进入 GetUp 后动画状态为空（未配置/被打断）——给一个极短缓冲后结束
            if (self.CurrentAnimState == null && self.GetUpStartTime > 0 && now - self.GetUpStartTime >= 100)
            {
                self.EndHitReaction();
            }
        }

        /// <summary>
        /// 结束受击反应
        /// </summary>
        private static void EndHitReaction(this HitReactionComponent self)
        {
            self.CurrentState = HitState.None;
            self.CurrentAnimState = null;
            self.KnockbackSpeed = 0;
            self.GetUpStartTime = 0;

            // 清除意图层中的外部冲量
            if (self.LocomotionIntent != null)
            {
                self.LocomotionIntent.ExternalImpulse = Vector3.zero;
                // 恢复行为能力
                self.LocomotionIntent.Capabilities |= (ActionCapabilities.Attack | ActionCapabilities.Jump | ActionCapabilities.Move);
            }

            // 恢复移动开关
            self.RestoreMovementIfNeeded();

            // 恢复地检降频配置（若本次受击期间临时提升过）
            self.RestoreGroundDetectConfigIfNeeded();

            self.OnHitReactionEnd?.Invoke();
        }

        #region 地面检测协作（战斗手感）

        /// <summary>
        /// 受击浮空期间：临时提升地检频率，提升落地响应与稳定性。
        /// </summary>
        private static void BoostGroundDetectForHitAirborne(this HitReactionComponent self)
        {
            var g = self.Ground;
            var cfg = g?.Config;
            if (cfg == null)
            {
                return;
            }

            if (!self.HasCachedGroundDetectConfig)
            {
                self.HasCachedGroundDetectConfig = true;
                self.CachedReduceAirborneCheckFrequency = cfg.ReduceAirborneCheckFrequency;
                self.CachedAirborneCheckInterval = cfg.AirborneCheckInterval;
            }

            // 战斗浮空：宁可多检测一点，也不要落地/倒地反应延迟带来手感割裂
            cfg.ReduceAirborneCheckFrequency = false;
            cfg.AirborneCheckInterval = 1;
        }

        private static void RestoreGroundDetectConfigIfNeeded(this HitReactionComponent self)
        {
            if (!self.HasCachedGroundDetectConfig)
            {
                return;
            }

            var g = self.Ground;
            var cfg = g?.Config;
            if (cfg != null)
            {
                cfg.ReduceAirborneCheckFrequency = self.CachedReduceAirborneCheckFrequency;
                cfg.AirborneCheckInterval = self.CachedAirborneCheckInterval;
            }

            self.HasCachedGroundDetectConfig = false;
        }

        #endregion

        #endregion

        #region 公共方法
        
        /// <summary>
        /// 强制结束受击状态
        /// </summary>
        public static void ForceEndHitReaction(this HitReactionComponent self)
        {
            if (self.IsInHitReaction)
            {
                self.EndHitReaction();
            }
        }

        /// <summary>
        /// 检查是否可以追击（用于连击判定）
        /// </summary>
        public static bool CanBeJuggled(this HitReactionComponent self)
        {
            return self.IsAirborne;
        }
        
        #endregion

        #region CombatTime（顿帧期间不推进）

        private static long GetCombatNowMs(this HitReactionComponent self)
        {
            if (self.HitStop != null)
            {
                return self.HitStop.NowCombatMs();
            }

            // 兜底：没有 CombatFeedback 时退回 client frame time
            return TimeInfo.Instance.ClientFrameTime();
        }

        private static float GetCombatDeltaSeconds(this HitReactionComponent self)
        {
            if (self.HitStop != null)
            {
                return Mathf.Max(0f, self.HitStop.CombatDeltaMs * 0.001f);
            }

            // 兜底
            return Time.deltaTime;
        }

        #endregion
    }
}