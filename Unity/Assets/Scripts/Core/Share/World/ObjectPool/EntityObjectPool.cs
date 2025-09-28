using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ET
{
    public sealed class EntityObjectPool
    {
        // 默认每个类型最大缓存数，避免池无限增长。按需调整或暴露为配置。
        private const int DefaultMaxPerType = 256;
#if DOTNET
        private static readonly Lazy<EntityObjectPool> Lazy = new Lazy<EntityObjectPool>(() => new EntityObjectPool());
        public static EntityObjectPool Instance => Lazy.Value;
        private readonly ConcurrentDictionary<long,PoolBucket> _pool = new();
#elif UNITY
        private static EntityObjectPool _instance;
        public static EntityObjectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EntityObjectPool();
                }
                return _instance;
            }
        }
        private readonly Dictionary<long,PoolBucket> _pool = new();
        private readonly object _globalLock = new object();
#endif
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetEntity<T>(long typeId,bool isFromPool,int maxPerType = DefaultMaxPerType) where T : Entity,new()
        {
            if (isFromPool)
            {
                PoolBucket bucket = GetOrCreateBucket(typeId, maxPerType);
                if (bucket.TryPop(out Entity entity))
                {
                    entity.IsFromPool = true;
                    return (T)entity;
                }
            }
            
            return CreateEntity<T>(isFromPool);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecycleEntity(Entity entity,int maxPerType = DefaultMaxPerType)
        {
            long typeId = entity.TypeId;
            if (entity.IsFromPool)
            {
                PoolBucket bucket = GetOrCreateBucket(typeId, maxPerType);
                var pushed = bucket.TryPush(entity);
                if (!pushed)
                {
                    // 池已满：释放或忽略（建议调用 Dispose 以释放非托管资源）
                    entity.Dispose();
                }
            }
            else
            {
                entity.Dispose();
            }
        }
        
        /// <summary>
        /// 显式清空某个 typeId 的池（例如类型很少使用时释放内存）
        /// </summary>
        public void ClearPool(long typeId)
        {
#if DOTNET
            if (_pool.TryRemove(typeId, out var bucket))
            {
                // 弹出所有并 Dispose
                while (bucket.TryPop(out var e))
                {
                    e.Dispose();
                }
            }
#elif UNITY
            lock (_globalLock)
            {
                if (_pool.Remove(typeId, out var bucket))
                {
                    while (bucket.TryPop(out var e))
                    {
                        e.Dispose();
                    }
                }
            }
#endif
        }
        
        /// <summary>
        /// 获取指定 typeId 池当前的近似缓存大小（便于监控/调试）
        /// </summary>
        public int GetPoolCount(long typeId)
        {
#if DOTNET
            if (_pool.TryGetValue(typeId, out var b))
            {
                return b.SnapshotCount();
            }
            return 0;
#elif UNITY
            lock (_globalLock)
            {
                if (_pool.TryGetValue(typeId, out var b))
                {
                    return b.SnapshotCount();
                }
                return 0;
            }
#endif
        }

        private T CreateEntity<T>(bool isFormPool) where T : Entity,new()
        {
            T component = new T();
            component.TypeId = TypeId<T>.Id;
            component.IsFromPool = isFormPool;
            component.IsNew = true;
            component.Id = 0;
            return component; 
        }

        private class PoolBucket
        {
#if DOTNET
            public readonly ConcurrentStack<Entity> Stack = new ConcurrentStack<Entity>();
#elif UNITY
            public readonly Stack<Entity> Stack = new Stack<Entity>();
            public readonly object SyncRoot = new object();
#endif
            public readonly int MaxCapacity;
            // 维护一个原子计数来避免每次访问 Count（Count可能是O(n)）
            public int Count;

            public PoolBucket(int maxCapacity)
            {
                this.MaxCapacity = maxCapacity;
                this.Count = 0;
            }

            public bool TryPop(out Entity entity)
            {
#if DOTNET
                if (Stack.TryPop(out entity))
                {
                    Interlocked.Decrement(ref Count);
                    return true;
                }

                entity = null;
                return false;
#elif UNITY
                lock (SyncRoot)
                {
                    if (Stack.Count > 0)
                    {
                        entity = Stack.Pop();
                        Count--;
                        return true;
                    }
                }

                entity = null;
                return false;
#endif
            }

            public bool TryPush(Entity entity)
            {
#if DOTNET
                // 先检查计数避免不必要 Push
                int cur = Interlocked.Increment(ref Count);
                if (cur <= MaxCapacity)
                {
                    entity.Dispose();
                    Stack.Push(entity);
                    return true;
                }

                // 超出容量，回退计数并拒绝入池
                Interlocked.Decrement(ref Count);
                return false;
#elif UNITY
                lock (SyncRoot)
                {
                    if (Count < MaxCapacity)
                    {
                        entity.Dispose();
                        Stack.Push(entity);
                        Count++;
                        return true;
                    }
                }

                return false;
#endif
            }

            public int SnapshotCount()
            {
#if DOTNET
                return Volatile.Read(ref Count);
#elif UNITY
                lock (SyncRoot)
                {
                    return Count;
                }
#endif
            }
        }
        
        private PoolBucket GetOrCreateBucket(long typeId, int maxPerType)
        {
#if DOTNET
            return _pool.GetOrAdd(typeId, _ => new PoolBucket(maxPerType));
#elif UNITY
            lock (_globalLock)
            {
                if (!_pool.TryGetValue(typeId, out var bucket))
                {
                    bucket = new PoolBucket(maxPerType);
                    _pool.Add(typeId, bucket);
                }
                return bucket;
            }
#endif
        }

    }
}