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
            self.LoadAnimationsAsync().NoContext();

            // 商业级默认：以当前脚底为“地面高度”初始值（后续可接 Ground/地形系统动态更新）
            if (self.Owner != null)
            {
                self.GroundHeight = self.Owner.position.y;
            }
        }

        [EntitySystem]
        private static void Destroy(this HitReactionComponent self)
        {
            // 确保恢复移动开关（避免受击中销毁导致永久锁移动）
            self.RestoreMovementIfNeeded();

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
                HitReactionType.Light, TargetStateMask.Any, direction, 0f, 0f, stunMs));
        }

        /// <summary>
        /// 播放中度受击
        /// </summary>
        public static void PlayMediumHit(this HitReactionComponent self, Vector3 direction, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Medium, TargetStateMask.Any, direction, 0f, 0f, stunMs));
        }

        /// <summary>
        /// 播放重度受击
        /// </summary>
        public static void PlayHeavyHit(this HitReactionComponent self, Vector3 direction, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Heavy, TargetStateMask.Any, direction, 0f, 0f, stunMs));
        }

        /// <summary>
        /// 播放击退
        /// </summary>
        public static void PlayKnockback(this HitReactionComponent self, Vector3 direction, float force, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Knockback, TargetStateMask.Any, direction, force, 0f, stunMs));
        }

        /// <summary>
        /// 播放击飞
        /// </summary>
        public static void PlayKnockup(this HitReactionComponent self, float upForce, int stunMs)
        {
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Knockup, TargetStateMask.Any, Vector3.zero, 0f, upForce, stunMs));
        }

        /// <summary>
        /// 播放击倒
        /// </summary>
        public static void PlayKnockdown(this HitReactionComponent self, Vector3 direction, float force, int stunMs)
        {
            // Knockdown 可能附带一个“轻微上挑”用于离地，默认给一个小值（保留旧行为 5f）。
            self.TryApplyHit(new HitReactionRequest(
                HitReactionType.Knockdown, TargetStateMask.Any, direction, force, 5f, stunMs));
        }
        
        #endregion

        #region 统一入口（商业级）

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

            // 反馈：目标侧顿帧（商业级：只影响表现域，不影响数值/服务端）
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
            if (profile.AllowVictimHitStop && request.HitStopMs > 0)
            {
                int ms = request.HitStopMs;
                if (profile.VictimHitStopScale != 1f)
                {
                    ms = Mathf.RoundToInt(ms * Mathf.Max(0f, profile.VictimHitStopScale));
                }
                if (ms <= 0)
                {
                    return;
                }

                var hitStop = self.OwnerUnit?.GetComponent<HitStopComponent>();
                var anim = self.OwnerUnit?.GetComponent<AnimatorComponent>()?.Animancer;
                if (hitStop != null && anim != null)
                {
                    hitStop.RequestHitStop(ms, anim);
                }
            }

            // 震屏/慢动作：商业级应由统一反馈系统/相机系统处理，这里先预留扩展点（不硬编码 Camera 单例）。
            // - request.ScreenShakeIntensity/Duration
            // - request.TimeScale/TimeScaleDurationMs
        }

        private static void StartHitReaction(this HitReactionComponent self, in HitReactionRequest request)
        {
            // 统一清理部分状态（避免残留）
            self.CurrentAnimState = null;
            self.KnockbackDirection = request.HitDirection.sqrMagnitude > 0.0001f ? request.HitDirection.normalized : Vector3.zero;
            self.KnockbackSpeed = Mathf.Max(0f, request.KnockbackForce);
            self.VerticalVelocity = Mathf.Max(0f, request.KnockupForce);

            // 商业级：以当前高度作为“落地基准”（后续可替换为地形/地面检测）
            self.GroundHeight = self.Owner.position.y;

            // 硬直截止点（combat-time）
            int stunMs = Mathf.Max(0, request.HitStunMs);
            self.StunEndTime = self.GetCombatNowMs() + stunMs;

            // 状态/动画选择
            HitState state;
            ITransition anim;
            switch (request.ReactionType)
            {
                case HitReactionType.Light:
                    state = HitState.Stun;
                    anim = self.LightHitAnimation;
                    break;
                case HitReactionType.Medium:
                    state = HitState.Stun;
                    anim = self.MediumHitAnimation;
                    break;
                case HitReactionType.Heavy:
                    state = HitState.Stun;
                    anim = self.HeavyHitAnimation;
                    break;
                case HitReactionType.Knockback:
                    state = HitState.Knockback;
                    anim = self.KnockbackAnimation;
                    break;
                case HitReactionType.Knockup:
                    state = HitState.Airborne;
                    anim = self.AirborneAnimation;
                    break;
                case HitReactionType.Knockdown:
                    // 击倒：先按击退启动，落地后进入倒地/起身流程
                    state = HitState.Knockback;
                    anim = self.KnockbackAnimation;
                    break;
                default:
                    state = HitState.Stun;
                    anim = self.LightHitAnimation;
                    break;
            }

            self.CurrentState = state;
            self.OnHitReactionStart?.Invoke(state);
            self.PlayAnimation(anim);
        }

        /// <summary>
        /// 同级/低级受击：不重播动画，只刷新计时/力度，避免抖动。
        /// </summary>
        private static void RefreshHitReaction(this HitReactionComponent self, in HitReactionRequest request)
        {
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

            if (request.KnockupForce > self.VerticalVelocity)
            {
                self.VerticalVelocity = request.KnockupForce;
            }
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 播放动画
        /// </summary>
        private static void PlayAnimation(this HitReactionComponent self, ITransition animation)
        {
            if (animation == null)
                return;

            var unit = self.GetParent<Unit>();
            var animatorComponent = unit?.GetComponent<AnimatorComponent>();
            if (animatorComponent?.Animancer != null)
            {
                float fade = Mathf.Max(0f, self.AnimationFadeSec);
                self.CurrentAnimState = animatorComponent.Animancer.Play(animation, fade);
            }
        }

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

            // 应用击退位移
            if (self.KnockbackSpeed > 0.1f)
            {
                Vector3 movement = self.KnockbackDirection * (self.KnockbackSpeed * deltaTime);
                self.ApplyOwnerMove(movement);
                
                // 衰减击退速度
                self.KnockbackSpeed *= Mathf.Clamp01(self.KnockbackDamping);
            }

            // 应用垂直速度
            if (self.VerticalVelocity > 0 || self.Owner.position.y > self.GroundHeight)
            {
                self.VerticalVelocity -= self.Gravity * deltaTime;
                self.ApplyOwnerMove(Vector3.up * (self.VerticalVelocity * deltaTime));

                // 检查是否落地
                if (self.Owner.position.y <= self.GroundHeight && self.VerticalVelocity < 0)
                {
                    self.SetOwnerPosition(new Vector3(self.Owner.position.x, self.GroundHeight, self.Owner.position.z));
                    self.OnLand();
                    return;
                }

                // 切换到下落状态
                if (self.VerticalVelocity < 0 && self.CurrentState == HitState.Airborne)
                {
                    self.CurrentState = HitState.Falling;
                    self.PlayAnimation(self.FallingAnimation);
                }
            }

            // 检查硬直结束
            long currentTime = self.GetCombatNowMs();
            if (currentTime >= self.StunEndTime && self.KnockbackSpeed < 0.1f && 
                self.Owner.position.y <= self.GroundHeight)
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
            // 应用重力
            self.VerticalVelocity -= self.Gravity * deltaTime;
            self.ApplyOwnerMove(Vector3.up * (self.VerticalVelocity * deltaTime));

            // 应用水平击退
            if (self.KnockbackSpeed > 0.1f)
            {
                Vector3 movement = self.KnockbackDirection * (self.KnockbackSpeed * deltaTime);
                self.ApplyOwnerMove(movement);
                self.KnockbackSpeed *= Mathf.Clamp01(self.AirborneHorizontalDamping);
            }

            // 切换到下落状态
            if (self.VerticalVelocity < 0 && self.CurrentState == HitState.Airborne)
            {
                self.CurrentState = HitState.Falling;
                self.PlayAnimation(self.FallingAnimation);
            }

            // 检查是否落地
            if (self.Owner.position.y <= self.GroundHeight)
            {
                self.SetOwnerPosition(new Vector3(self.Owner.position.x, self.GroundHeight, self.Owner.position.z));
                self.OnLand();
            }
        }

        /// <summary>
        /// 落地处理
        /// </summary>
        private static void OnLand(this HitReactionComponent self)
        {
            self.VerticalVelocity = 0;
            self.OnLanded?.Invoke();

            // 根据之前的状态决定落地后的行为
            if (self.CurrentState == HitState.Falling || self.CurrentState == HitState.Airborne)
            {
                // 浮空落地后倒地
                self.CurrentState = HitState.Knockdown;
                self.KnockdownEndTime = self.GetCombatNowMs() + self.KnockdownDurationMs;
                self.PlayAnimation(self.KnockdownAnimation);
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
                self.PlayAnimation(self.GetUpAnimation);
            }
        }

        /// <summary>
        /// 更新起身状态
        /// </summary>
        private static void UpdateGetUp(this HitReactionComponent self)
        {
            if (self.CurrentAnimState != null && self.CurrentAnimState.NormalizedTime >= 0.9f)
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
            self.VerticalVelocity = 0;

            // 恢复移动开关
            self.RestoreMovementIfNeeded();

            self.OnHitReactionEnd?.Invoke();
        }

        #endregion

        #region 位移驱动（默认：玩家走 Rigidbody，其他走 Transform）

        private static void ApplyOwnerMove(this HitReactionComponent self, Vector3 delta)
        {
            if (self.Owner == null || delta == Vector3.zero)
            {
                return;
            }

            // 玩家：若存在 CharacterControllerComponent + Rigidbody，优先走 Rigidbody.MovePosition（更稳定的碰撞/插值）
            var cc = self.OwnerUnit?.GetComponent<CharacterControllerComponent>();
            if (cc?.Rigidbody != null)
            {
                cc.Rigidbody.MovePosition(cc.Rigidbody.position + delta);
                return;
            }

            // 兜底：直接改 Transform（适用于怪物/无物理驱动目标）
            self.Owner.position += delta;
        }

        private static void SetOwnerPosition(this HitReactionComponent self, Vector3 position)
        {
            if (self.Owner == null)
            {
                return;
            }

            var cc = self.OwnerUnit?.GetComponent<CharacterControllerComponent>();
            if (cc?.Rigidbody != null)
            {
                cc.Rigidbody.MovePosition(position);
                return;
            }

            self.Owner.position = position;
        }
        
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

        private static HitStopComponent GetFeedback(this HitReactionComponent self)
        {
            var unit = self.GetParent<Unit>();
            return unit?.GetComponent<HitStopComponent>();
        }

        private static long GetCombatNowMs(this HitReactionComponent self)
        {
            var fb = self.GetFeedback();
            if (fb != null)
            {
                return fb.NowCombatMs();
            }

            // 兜底：没有 CombatFeedback 时退回 client frame time
            return TimeInfo.Instance.ClientFrameTime();
        }

        private static float GetCombatDeltaSeconds(this HitReactionComponent self)
        {
            var fb = self.GetFeedback();
            if (fb != null)
            {
                return Mathf.Max(0f, fb.CombatDeltaMs * 0.001f);
            }

            // 兜底
            return Time.deltaTime;
        }

        #endregion
    }
}