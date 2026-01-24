namespace ET
{
    public static partial class RoleIdentityComponentSystem
    {
        [EntitySystem]
        public static void Awake(this RoleIdentityComponent self,UnitInfo unitInfo)
        {
            self.RoleConfigId = unitInfo.RoleConfigId;
            self.RoleId = unitInfo.RoleId;
            self.UserId = unitInfo.EntityId;
            self.RoleTable = RoleConfig.Instance.Get(unitInfo.RoleConfigId);
            self.RoleSkillSetTable = RoleSkillSetConfig.Instance.Get(self.RoleTable.SkillSetId);
        }

        [EntitySystem]
        public static void Destroy(this RoleIdentityComponent self)
        {
            self.RoleConfigId = 0;
            self.RoleId = 0;
            self.RoleTable = null;
            self.UserId = 0;
        }
    }
}