using System;
using Animancer;
using UnityEngine;

namespace ET
{
	[EntitySystemOf(typeof(AnimatorComponent))]
	public static partial class AnimatorComponentSystem
	{
		[EntitySystem]
		private static void Awake(this AnimatorComponent self)
		{
			var unit = self.GetParent<Unit>();
			var obj = unit.GetComponent<GameObjectComponent>().GameObject;
			self.Animancer = obj.GetComponent<AnimancerComponent>();
			if (self.Animancer == null)
			{
				Log.Error($"{unit.UnitName} AnimancerComponent未找到");
				return;
			}
			
			// Layer0：Move/Jump/Hit（基础表现层）
			// Layer1：Attack（攻击覆盖），默认权重为 0
			self.Animancer.Layers.SetMinCount(2);
			self.AttackLayer = self.Animancer.Layers[1];
			self.AttackLayer.Weight = 0f;
			self.AttackLayer.Mask = self.CreateAttackBodyMask();

			// 禁用RootMotion，由CharacterControllerComponent手动控制移动
			if (self.Animancer.Animator != null)
			{
				self.Animancer.Animator.applyRootMotion = false;
			}

			if (obj.GetComponent<AttackEventReceiver>() == null)
			{
				obj.AddComponent<AttackEventReceiver>();
			}

			self.CharacterController = unit.GetComponent<CharacterControllerComponent>();
			self.CharacterController.Animator = self.Animancer.Animator;
			self.Ground = self.CharacterController.Ground;
			self.HitReaction = unit.GetComponent<HitReactionComponent>();
			
			self.LoadAnimation().NoContext();
		}
		
		private static AvatarMask CreateAttackBodyMask(this AnimatorComponent self)
		{
			var mask = new AvatarMask();
			
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, true);
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);

			// —— 下半身全打开 ——
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, true);
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, true);
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, true);
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, true);

			// —— 上半身打开 ——
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);

			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);

			// —— IK（可选，建议先关） ——
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, false);
			mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, false);

			return mask;
		}

		private static async ETTask LoadAnimation(this AnimatorComponent self)
		{
			if (self.LocomotionLoaded && self.HitAnimationsLoaded)
			{
				return;
			}

			var unit = self.GetParent<Unit>();
			var catalog = unit.GetComponent<AnimationCatalogComponent>();
			if (catalog == null)
			{
				Log.Error("AnimationCatalogComponent 缺失");
				return;
			}

			// 1. 加载基础移动资源
			if (!self.LocomotionLoaded)
			{
				if (catalog.TryGet(AnimationCatalogComponent.AnimKey.Locomotion_Move, out var moveName) &&
				    catalog.TryGet(AnimationCatalogComponent.AnimKey.Locomotion_Jump, out var jumpName))
				{
					self.MoveMixer = await self.LoadTransition<LinearMixerTransition>(moveName);
					self.JumpMixer = await self.LoadTransition<LinearMixerTransition>(jumpName);
					
					if (self.MoveMixer != null && self.Animancer != null)
					{
						self.Animancer.Play(self.MoveMixer);
						self.MoveMixer.State.Parameter = 0;
					}
					self.LocomotionLoaded = self.MoveMixer != null && self.JumpMixer != null;
				}
			}

			// 2. 加载受击资源
			if (!self.HitAnimationsLoaded)
			{
				self.HitLightTransition = await self.LoadHitTransition(catalog, AnimationCatalogComponent.AnimKey.Hit_Light);
				self.HitMediumTransition = await self.LoadHitTransition(catalog, AnimationCatalogComponent.AnimKey.Hit_Medium);
				self.HitHeavyTransition = await self.LoadHitTransition(catalog, AnimationCatalogComponent.AnimKey.Hit_Heavy);
				self.HitKnockbackTransition = await self.LoadHitTransition(catalog, AnimationCatalogComponent.AnimKey.Hit_Knockback);
				self.HitAirborneTransition = await self.LoadHitTransition(catalog, AnimationCatalogComponent.AnimKey.Hit_Airborne);
				self.HitFallingTransition = await self.LoadHitTransition(catalog, AnimationCatalogComponent.AnimKey.Hit_Falling);
				self.HitKnockdownTransition = await self.LoadHitTransition(catalog, AnimationCatalogComponent.AnimKey.Hit_Knockdown);
				self.HitGetUpTransition = await self.LoadHitTransition(catalog, AnimationCatalogComponent.AnimKey.Hit_GetUp);
				
				self.HitAnimationsLoaded = true; // 即使部分缺失也标记，避免重复加载
			}

			await ETTask.CompletedTask;
		}

		private static async ETTask<ITransition> LoadHitTransition(this AnimatorComponent self, AnimationCatalogComponent catalog, AnimationCatalogComponent.AnimKey key)
		{
			if (catalog.TryGet(key, out var assetName))
			{
				return await self.LoadTransition<ITransition>(assetName);
			}
			return null;
		}

		private static async ETTask<T> LoadTransition<T>(this AnimatorComponent self, string assetName) where T : class, ITransition
		{
			var asset = await ResourcesLoadManager.Instance.LoadAssetAsync<ScriptableObject>(assetName);
			if (self.IsDisposed || asset == null) return null;

			if (asset is TransitionAsset transitionAsset)
			{
				var transition = transitionAsset.GetTransition();
				if (transition is T result)
				{
					return result;
				}
				Log.Error($"TransitionAsset {assetName} 类型不匹配，期望 {typeof(T).Name}，实际 {transition?.GetType().Name}");
			}
			return null;
		}

		[EntitySystem]
		private static void Update(this AnimatorComponent self)
		{
			if (self.CharacterController == null || self.Animancer == null)
			{
				return;
			}

			// 1. 优先级：受击状态优先 (Hit Authority)
			if (self.HitReaction != null && self.HitReaction.IsInHitReaction)
			{
				self.SynthesizeHitAnimation();
				return;
			}

			// 2. 优先级：基础移动 (Locomotion)
			if (self.MoveMixer != null && self.JumpMixer != null)
			{
				self.SynthesizeLocomotionAnimation();
			}
		}

		private static void SynthesizeHitAnimation(this AnimatorComponent self)
		{
			ITransition transition = null;
			var hit = self.HitReaction;

			switch (hit.CurrentState)
			{
				case HitState.Stun:
					switch (hit.CurrentReactionType)
					{
						case HitReactionType.Medium: transition = self.HitMediumTransition; break;
						case HitReactionType.Heavy:  transition = self.HitHeavyTransition; break;
						case HitReactionType.Stagger: transition = self.HitMediumTransition; break;
						case HitReactionType.Stun: transition = self.HitHeavyTransition; break;
						default:                     transition = self.HitLightTransition; break;
					}
					break;
				case HitState.Knockback: transition = self.HitKnockbackTransition; break;
				case HitState.Airborne:  transition = self.HitAirborneTransition; break;
				case HitState.Falling:   transition = self.HitFallingTransition; break;
				case HitState.Knockdown: transition = self.HitKnockdownTransition; break;
				case HitState.GetUp:     transition = self.HitGetUpTransition; break;
			}

			// 回退逻辑：如果没有配置对应的受击动画，尝试播放最基础的轻度受击
			if (transition == null)
			{
				transition = self.HitLightTransition;
			}

			if (transition != null)
			{
				// 优化：仅在 Transition 改变时调用 Play
				// 注意：这里使用 Key 比对，AnimancerState.Key 默认通常是 Transition 对象
				if (self.Animancer.Layers[0].CurrentState?.Key as ITransition != transition)
				{
					var state = self.Animancer.Play(transition, hit != null ? hit.AnimationFadeSec : 0.05f);

					// 受击瞬间：如果攻击层仍有权重，根据受击强度快速淡出，确保受击表现清晰
					if (self.AttackLayer != null && self.AttackLayer.Weight > 0.01f)
					{
						// 重度受击、击飞、倒地：瞬间切断攻击层
						// 轻度/中度受击：快速淡出 (0.1s)
						bool isHeavyHit = hit.CurrentState == HitState.Airborne || 
						                  hit.CurrentState == HitState.Knockdown || 
						                  hit.CurrentReactionType == HitReactionType.Heavy;
						
						self.AttackLayer.StartFade(0f, isHeavyHit ? 0f : 0.1f);
					}
				}
			}

			// 单一权威：CurrentAnimState 始终由 Animator 合成侧维护
			// - 即使 transition 未变化（不触发 Play），也要更新引用，避免受击系统把 CurrentAnimState 置空后无法恢复。
			if (hit != null)
			{
				hit.CurrentAnimState = self.Animancer.Layers[0].CurrentState;
			}
		}

		private static void SynthesizeLocomotionAnimation(this AnimatorComponent self)
		{
			AnimancerLayer layer = self.Animancer; // 隐式转换到 Layer 0
			if (self.Ground.IsGrounded(self.Ground.State))
			{
				if (layer.CurrentState != self.MoveMixer.State)
				{
					// 落地回到 Locomotion：击飞/连段落地通常希望更快衔接
					float fade = 0.2f;
					if (self.Ground != null && self.Ground.IsAirborne(self.Ground.PrevState))
					{
						switch (self.Ground.AirborneReason)
						{
							case AirborneReason.Launched:
							case AirborneReason.Juggled:
							case AirborneReason.Knockdown:
								fade = 0.12f;
								break;
						}
					}
					self.Animancer.Play(self.MoveMixer, fade);
				}
				self.MoveMixer.State.Parameter = self.CharacterController.GetNormalizedAnimationSpeed();
			}
			else
			{
				if (layer.CurrentState != self.JumpMixer.State)
				{
					// 离地进入空中
					float fade = 0.2f;
					if (self.Ground != null)
					{
						switch (self.Ground.AirborneReason)
						{
							case AirborneReason.Launched:
							case AirborneReason.Juggled:
							case AirborneReason.Knockdown:
								fade = 0.08f;
								break;
						}
					}
					self.Animancer.Play(self.JumpMixer, fade);
				}
				self.JumpMixer.State.Parameter = self.CharacterController.GetVerticalAnimationSpeed();
			}
		}
		
		[EntitySystem]
		private static void Destroy(this AnimatorComponent self)
		{
			self.MoveMixer = null;
			self.JumpMixer = null;
		}

	}
}