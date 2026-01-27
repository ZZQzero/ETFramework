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
				Log.Error("AnimancerComponent未找到");
				return;
			}
			
			// Layer0：Move/Jump（基础移动）
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
			if (self.LocomotionLoaded)
			{
				return;
			}

			var unit = self.GetParent<Unit>();
			var catalog = unit.GetComponent<AnimationCatalogComponent>();
			if (catalog == null ||
			    !catalog.TryGet(AnimationCatalogComponent.AnimKey.Locomotion_Move, out var moveAssetName) ||
			    !catalog.TryGet(AnimationCatalogComponent.AnimKey.Locomotion_Jump, out var jumpAssetName))
			{
				Log.Error("AnimationCatalog 缺失或未配置 Locomotion_Move/Locomotion_Jump，无法加载 Move/Jump TransitionAsset");
				return;
			}

			// 加载Move资源（水平移动混合动画）
			var moveAsset = await ResourcesLoadManager.Instance.LoadAssetAsync<ScriptableObject>(moveAssetName);
			if (self.IsDisposed || self.Animancer == null)
			{
				return;
			}
			if (moveAsset == null)
			{
				Log.Error($"加载Move TransitionAsset失败：{moveAssetName}");
				return;
			}

			// 转换为TransitionAsset
			if (moveAsset is TransitionAsset moveTransitionAsset)
			{
				ITransition moveTransition = moveTransitionAsset.GetTransition();
				if (moveTransition is LinearMixerTransition linearMixer)
				{
					self.MoveMixer = linearMixer;
					// 在Layer 0播放水平移动动画（初始状态）
					self.Animancer.Play(self.MoveMixer);
					self.MoveMixer.State.Parameter = 0;
				}
				else
				{
					Log.Error($"Move TransitionAsset不包含LinearMixerTransition，实际类型: {moveTransition?.GetType().Name}");
				}
			}

			// 加载Jump资源（跳跃混合动画）
			var jumpAsset = await ResourcesLoadManager.Instance.LoadAssetAsync<ScriptableObject>(jumpAssetName);
			if (self.IsDisposed || self.Animancer == null)
			{
				return;
			}
			if (jumpAsset == null)
			{
				Log.Error($"加载Jump TransitionAsset失败：{jumpAssetName}");
				return;
			}

			// 转换为TransitionAsset
			if (jumpAsset is TransitionAsset jumpTransitionAsset)
			{
				ITransition jumpTransition = jumpTransitionAsset.GetTransition();
				if (jumpTransition is LinearMixerTransition jumpMixer)
				{
					self.JumpMixer = jumpMixer;
				}
				else
				{
					Log.Error($"Jump TransitionAsset不包含LinearMixerTransition，实际类型: {jumpTransition?.GetType().Name}");
				}
			}

			self.LocomotionLoaded = self.MoveMixer != null && self.JumpMixer != null;
			await ETTask.CompletedTask;
		}

		[EntitySystem]
		private static void Update(this AnimatorComponent self)
		{
			if (self.CharacterController == null || self.MoveMixer == null || self.Animancer == null || self.JumpMixer == null)
			{
				return;
			}

			AnimancerLayer layer = self.Animancer; // 隐式转换到 Layer 0
			if (self.Ground.IsGrounded(self.Ground.State))
			{
				if (layer.CurrentState != self.MoveMixer.State)
				{
					self.Animancer.Play(self.MoveMixer,0.2f);
				}
				self.MoveMixer.State.Parameter = self.CharacterController.GetNormalizedAnimationSpeed();
			}
			else
			{
				if (layer.CurrentState != self.JumpMixer.State)
				{
					self.Animancer.Play(self.JumpMixer, 0.2f);
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