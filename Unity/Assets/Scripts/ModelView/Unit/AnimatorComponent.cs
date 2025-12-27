using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ET
{
    public enum MotionType
    {
        None,
        Idle,
        Run,
    }

    [ComponentOf]
    public class AnimatorComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public Dictionary<string, AnimationClip> animationClips = new();
        public HashSet<string> Parameter = new();

        public MotionType MotionType;
        public float MontionSpeed;
        public bool isStop;
        public float stopSpeed;
        public Animator Animator;
        public LinearMixerTransition LocomotionMixer;
        public AnimancerComponent Animancer { get; set; }
        public CharacterControllerComponent CharacterController { get; set; }
    }
}