namespace ET
{
    [EntitySystemOf(typeof(UserSessionComponent))]
    public static partial class UserSessionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this UserSessionComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this UserSessionComponent self)
        {
            self.UserSessions.Clear();
        }

        public static Session Get(this UserSessionComponent self, string username)
        {
            if (self.UserSessions.TryGetValue(username, out var entityRef))
            {
                Session session = entityRef;
                if (session == null || session.IsDisposed)
                {
                    self.Remove(username);
                    Log.Debug($"清理已释放的Realm Session：用户 {username}");
                    return null;
                }
                return session;
            }
            return null;
        }

        public static void Add(this UserSessionComponent self, string username, EntityRef<Session> session)
        {
            self.UserSessions[username] = session;
        }
        
        public static void Remove(this UserSessionComponent self, string username)
        {
            self.UserSessions.Remove(username);
        }
    }
}