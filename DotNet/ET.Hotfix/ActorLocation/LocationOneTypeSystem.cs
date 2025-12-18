using System;

namespace ET
{
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
        /// 批量添加Location
        /// </summary>
        public static async ETTask AddBatch(this LocationOneType self,long lockKey, List<(long key, ActorId instanceId)> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }
    
            long coroutineLockType = (self.Id << 32) | CoroutineLockType.Location;
            using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(coroutineLockType, lockKey))
            {
                foreach (var (key, instanceId) in items)
                {
                    self.locations[key] = instanceId;
                }
            }
        }

        public static async ETTask Remove(this LocationOneType self, long key)
        {
            long coroutineLockType = (self.Id << 32) | CoroutineLockType.Location;
            using (await self.Root().GetComponent<CoroutineLockComponent>().Wait(coroutineLockType, key))
            {
                self.locations.Remove(key);
            }
        }

        public static async ETTask Lock(this LocationOneType self, long key, ActorId actorId, int time = 0)
        {
            long coroutineLockType = (self.Id << 32) | CoroutineLockType.Location;
            CoroutineLock coroutineLock = await self.Root().GetComponent<CoroutineLockComponent>().Wait(coroutineLockType, key);

            LockInfo lockInfo = self.AddChild<LockInfo, ActorId, CoroutineLock>(actorId, coroutineLock);
            self.lockInfos.Add(key, lockInfo);
            
            if (time > 0)
            {
                async ETTask TimeWaitAsync()
                {
                    try
                    {
                        long lockInfoInstanceId = lockInfo.InstanceId;
                        await self.Root().GetComponent<TimerComponent>().WaitAsync(time);
                        if (lockInfo.InstanceId != lockInfoInstanceId)
                        {
                            return; // 已经被正常释放
                        }
                        Log.Warning($"location timeout unlock key: {key} instanceId: {actorId}");
                        self.UnLock(key, actorId, actorId);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"location timeout unlock error: {e}");
                    }
                }
                TimeWaitAsync().NoContext();
            }
        }

        public static void UnLock(this LocationOneType self, long key, ActorId oldActorId, ActorId newInstanceId)
        {
            if (!self.lockInfos.TryGetValue(key, out EntityRef<LockInfo> lockInfoRef))
            {
                Log.Warning($"location unlock not found key: {key} {oldActorId} (可能已超时释放)");
                return;
            }

            LockInfo lockInfo = lockInfoRef;
            if (oldActorId != lockInfo.LockActorId)
            {
                throw new Exception($"location unlock oldInstanceId is different: key={key}, expected={lockInfo.LockActorId}, actual={oldActorId}");
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
}