namespace ET
{
    /// <summary>
    /// 用户会话超时检测组件
    /// </summary>
    [ComponentOf(typeof(Session))]
    public class UserSessionTimeoutComponent : Entity, IAwake<string>, IDestroy
    {
        public long Timer;
        
        public string Username;
    }
}