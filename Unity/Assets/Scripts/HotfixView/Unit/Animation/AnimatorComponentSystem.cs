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
			self.Unit = self.GetParent<Unit>();
			self.InitComponentRefs(self.Unit);
			var obj = self.Unit.GetComponent<GameObjectComponent>().GameObject;
			self.Animancer = obj.GetComponent<AnimancerComponent>();
			if (self.Animancer == null)
			{
				Log.Error($"{self.Unit.UnitName} AnimancerComponent未找到");
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

			var characterController = self.CharacterController;
			if (characterController != null)
			{
				characterController.Animator = self.Animancer.Animator;
			}
			
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

			if (self.AnimationCatalog == null)
			{
				Log.Error("AnimationCatalogComponent 缺失");
				return;
			}

			// 1. 加载基础移动资源
			if (!self.LocomotionLoaded)
			{
				if (self.AnimationCatalog.TryGet(AnimationCatalogComponent.AnimKey.Locomotion_Move, out var moveName) &&
				    self.AnimationCatalog.TryGet(AnimationCatalogComponent.AnimKey.Locomotion_Jump, out var jumpName))
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
				self.HitAnimationDic.Clear();
				var hitHeavy = await self.LoadHitTransition<ClipTransition>(self.AnimationCatalog, AnimationCatalogComponent.AnimKey.Hit_Heavy);
				if (hitHeavy != null)
				{
					self.HitAnimationDic.Add(HitReactionType.HeavyHit,hitHeavy);
					self.HitAnimationDic.Add(HitReactionType.LightHit,hitHeavy);
				}
				var hitGitUp = await self.LoadHitTransition<ClipTransition>(self.AnimationCatalog, AnimationCatalogComponent.AnimKey.Hit_GetUp);
				if (hitGitUp != null)
				{
					self.HitAnimationDic.Add(HitReactionType.GetUp,hitGitUp);
				}
				var hitKnockdown = await self.LoadHitTransition<ClipTransition>(self.AnimationCatalog, AnimationCatalogComponent.AnimKey.Hit_Knockdown);
				if(hitKnockdown != null)
				{
					self.HitAnimationDic.Add(HitReactionType.Knockdown,hitKnockdown);
				}
				var hitGroundToAir = await self.LoadHitTransition<ClipTransition>(self.AnimationCatalog, AnimationCatalogComponent.AnimKey.Hit_GroundToAir);
				if(hitGroundToAir != null)
				{
					self.HitAnimationDic.Add(HitReactionType.GroundToAir,hitGroundToAir);
					self.HitAnimationDic.Add(HitReactionType.AirCombo,hitGroundToAir);
				}
				var hitAirToGround = await self.LoadHitTransition<ClipTransition>(self.AnimationCatalog, AnimationCatalogComponent.AnimKey.Hit_AirToGround);
				if(hitAirToGround != null)
				{
					self.HitAnimationDic.Add(HitReactionType.AirToGround,hitAirToGround);
				}
				self.HitAnimationsLoaded = true; // 即使部分缺失也标记，避免重复加载
			}

			await ETTask.CompletedTask;
		}

		private static async ETTask<T> LoadHitTransition<T>(this AnimatorComponent self, AnimationCatalogComponent catalog, AnimationCatalogComponent.AnimKey key) where T : class, ITransition
		{
			if (catalog.TryGet(key, out var assetName))
			{
				return await self.LoadTransition<T>(assetName);
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

			if (!self.HitReactionStartHooked)
			{
				var hitReaction = self.HitReaction;
				if (hitReaction != null)
				{
					hitReaction.OnHitReactionStart = self.OnHitReactionStart;
					self.HitReactionStartHooked = true;
				}
			}
			// 1. 优先级：受击状态优先 (Hit Authority)
			// 规则上处于受击并不等价于“必须播放受击动画”（可配置禁播/降级表现）
			if (self.HitReaction != null && self.HitReaction.IsInHitReaction)
			{
				// 受击期：强制将 Locomotion BlendTree 的 Parameter 钳为 0（Idle）。
				// 原因：受击动画 OnEnd 触发后会切回 MoveMixer，但此处 early return 导致
				// Parameter 不再被每帧更新，残余的走/跑权重会持续输出 RootMotion。
				if (self.MoveMixer?.State != null)
				{
					self.MoveMixer.State.Parameter = 0;
				}
				return;
			}

			// 2. 优先级：基础移动 (Locomotion)
			if (self.MoveMixer != null && self.JumpMixer != null)
			{
				self.SynthesizeLocomotionAnimation();
			}
		}

		private static void OnHitReactionStart(this AnimatorComponent self, HitState state,
			HitReactionType reactionType)
		{
			self.HitReaction.CurrentAnimEnd = false;
			var hit = self.HitReaction;
			if (self.HitAnimationDic.TryGetValue(reactionType, out var transition))
			{
				if (transition != null)
				{
					var curAnimaState = self.Animancer.Play(transition);
					curAnimaState.Time = 0;
					// 受击动画结束时，通过 OnEnd 回调切回 locomotion（Animancer 淡出时也会触发）
					curAnimaState.Events(self).OnEnd = () =>
					{
						hit.CurrentAnimEnd = true;
						if (state == HitState.GetUpHit)
						{
							self.SynthesizeLocomotionAnimation();
						}
					};

					// 受击瞬间：如果攻击层仍有权重，根据受击强度快速淡出，确保受击表现清晰
					if (self.AttackLayer != null && self.AttackLayer.Weight > 0.01f)
					{
						// 重度受击、击飞、倒地：瞬间切断攻击层
						// 轻度/中度受击：快速淡出 (0.1s)
						bool isHeavyHit = state == HitState.AirborneHit ||
						                  state == HitState.KnockdownHit ||
						                  reactionType == HitReactionType.GroundToAir;

						self.AttackLayer.StartFade(0f, isHeavyHit ? 0f : 0.1f);
					}
				}
			}
		}

		private static void SynthesizeLocomotionAnimation(this AnimatorComponent self)
		{
			AnimancerLayer layer = self.Animancer; // 隐式转换到 Layer 0
			if (self.Ground.IsGrounded(self.Ground.StateContext.State))
			{
				if (self.LocomotionIntent.IsMoveAllowed)
				{
					if (layer.CurrentState != self.MoveMixer.State)
					{
						// 落地回到 Locomotion：击飞/连段落地通常希望更快衔接
						float fade = 0.2f;
						if (self.Ground != null && self.Ground.IsAirborne(self.Ground.StateContext.PrevState))
						{
							switch (self.Ground.StateContext.AirborneReason)
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
					// 防御性兜底：即使 Move 被 Inhibitor 锁定也播放 Idle（Parameter=0），
					// 避免因 Inhibitor 泄漏导致动画永久卡在最后一帧
					if (layer.CurrentState != self.MoveMixer.State)
					{
						self.Animancer.Play(self.MoveMixer, 0.2f);
					}
					self.MoveMixer.State.Parameter = 0;
				}
			}
			else
			{
				if (self.JumpMixer == null || !self.LocomotionIntent.IsJumpAllowed)
				{
					return;
				}
				if (layer.CurrentState != self.JumpMixer.State)
				{
					// 离地进入空中
					float fade = 0.2f;
					if (self.Ground != null)
					{
						switch (self.Ground.StateContext.AirborneReason)
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
			// 清理事件订阅，防止内存泄漏
			if (self.HitReaction != null)
			{
				self.HitReaction.OnHitReactionStart = null;
			}
			
			self.MoveMixer = null;
			self.JumpMixer = null;
			self.Animancer = null;
			self.AttackLayer = null;
		}

	}
}