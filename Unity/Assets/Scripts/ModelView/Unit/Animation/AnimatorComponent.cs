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

        private ComponentRef<CharacterControllerComponent> characterControllerRef;
        private ComponentRef<MovementContextComponent> movementContextRef;
        private ComponentRef<CheckGroundedComponent> groundRef;
        private ComponentRef<CombatContextComponent> combatContextRef;
        private ComponentRef<HitReactionComponent> hitReactionRef;
        private ComponentRef<AnimationCatalogComponent> animationCatalogRef;
        /// <summary>
        /// 初始化组件引用（延迟解析，避免 Awake 时序/重复 GetComponent）。
        /// </summary>
        public void InitComponentRefs(Unit unit)
        {
            this.characterControllerRef = new ComponentRef<CharacterControllerComponent>(unit);
            this.movementContextRef = new ComponentRef<MovementContextComponent>(unit);
            this.groundRef = new ComponentRef<CheckGroundedComponent>(unit);
            this.combatContextRef = new ComponentRef<CombatContextComponent>(unit);
            this.hitReactionRef = new ComponentRef<HitReactionComponent>(unit);
            this.animationCatalogRef = new ComponentRef<AnimationCatalogComponent>(unit);
        }

        public CharacterControllerComponent CharacterController => this.characterControllerRef.Get();

        public CheckGroundedComponent Ground => this.movementContextRef.Get()?.Ground ?? this.groundRef.Get();

        public HitReactionComponent HitReaction => this.combatContextRef.Get()?.HitReaction ?? this.hitReactionRef.Get();
        public AnimationCatalogComponent AnimationCatalog => this.animationCatalogRef.Get();
        
        public LocomotionIntentComponent LocomotionIntent => this.movementContextRef.Get()?.LocomotionIntent;

        public Unit Unit;
        
        /// <summary>
        /// 受击事件是否已完成一次性绑定（避免 Update 中重复赋值）。
        /// </summary>
        public bool HitReactionStartHooked { get; set; }
        /// <summary>
        /// 是否已完成 Locomotion（Move/Jump）以及受击资源的加载。
        /// </summary>
        public bool LocomotionLoaded { get; set; }
        public bool HitAnimationsLoaded { get; set; }

    }
}