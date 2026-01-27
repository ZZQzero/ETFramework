using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ET
{
    public class AnimatorComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public LinearMixerTransition MoveMixer;
        public LinearMixerTransition JumpMixer;
        /// <summary>
        /// 攻击动画层（Animancer Layer1）。
        /// 设计：Move/Jump 常驻 Layer0，攻击在 Layer1 覆盖，从根上避免“段间被 Idle/Move 抢占”。
        /// </summary>
        public AnimancerLayer AttackLayer;
        public AnimancerComponent Animancer { get; set; }
        public CharacterControllerComponent CharacterController { get; set; }
        public CheckGroundedComponent Ground { get; set; }

        /// <summary>
        /// 是否已完成 Locomotion（Move/Jump）过渡资源加载。
        /// 用于避免重复异步加载与销毁后回写。
        /// </summary>
        public bool LocomotionLoaded { get; set; }
    }
}