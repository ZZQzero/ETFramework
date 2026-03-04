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
            self.InitComponentRefs(unit);
        }

        [EntitySystem]
        private static void Update(this AIDriverComponent self)
        {
            if (self.LocomotionIntent == null || self.AttackCommand == null)
            {
                return;
            }

            // AirCombo（被挂空中）：AI 不推进移动/跳跃/转向，避免寻路/行为树持续写入导致抖动。
            // 攻击请求是否要允许由上层AI决定；这里默认也禁掉，避免空中时仍尝试发起地面攻击。
            // 注意：不清零 FaceDirection，空中朝向由 HitReaction 系统在命中时设置并维持。
            var airCombo = self.AirCombo;
            if (airCombo != null && airCombo.Active)
            {
                self.LocomotionIntent.MoveDirection = Vector3.zero;
                self.LocomotionIntent.JumpRequested = false;
                self.DesiredJump = false;
                self.DesiredAttack = false;
                return;
            }

            if (self.HitReaction.IsInHitReaction)
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

