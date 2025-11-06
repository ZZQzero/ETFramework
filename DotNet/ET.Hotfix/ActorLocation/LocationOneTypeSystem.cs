using System;

namespace ET
{
    [EntitySystemOf(typeof(LockInfo))]
    [FriendOf(typeof(LockInfo))]
    public static partial class LockInfoSystem
    {
        [EntitySystem]
        private static void Awake(this LockInfo self, ActorId lockActorId, CoroutineLock coroutineLock)
        {
            self.CoroutineLock = coroutineLock;
            self.LockActorId = lockActorId;
        }
        
        [EntitySystem]
        private static void Destroy(this LockInfo self)
        {
            self.CoroutineLock.Dispose();
            self.LockActorId = default;
        }
    }
    

    [EntitySystemOf(typeof(LocationOneType))]
    [FriendOf(typeof(LocationOneType))]
    [FriendOf(typeof(LockInfo))]
    public static partial class LocationOneTypeSystem
    {
        [EntitySystem]
        private static void Awake(this LocationOneType self)
        {
        }
        
        public static async ETTask Add(this LocationOneType self, long key, ActorId instanceId)
        {
            long coroutineLockType = (self.Id << 32) | CoroutineLockType.Location;
            using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(coroutineLockType, key))
            {
                self.locations[key] = instanceId;
                Log.Info($"location add key: {key} instanceId: {instanceId}");
            }
        }

        /// <summary>
        /// 批量添加Location（用于批量注册优化）
        /// 注意：此方法在批量处理时使用，可以减少CoroutineLock的等待次数
        /// 使用统一的锁key（0）来保护整个批量操作，避免不必要的锁竞争
        /// </summary>
        public static async ETTask AddBatch(this LocationOneType self, List<(long key, ActorId instanceId)> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }
            
            // 对于批量操作，使用统一的锁key（0）来保护整个批量操作
            // 这样可以减少锁竞争，提高性能
            // 注意：批量操作应该是原子的，所以使用统一的锁是合理的
            long coroutineLockType = (self.Id << 32) | CoroutineLockType.Location;
            long lockKey = 0; // 使用统一的锁key，避免不必要的锁竞争
            
            using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(coroutineLockType, lockKey))
            {
                foreach (var (key, instanceId) in items)
                {
                    self.locations[key] = instanceId;
                    Log.Info($"location batch add key: {key} instanceId: {instanceId}");
                }
            }
        }

        public static async ETTask Remove(this LocationOneType self, long key)
        {
            long coroutineLockType = (self.Id << 32) | CoroutineLockType.Location;
            using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(coroutineLockType, key))
            {
                self.locations.Remove(key);
                Log.Info($"location remove key: {key}");
            }
        }

        public static async ETTask Lock(this LocationOneType self, long key, ActorId actorId, int time = 0)
        {
            long coroutineLockType = (self.Id << 32) | CoroutineLockType.Location;
            CoroutineLock coroutineLock = await self.Root().GetComponent<CoroutineLockComponent>().Wait(coroutineLockType, key);

            LockInfo lockInfo = self.AddChild<LockInfo, ActorId, CoroutineLock>(actorId, coroutineLock);
            self.lockInfos.Add(key, lockInfo);

            Log.Info($"location lock key: {key} instanceId: {actorId}");

            if (time > 0)
            {
                async ETTask TimeWaitAsync()
                {
                    long lockInfoInstanceId = lockInfo.InstanceId;
                    await self.Root().GetComponent<TimerComponent>().WaitAsync(time);
                    if (lockInfo.InstanceId != lockInfoInstanceId)
                    {
                        return;
                    }
                    Log.Info($"location timeout unlock key: {key} instanceId: {actorId} newInstanceId: {actorId}");
                    self.UnLock(key, actorId, actorId);
                }
                TimeWaitAsync().NoContext();
            }
        }

        public static void UnLock(this LocationOneType self, long key, ActorId oldActorId, ActorId newInstanceId)
        {
            if (!self.lockInfos.TryGetValue(key, out EntityRef<LockInfo> lockInfoRef))
            {
                Log.Error($"location unlock not found key: {key} {oldActorId}");
                return;
            }

            LockInfo lockInfo = lockInfoRef;
            if (oldActorId != lockInfo.LockActorId)
            {
                Log.Error($"location unlock oldInstanceId is different: {key} {oldActorId}");
                return;
            }

            Log.Info($"location unlock key: {key} instanceId: {oldActorId} newInstanceId: {newInstanceId}");

            self.locations[key] = newInstanceId;

            self.lockInfos.Remove(key);

            // 解锁
            lockInfo.Dispose();
        }

        public static async ETTask<ActorId> Get(this LocationOneType self, long key)
        {
            long coroutineLockType = (self.Id << 32) | CoroutineLockType.Location;
            using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(coroutineLockType, key))
            {
                self.locations.TryGetValue(key, out ActorId actorId);
                Log.Info($"location get key: {key} actorId: {actorId}");
                return actorId;
            }
        }
    }

    [EntitySystemOf(typeof(LocationManagerComoponent))]
    [FriendOf(typeof(LocationManagerComoponent))]
    public static partial class LocationComoponentSystem
    {
        [EntitySystem]
        private static void Awake(this LocationManagerComoponent self)
        {
        }
        
        public static LocationOneType Get(this LocationManagerComoponent self, int locationType)
        {
            LocationOneType locationOneType = self.GetChild<LocationOneType>(locationType);
            if (locationOneType != null)
            {
                return locationOneType;
            }
            locationOneType = self.AddChildWithId<LocationOneType>(locationType);
            return locationOneType;
        }

    }
}