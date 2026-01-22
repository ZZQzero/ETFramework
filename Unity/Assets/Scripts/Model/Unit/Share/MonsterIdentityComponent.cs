namespace ET
{
    public class MonsterIdentityComponent :  Entity,IAwake<UnitInfo>,IDestroy
    {
        public MonsterTable MonsterTable { get; set; }
        
        public int MonsterConfigId { get; set; }
        
        public long MonsterId { get; set; }
    }
}