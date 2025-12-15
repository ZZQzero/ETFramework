using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// Realm服务器的用户会话管理组件
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class UserSessionComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<string, EntityRef<Session>> UserSessions = new Dictionary<string, EntityRef<Session>>();
    }
}