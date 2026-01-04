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

			if (obj.GetComponent<AttackEventReceiver>() == null)
			{
				obj.AddComponent<AttackEventReceiver>();
			}
			self.Input = unit.GetComponent<InputComponent>();
			self.Attack = unit.GetComponent<AttackComponent>();
			self.CharacterController = unit.GetComponent<CharacterControllerComponent>();
			self.Ground = self.CharacterController.Ground;
			self.LoadAnimation().NoContext();
		}

		private static async ETTask LoadAnimation(this AnimatorComponent self)
		{
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
					self.MoveMixer = linearMixer;
					// 在Layer 0播放水平移动动画（初始状态）
					self.Animancer.Play(self.MoveMixer);
					self.MoveMixer.State.Parameter = 0;
				}
				else
				{
					Log.Error($"PlayerMove资源不包含LinearMixerTransition，实际类型: {moveTransition?.GetType().Name}");
				}
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

			await ETTask.CompletedTask;
		}

		[EntitySystem]
		private static void Update(this AnimatorComponent self)
		{
			if (self.CharacterController == null || self.MoveMixer == null || self.Animancer == null || self.JumpMixer == null)
			{
				return;
			}

			// 检测攻击输入，交给AttackComponent处理
			if (self.Attack != null && self.Input != null && self.Input.HasAttackRequest())
			{
				self.Attack.HandleAttackInput();
			}
			// 仅在“真正攻击播放中/顿帧中”时阻止移动/跳跃动画切换。
			// Recovery 阶段允许恢复移动/待机动画，否则攻击段播完后会出现“没有动画”的空窗。
			if (self.Attack != null && self.Attack.IsAttacking)
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