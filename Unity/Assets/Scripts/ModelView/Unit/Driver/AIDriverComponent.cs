using UnityEngine;

namespace ET
{
    /// <summary>
    /// AI 驱动：由 AI/行为树/FSM 写入 Desired* 字段，
    /// 本组件每帧把 Desired* 同步到 Intent，Motor/Combat 再执行。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class AIDriverComponent : Entity, IAwake, IUpdate
    {
        private ComponentRef<MovementContextComponent> movementContextRef;
        private ComponentRef<CombatContextComponent> combatContextRef;
        private ComponentRef<LocomotionIntentComponent> locomotionIntentRef;
        private ComponentRef<AttackCommandComponent> attackCommandRef;
        private ComponentRef<AirComboComponent> airComboRef;

        public void InitComponentRefs(Unit unit)
        {
            this.movementContextRef = new ComponentRef<MovementContextComponent>(unit);
            this.combatContextRef = new ComponentRef<CombatContextComponent>(unit);
            this.locomotionIntentRef = new ComponentRef<LocomotionIntentComponent>(unit);
            this.attackCommandRef = new ComponentRef<AttackCommandComponent>(unit);
            this.airComboRef = new ComponentRef<AirComboComponent>(unit);
        }

        public LocomotionIntentComponent LocomotionIntent => this.movementContextRef.Get()?.LocomotionIntent ?? this.locomotionIntentRef.Get();
        public AttackCommandComponent AttackCommand => this.combatContextRef.Get()?.AttackCommand ?? this.attackCommandRef.Get();
        public AirComboComponent AirCombo => this.combatContextRef.Get()?.AirCombo ?? this.airComboRef.Get();

        /// <summary>
        /// AI 期望移动方向（世界空间 XZ）
        /// </summary>
        public Vector3 DesiredMoveDirection;

        /// <summary>
        /// AI 期望朝向（世界空间 XZ）
        /// </summary>
        public Vector3 DesiredFaceDirection;

        /// <summary>
        /// AI 请求攻击（边沿触发）
        /// </summary>
        public bool DesiredAttack;

        /// <summary>
        /// AI 请求跳跃（边沿触发）
        /// </summary>
        public bool DesiredJump;
    }
}