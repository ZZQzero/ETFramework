using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ET
{
    public class AnimatorComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public LinearMixerTransition MoveMixer;
        public LinearMixerTransition JumpMixer;
        public AnimancerComponent Animancer { get; set; }
        public CharacterControllerComponent CharacterController { get; set; }
        public CheckGroundedComponent Ground { get; set; }
        public InputComponent Input { get; set; }
        public AttackComponent Attack { get; set; }
    }
}