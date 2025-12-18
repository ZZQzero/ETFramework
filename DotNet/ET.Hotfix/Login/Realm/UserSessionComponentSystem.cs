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

        public static Session Get(this UserSessionComponent self, long userId)
        {
            if (self.UserSessions.TryGetValue(userId, out var entityRef))
            {
                Session session = entityRef;
                if (session == null || session.IsDisposed)
                {
                    self.Remove(userId);
                    Log.Debug($"清理已释放的Realm Session：用户 {userId}");
                    return null;
                }
                return session;
            }
            return null;
        }

        public static void Add(this UserSessionComponent self, long userId, EntityRef<Session> session)
        {
            self.UserSessions[userId] = session;
        }
        
        public static void Remove(this UserSessionComponent self, long userId)
        {
            self.UserSessions.Remove(userId);
        }
    }
}