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
            self.InitComponentRefs(unit);
        }

        [EntitySystem]
        private static void Update(this PlayerDriverComponent self)
        {
            if (self.Input == null || self.LocomotionIntent == null || self.AttackCommand == null)
            {
                return;
            }

            // AirCombo（被挂空中）：Driver 不再写入 Move/Face/Jump，避免AI/输入与Motor接管垂直规则产生抖动。
            // AttackCommand 仍允许入队（最终是否执行由 AttackComponent/受击锁裁决）。
            var airCombo = self.AirCombo;
            if (airCombo != null && airCombo.Active)
            {
                self.LocomotionIntent.MoveDirection = Vector3.zero;
                self.LocomotionIntent.FaceDirection = Vector3.zero;
                self.LocomotionIntent.JumpRequested = false;
                // 空中连段中：仍允许输入攻击（用于“挣扎/反打/特定系统”），最终是否执行由受击锁与 AttackComponent 裁决
                if (self.Input.ConsumeAttackRequest())
                {
                    self.AttackCommand.Enqueue(new AttackCommandComponent.AttackCommand
                    {
                        SkillId = 0,
                        InputType = ComboInputType.Normal,
                        TargetUnitId = 0,
                    });
                }
                // 跳跃输入直接吞掉（避免堆积到落地后瞬间触发）
                self.Input.ConsumeJumpRequest();
                return;
            }

            if (!self.Input.EnableInput)
            {
                self.LocomotionIntent.MoveDirection = Vector3.zero;
                return;
            }

            // Move / Face
            Vector3 move = self.Input.GetMoveInput();
            move.y = 0f;
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }
            self.LocomotionIntent.MoveDirection = move;

            // FaceDirection：仅有当前移动输入时写入，无输入时不覆盖（停攻后保持对敌，等有输入再转）
            if (move.sqrMagnitude > 0.01f)
            {
                self.LocomotionIntent.FaceDirection = move.normalized;
            }

            // Jump (无条件写入意图，由 Motor 最终裁决)
            if (self.Input.ConsumeJumpRequest())
            {
                self.LocomotionIntent.JumpRequested = true;
            }

            // Attack (无条件入队命令，由 AttackComponent 最终裁决)
            if (self.Input.ConsumeAttackRequest())
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

