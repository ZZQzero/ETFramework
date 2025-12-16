namespace ET
{
    public enum UserSessionState
    {
        Disconnect,
        Gate,
        Game
    }
    
    /// <summary>
    /// Gate服务器的玩家会话实体
    /// </summary>
    [ChildOf(typeof(UserEntityComponent))]
    public sealed class UserEntity : Entity, IAwake<string, long>
    {
        /// <summary>登录账号（关联User.Account，不可变）</summary>
        public string Account { get; set; }
        
        /// <summary>用户ID（关联User.UserId，永久唯一标识）</summary>
        public long UserId { get; set; }
        
        /// <summary>会话状态</summary>
        public UserSessionState State;
        
        /// <summary>当前角色ID（0表示未选择角色）</summary>
        public long CurrentRoleId;
    }
}