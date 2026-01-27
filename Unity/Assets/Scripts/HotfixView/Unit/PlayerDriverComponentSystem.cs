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
            self.LocomotionIntent.MoveDirection = move;

            // FaceDirection：优先使用“瞄准方向”（来自输入缓存），没有则退回移动方向
            // 注意：GetAimDirection 需要 owner transform，这里用 UnitView 的 Transform 更合理；
            // 但 Driver 不依赖 View 组件时，先回退用 move 方向（保持纯逻辑）。
            Vector3 face = self.Input.LastAimDirection;
            face.y = 0f;
            if (face.sqrMagnitude > 0.001f)
            {
                face.Normalize();
                self.LocomotionIntent.FaceDirection = face;
            }
            else
            {
                self.LocomotionIntent.FaceDirection = move;
            }

            // Jump（边沿触发）：按住也只会持续为 true，消费端会清除
            if (self.Input.HasJumpRequest())
            {
                self.LocomotionIntent.JumpRequested = true;
            }

            // Attack（边沿触发：Input 里已经是 GetMouseButtonDown）
            if (self.Input.HasAttackRequest())
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

