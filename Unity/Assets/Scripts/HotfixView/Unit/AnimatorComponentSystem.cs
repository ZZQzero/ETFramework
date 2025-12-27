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
			self.CharacterController = unit.GetComponent<CharacterControllerComponent>();
			
			var scriptableObject = await ResourcesLoadManager.Instance.LoadAssetAsync<ScriptableObject>("PlayerMove");
			if (scriptableObject == null)
			{
				Log.Error("加载PlayerMove资源失败：资源为null");
				return;
			}
				
			// 转换为TransitionAsset
			if (scriptableObject is TransitionAsset transitionAsset)
			{
				// 获取Transition并转换为LinearMixerTransition
				ITransition transition = transitionAsset.GetTransition();
				if (transition is LinearMixerTransition linearMixer)
				{
					self.LocomotionMixer = linearMixer;
					Log.Info("成功加载LinearMixerTransition");
				}
				else
				{
					Log.Error($"PlayerMove资源不包含LinearMixerTransition，实际类型: {transition?.GetType().Name}");
				}
			}
			else
			{
				Log.Error($"PlayerMove资源不是TransitionAsset类型，实际类型: {scriptableObject.GetType().Name}");
			}
		}
		
		[EntitySystem]
		private static void Update(this AnimatorComponent self)
		{
			if (self.CharacterController == null)
			{
				return;
			}

			if (self.LocomotionMixer != null)
			{
				self.Animancer.Play(self.LocomotionMixer);
				self.LocomotionMixer.State.Parameter = self.CharacterController.GetNormalizedAnimationSpeed();
			}
			//Log.Error($"{self.CharacterController.GetNormalizedAnimationSpeed()}  {self.CharacterController.GetVerticalAnimationSpeed()}");
		}
		
		[EntitySystem]
		private static void Destroy(this AnimatorComponent self)
		{
			self.animationClips = null;
			self.Parameter = null;
			self.Animator = null;
		}

	}
}