namespace ET
{
    /// <summary>
    /// 简易怪物AI（工程级“最小可用”）
    /// - 仅用于验证新架构：AI 输出 → Intent → Motor/Combat → Animator
    /// - 后续可替换为行为树/FSM（只要继续写 AIDriver.Desired* 即可）
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class SimpleMonsterAIComponent : Entity, IAwake, IUpdate
    {
        public AIDriverComponent Driver;
        public AttackComponent Attack;
        public float ChaseRange = 12f;
        public float AttackRange = 2.1f;
    }
}