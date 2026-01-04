using Animancer;
using UnityEngine;

namespace ET
{
    public static partial class HitReactionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HitReactionComponent self,Transform player)
        {
            self.Player = player;
            self.LoadAnimationsAsync().NoContext();
        }

        [EntitySystem]
        private static void Destroy(this HitReactionComponent self)
        {
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
            self.StartHitReaction(HitState.Stun, direction, 0, 0, stunMs);
            self.PlayAnimation(self.LightHitAnimation);
        }

        /// <summary>
        /// 播放中度受击
        /// </summary>
        public static void PlayMediumHit(this HitReactionComponent self, Vector3 direction, int stunMs)
        {
            self.StartHitReaction(HitState.Stun, direction, 0, 0, stunMs);
            self.PlayAnimation(self.MediumHitAnimation);
        }

        /// <summary>
        /// 播放重度受击
        /// </summary>
        public static void PlayHeavyHit(this HitReactionComponent self, Vector3 direction, int stunMs)
        {
            self.StartHitReaction(HitState.Stun, direction, 0, 0, stunMs);
            self.PlayAnimation(self.HeavyHitAnimation);
        }

        /// <summary>
        /// 播放击退
        /// </summary>
        public static void PlayKnockback(this HitReactionComponent self, Vector3 direction, float force, int stunMs)
        {
            self.StartHitReaction(HitState.Knockback, direction, force, 0, stunMs);
            self.PlayAnimation(self.KnockbackAnimation);
        }

        /// <summary>
        /// 播放击飞
        /// </summary>
        public static void PlayKnockup(this HitReactionComponent self, float upForce, int stunMs)
        {
            self.StartHitReaction(HitState.Airborne, Vector3.zero, 0, upForce, stunMs);
            self.PlayAnimation(self.AirborneAnimation);
        }

        /// <summary>
        /// 播放击倒
        /// </summary>
        public static void PlayKnockdown(this HitReactionComponent self, Vector3 direction, float force, int stunMs)
        {
            self.StartHitReaction(HitState.Knockback, direction, force, 5f, stunMs);
            self.PlayAnimation(self.KnockbackAnimation);
        }
        
        #endregion

        #region 内部方法
        
        /// <summary>
        /// 开始受击反应
        /// </summary>
        private static void StartHitReaction(this HitReactionComponent self, HitState state, 
            Vector3 direction, float knockbackForce, float upForce, int stunMs)
        {
            self.CurrentState = state;
            self.KnockbackDirection = direction.normalized;
            self.KnockbackSpeed = knockbackForce;
            self.VerticalVelocity = upForce;
            self.StunEndTime = TimeInfo.Instance.ClientFrameTime() + stunMs;

            // 通知其他组件（如攻击组件）停止当前动作
            var unit = self.GetParent<Unit>();
            var attackComponent = unit?.GetComponent<AttackComponent>();
            attackComponent?.ForceCancel();

            self.OnHitReactionStart?.Invoke(state);
        }

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
                self.CurrentAnimState = animatorComponent.Animancer.Play(animation, 0.05f);
            }
        }

        /// <summary>
        /// 更新硬直状态
        /// </summary>
        private static void UpdateStun(this HitReactionComponent self)
        {
            long currentTime = TimeInfo.Instance.ClientFrameTime();
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
            float deltaTime = Time.deltaTime;

            // 应用击退位移
            if (self.KnockbackSpeed > 0.1f)
            {
                Vector3 movement = self.KnockbackDirection * (self.KnockbackSpeed * deltaTime);
                self.Player.position += movement;
                
                // 衰减击退速度
                self.KnockbackSpeed *= 0.9f;
            }

            // 应用垂直速度
            if (self.VerticalVelocity > 0 || self.Player.position.y > self.GroundHeight)
            {
                self.VerticalVelocity -= self.Gravity * deltaTime;
                self.Player.position += Vector3.up * (self.VerticalVelocity * deltaTime);

                // 检查是否落地
                if (self.Player.position.y <= self.GroundHeight && self.VerticalVelocity < 0)
                {
                    self.Player.position = new Vector3(self.Player.position.x, self.GroundHeight, self.Player.position.z);
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
            long currentTime = TimeInfo.Instance.ClientFrameTime();
            if (currentTime >= self.StunEndTime && self.KnockbackSpeed < 0.1f && 
                self.Player.position.y <= self.GroundHeight)
            {
                self.EndHitReaction();
            }
        }

        /// <summary>
        /// 更新浮空状态
        /// </summary>
        private static void UpdateAirborne(this HitReactionComponent self)
        {
            float deltaTime = Time.deltaTime;
            // 应用重力
            self.VerticalVelocity -= self.Gravity * deltaTime;
            self.Player.position += Vector3.up * (self.VerticalVelocity * deltaTime);

            // 应用水平击退
            if (self.KnockbackSpeed > 0.1f)
            {
                Vector3 movement = self.KnockbackDirection * (self.KnockbackSpeed * deltaTime);
                self.Player.position += movement;
                self.KnockbackSpeed *= 0.95f;
            }

            // 切换到下落状态
            if (self.VerticalVelocity < 0 && self.CurrentState == HitState.Airborne)
            {
                self.CurrentState = HitState.Falling;
                self.PlayAnimation(self.FallingAnimation);
            }

            // 检查是否落地
            if (self.Player.position.y <= self.GroundHeight)
            {
                self.Player.position = new Vector3(self.Player.position.x, self.GroundHeight, self.Player.position.z);
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
                self.KnockdownEndTime = TimeInfo.Instance.ClientFrameTime() + self.KnockdownDurationMs;
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
            long currentTime = TimeInfo.Instance.ClientFrameTime();
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

            self.OnHitReactionEnd?.Invoke();
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
    }
}