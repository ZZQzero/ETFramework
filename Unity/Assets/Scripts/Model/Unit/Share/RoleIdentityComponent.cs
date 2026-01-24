namespace ET
{
    /// <summary>
    /// 角色的相关配置数据
    /// </summary>
    public class RoleIdentityComponent : Entity,IAwake<UnitInfo>,IDestroy
    {
        public int RoleConfigId { get; set; }
        public long RoleId { get; set; }
        public long UserId { get; set; }
        public RoleTable RoleTable { get; set; }
        
        public RoleSkillSetTable RoleSkillSetTable { get; set; }
    }
}