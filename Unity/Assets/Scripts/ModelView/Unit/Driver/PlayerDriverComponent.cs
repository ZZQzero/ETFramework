namespace ET
{
    /// <summary>
    /// 玩家驱动：把 InputComponent（设备输入缓存）翻译成 Intent（动作意图）。
    /// 注意：该组件只做“输入→意图”的映射，不做移动/攻击的执行。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class PlayerDriverComponent : Entity, IAwake, IUpdate
    {
        private ComponentRef<InputComponent> inputRef;
        private ComponentRef<MovementContextComponent> movementContextRef;
        private ComponentRef<CombatContextComponent> combatContextRef;
        private ComponentRef<LocomotionIntentComponent> locomotionIntentRef;
        private ComponentRef<AttackCommandComponent> attackCommandRef;
        private ComponentRef<AirComboComponent> airComboRef;

        public void InitComponentRefs(Unit unit)
        {
            this.inputRef = new ComponentRef<InputComponent>(unit);
            this.movementContextRef = new ComponentRef<MovementContextComponent>(unit);
            this.combatContextRef = new ComponentRef<CombatContextComponent>(unit);
            this.locomotionIntentRef = new ComponentRef<LocomotionIntentComponent>(unit);
            this.attackCommandRef = new ComponentRef<AttackCommandComponent>(unit);
            this.airComboRef = new ComponentRef<AirComboComponent>(unit);
        }

        public InputComponent Input => this.inputRef.Get();
        public LocomotionIntentComponent LocomotionIntent => this.movementContextRef.Get()?.LocomotionIntent ?? this.locomotionIntentRef.Get();
        public AttackCommandComponent AttackCommand => this.combatContextRef.Get()?.AttackCommand ?? this.attackCommandRef.Get();
        public AirComboComponent AirCombo => this.combatContextRef.Get()?.AirCombo ?? this.airComboRef.Get();
    }
}