using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ET
{
    public class AnimatorComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public LinearMixerTransition MoveMixer;
        public LinearMixerTransition JumpMixer;

        // 受击系列 Transitions
        public ITransition HitLightTransition;
        public ITransition HitMediumTransition;
        public ITransition HitHeavyTransition;
        public ITransition HitKnockbackTransition;
        public ITransition HitAirborneTransition;
        public ITransition HitFallingTransition;
        public ITransition HitKnockdownTransition;
        public ITransition HitGetUpTransition;

        /// <summary>
        /// 攻击动画层（Animancer Layer1）。
        /// 设计：Move/Jump 常驻 Layer0，攻击在 Layer1 覆盖，从根上避免“段间被 Idle/Move 抢占”。
        /// </summary>
        public AnimancerLayer AttackLayer;
        public AnimancerComponent Animancer { get; set; }
        public CharacterControllerComponent CharacterController { get; set; }
        public CheckGroundedComponent Ground { get; set; }
        public HitReactionComponent HitReaction { get; set; }

        /// <summary>
        /// 是否已完成 Locomotion（Move/Jump）以及受击资源的加载。
        /// </summary>
        public bool LocomotionLoaded { get; set; }
        public bool HitAnimationsLoaded { get; set; }
    }
}