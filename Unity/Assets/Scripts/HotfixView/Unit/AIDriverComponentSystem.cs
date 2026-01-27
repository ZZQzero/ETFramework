using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(AIDriverComponent))]
    public static partial class AIDriverComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AIDriverComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            self.LocomotionIntent = unit.GetComponent<LocomotionIntentComponent>();
            self.AttackCommand = unit.GetComponent<AttackCommandComponent>();
        }

        [EntitySystem]
        private static void Update(this AIDriverComponent self)
        {
            if (self.LocomotionIntent == null || self.AttackCommand == null)
            {
                return;
            }

            Vector3 move = self.DesiredMoveDirection;
            move.y = 0f;
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }
            self.LocomotionIntent.MoveDirection = move;

            Vector3 face = self.DesiredFaceDirection;
            face.y = 0f;
            if (face.sqrMagnitude > 0.001f)
            {
                face.Normalize();
                self.LocomotionIntent.FaceDirection = face;
            }
            else
            {
                // 没有 face 方向时回退用 move
                self.LocomotionIntent.FaceDirection = move;
            }

            if (self.DesiredJump)
            {
                self.LocomotionIntent.JumpRequested = true;
                self.DesiredJump = false;
            }

            if (self.DesiredAttack)
            {
                self.AttackCommand.Enqueue(new AttackCommandComponent.AttackCommand
                {
                    SkillId = 0,
                    InputType = ComboInputType.Normal,
                    TargetUnitId = 0,
                });
                self.DesiredAttack = false;
            }
        }
    }
}