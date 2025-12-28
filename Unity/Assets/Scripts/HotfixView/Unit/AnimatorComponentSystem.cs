using System;
using Animancer;
using UnityEngine;

namespace ET
{
	[EntitySystemOf(typeof(AnimatorComponent))]
	public static partial class AnimatorComponentSystem
	{
		[EntitySystem]
		private static async void Awake(this AnimatorComponent self)
		{
			var unit = self.GetParent<Unit>();
			var obj = unit.GetComponent<GameObjectComponent>().GameObject;
			self.Animancer = obj.GetComponent<AnimancerComponent>();
			if (self.Animancer == null)
			{
				Log.Error("AnimancerComponent未找到");
				return;
			}
			
			self.CharacterController = unit.GetComponent<CharacterControllerComponent>();
			self.Ground = self.CharacterController.Ground;
			
			// 加载PlayerMove资源（水平移动混合动画）
			var moveAsset = await ResourcesLoadManager.Instance.LoadAssetAsync<ScriptableObject>("PlayerMove");
			if (moveAsset == null)
			{
				Log.Error("加载PlayerMove资源失败：资源为null");
				return;
			}
				
			// 转换为TransitionAsset
			if (moveAsset is TransitionAsset moveTransitionAsset)
			{
				ITransition moveTransition = moveTransitionAsset.GetTransition();
				if (moveTransition is LinearMixerTransition linearMixer)
				{
					self.LocomotionMixer = linearMixer;
					// 在Layer 0播放水平移动动画（初始状态）
					self.Animancer.Play(self.LocomotionMixer);
					self.LocomotionMixer.State.Parameter = 0;
				}
				else
				{
					Log.Error($"PlayerMove资源不包含LinearMixerTransition，实际类型: {moveTransition?.GetType().Name}");
				}
			}
			else
			{
				Log.Error($"PlayerMove资源不是TransitionAsset类型，实际类型: {moveAsset.GetType().Name}");
			}
			
			// 加载PlayerJump资源（跳跃混合动画）
			var jumpAsset = await ResourcesLoadManager.Instance.LoadAssetAsync<ScriptableObject>("PlayerJump");
			if (jumpAsset == null)
			{
				Log.Error("加载PlayerJump资源失败：资源为null");
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
					Log.Error($"PlayerJump资源不包含LinearMixerTransition，实际类型: {jumpTransition?.GetType().Name}");
				}
			}
			else
			{
				Log.Error($"PlayerJump资源不是TransitionAsset类型，实际类型: {jumpAsset.GetType().Name}");
			}
		}
		
		[EntitySystem]
		private static void Update(this AnimatorComponent self)
		{
			if (self.CharacterController == null || self.LocomotionMixer == null || self.Animancer == null || self.JumpMixer == null)
			{
				return;
			}

			AnimancerLayer layer = self.Animancer; // 隐式转换到 Layer 0
			if (self.Ground.IsGrounded(self.Ground.State))
			{
				if (layer.CurrentState != self.LocomotionMixer.State)
				{
					self.Animancer.Play(self.LocomotionMixer,0.2f);
				}
				self.LocomotionMixer.State.Parameter = self.CharacterController.GetNormalizedAnimationSpeed();
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
			self.animationClips = null;
			self.Parameter = null;
			self.Animator = null;
			self.LocomotionMixer = null;
			self.JumpMixer = null;
		}

	}
}