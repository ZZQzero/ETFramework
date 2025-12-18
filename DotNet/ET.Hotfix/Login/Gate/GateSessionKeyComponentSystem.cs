namespace ET
{
    [FriendOf(typeof(GateSessionKeyComponent))]
    public static partial class GateSessionKeyComponentSystem
    {
        public static void Add(this GateSessionKeyComponent self, long key, long userId)
        {
            self.SessionKey.Add(key, userId);
            self.TimeoutRemoveKey(key).NoContext();
        }

        public static long Get(this GateSessionKeyComponent self, long key)
        {
            self.SessionKey.TryGetValue(key, out var userId);
            return userId;
        }

        public static void Remove(this GateSessionKeyComponent self, long key)
        {
            self.SessionKey.Remove(key);
        }

        private static async ETTask TimeoutRemoveKey(this GateSessionKeyComponent self, long key)
        {
            await self.Root().GetComponent<TimerComponent>().WaitAsync(20000);
            self.SessionKey.Remove(key);
        }
    }
}