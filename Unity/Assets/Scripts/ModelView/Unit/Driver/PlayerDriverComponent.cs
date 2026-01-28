namespace ET
{
    /// <summary>
    /// 玩家驱动：把 InputComponent（设备输入缓存）翻译成 Intent（动作意图）。
    /// 注意：该组件只做“输入→意图”的映射，不做移动/攻击的执行。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class PlayerDriverComponent : Entity, IAwake, IUpdate
    {
        public InputComponent Input;
        public LocomotionIntentComponent LocomotionIntent;
        public AttackCommandComponent AttackCommand;
    }
}