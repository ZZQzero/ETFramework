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
        public InputComponent Input { get; set; }
        public AttackComponent Attack { get; set; }
    }
}