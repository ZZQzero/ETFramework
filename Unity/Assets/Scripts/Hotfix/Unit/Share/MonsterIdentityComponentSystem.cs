namespace ET
{
    public static partial class MonsterIdentityComponentSystem
    {
        public static void Awake(this MonsterIdentityComponent self, MonsterTable monsterTable)
        {
            self.MonsterTable = monsterTable;
            self.MonsterConfigId = monsterTable.Id;
            self.MonsterId = self.EntityId;
        }
    }
}