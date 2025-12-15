namespace ET
{
    /// <summary>
    /// 用户信息组件（Main Fiber全局组件）
    /// 存储当前登录用户的基本信息，供全局访问
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class UserComponent: Entity, IAwake
    {
        /// <summary>登录账号</summary>
        public string Account { get; set; }
        
        /// <summary>用户ID（永久唯一标识）</summary>
        public long UserId { get; set; }
        
        /// <summary>用户昵称（可修改）</summary>
        public string Username { get; set; }
        
        /// <summary>VIP等级</summary>
        public int VipLevel { get; set; }
        
        /// <summary>累计充值</summary>
        public long TotalRecharge { get; set; }
        
        /// <summary>角色ID列表</summary>
        public System.Collections.Generic.List<long> RoleIds { get; set; }
        
        /// <summary>当前选择的角色ID</summary>
        public long CurrentRoleId { get; set; }
    }
}