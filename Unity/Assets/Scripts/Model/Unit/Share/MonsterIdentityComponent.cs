namespace ET
{
    public class MonsterIdentityComponent :  Entity,IAwake<MonsterTable>,IDestroy
    {
        public MonsterTable MonsterTable { get; set; }
        
        public int MonsterConfigId { get; set; }
        
        public long MonsterId { get; set; }

        /// <summary>
        /// 是否为 Boss（在 Awake 时从 MonsterTable 缓存）。
        /// 说明：给表现/规则层读取，避免 HotfixView 直接依赖 Luban.Runtime 的 MonsterTable 类型。
        /// </summary>
        public bool IsBoss { get; set; }
    }
}