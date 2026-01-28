using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(PlayerDriverComponent))]
    public static partial class PlayerDriverComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerDriverComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            self.Input = unit.GetComponent<InputComponent>();
            self.LocomotionIntent = unit.GetComponent<LocomotionIntentComponent>();
            self.AttackCommand = unit.GetComponent<AttackCommandComponent>();
        }

        [EntitySystem]
        private static void Update(this PlayerDriverComponent self)
        {
            if (self.Input == null || self.LocomotionIntent == null || self.AttackCommand == null)
            {
                return;
            }

            if (!self.Input.EnableInput)
            {
                self.LocomotionIntent.MoveDirection = Vector3.zero;
                return;
            }

            // Move / Face
            Vector3 move = self.Input.GetMoveDirection();
            move.y = 0f;
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            // 驱动层抑制：如果当前不具备移动能力，则意图方向为零
            if (!self.LocomotionIntent.Capabilities.HasFlag(ActionCapabilities.Move))
            {
                move = Vector3.zero;
            }
            self.LocomotionIntent.MoveDirection = move;

            // FaceDirection：优先使用“瞄准方向”（来自输入缓存），没有则退回移动方向
            Vector3 face = self.Input.LastAimDirection;
            face.y = 0f;
            if (face.sqrMagnitude > 0.001f)
            {
                face.Normalize();
                // 驱动层抑制：如果当前不具备旋转能力，则不更新朝向意图
                if (self.LocomotionIntent.Capabilities.HasFlag(ActionCapabilities.Rotate))
                {
                    self.LocomotionIntent.FaceDirection = face;
                }
            }
            else
            {
                self.LocomotionIntent.FaceDirection = move;
            }

            // Jump（边沿触发）：按住也只会持续为 true，消费端会清除
            if (self.Input.HasJumpRequest())
            {
                // 驱动层抑制：如果当前不具备跳跃能力，则不产生跳跃意图
                if (self.LocomotionIntent.Capabilities.HasFlag(ActionCapabilities.Jump))
                {
                    self.LocomotionIntent.JumpRequested = true;
                }
            }

            // Attack（边沿触发：Input 里已经是 GetMouseButtonDown）
            if (self.Input.HasAttackRequest())
            {
                // 驱动层抑制：如果当前不具备攻击能力，则不产生指令
                if (self.LocomotionIntent.Capabilities.HasFlag(ActionCapabilities.Attack))
                {
                    self.AttackCommand.Enqueue(new AttackCommandComponent.AttackCommand
                    {
                        SkillId = 0, // 0 = BasicAttackSkillId
                        InputType = ComboInputType.Normal,
                        TargetUnitId = 0,
                    });
                }
            }
        }
    }
}

