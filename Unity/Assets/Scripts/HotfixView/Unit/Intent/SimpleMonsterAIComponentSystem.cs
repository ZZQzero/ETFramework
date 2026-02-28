using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(SimpleMonsterAIComponent))]
    public static partial class SimpleMonsterAIComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SimpleMonsterAIComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            self.Driver = unit.GetComponent<AIDriverComponent>();
            self.Attack = unit.GetComponent<AttackComponent>();
        }

        [EntitySystem]
        private static void Update(this SimpleMonsterAIComponent self)
        {
            if (self.Driver == null)
            {
                return;
            }

            Unit selfUnit = self.GetParent<Unit>();
            Unit player = UnitHelper.GetMyUnitFromCurrentScene(selfUnit.Scene());
            if (player == null || player.IsDisposed)
            {
                self.Driver.DesiredMoveDirection = Vector3.zero;
                self.Driver.DesiredFaceDirection = Vector3.zero;
                return;
            }

            var selfPos3 = selfUnit.Position;
            var playerPos3 = player.Position;
            Vector3 to = new Vector3(playerPos3.x - selfPos3.x, 0f, playerPos3.z - selfPos3.z);
            float dist = to.magnitude;

            // 超出追击范围：不动
            if (dist > self.ChaseRange)
            {
                self.Driver.DesiredMoveDirection = Vector3.zero;
                self.Driver.DesiredFaceDirection = Vector3.zero;
                return;
            }

            if (dist > 0.001f)
            {
                Vector3 dir = to / dist;
                self.Driver.DesiredFaceDirection = dir;
            }

            // 攻击优先：进入攻击范围且不在攻击流程中则触发一次攻击
            if (dist <= self.AttackRange)
            {
                self.Driver.DesiredMoveDirection = Vector3.zero;
                if (self.Attack != null && !self.Attack.IsInAttack)
                {
                    self.Driver.DesiredAttack = true;
                }
                return;
            }

            // 追击移动
            self.Driver.DesiredMoveDirection = to.sqrMagnitude > 0.001f ? to.normalized : Vector3.zero;
        }
    }
}

