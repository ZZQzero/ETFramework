using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ET
{
    public class ObjectPool : Singleton<ObjectPool> ,ISingletonAwake
    {
        public void Awake()
        {
            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Fetch<T>(bool isFromPool = true) where T : class, IPool, new()
        {
            if (!isFromPool)
            {
                var fresh = new T();
                fresh.IsFromPool = false;
                return fresh;
            }
            return PoolHolder<T>.Instance.Get();
        } 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Recycle<T>(T obj) where T : class, IPool, new()
        {
            if (obj == null)
            {
                return;
            }
            PoolHolder<T>.Instance.Return(obj);
        }
        
        private static class PoolHolder<T> where T : class, IPool, new()
        {
            // 视负载调整：capacity/fastSlotsCount
            [StaticField]
            public static readonly Pool<T> Instance = new Pool<T>(capacity: 256, fastSlotsCount: 16);
        }
        
        // —— 池实现：FastSlots（无锁） + 环形缓冲区（锁保护） —— //
        private class Pool<T> where T : class, IPool, new()
        {
            private readonly T[] buffer;
            private readonly int capacity;
            private int head; // 写入位置（受锁保护）
            private int tail; // 读取位置（受锁保护）
            private int count; // 当前数量（锁内）
            private readonly object _sync = new object();
            private readonly T[] fastSlots;// 一级缓存（无锁，减少锁竞争）

            public Pool(int capacity, int fastSlotsCount)
            {
                if (capacity <= 0)
                {
                    capacity = 1;
                }

                if (fastSlotsCount <= 0)
                {
                    fastSlotsCount = 1;
                }

                this.capacity = capacity;
                this.buffer = new T[capacity];
                this.fastSlots = new T[fastSlotsCount];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T Get()
            {
                // 1) FastSlots：无锁快路
                for (int i = 0; i < fastSlots.Length; i++)
                {
#if DOTNET
                    var inst = Interlocked.Exchange(ref fastSlots[i], null);
#elif UNITY
                    var inst = fastSlots[i];
                    fastSlots[i] = null;
#endif
                    if (inst != null)
                    {
                        inst.IsFromPool = true;
                        return inst;
                    }
                }

                // 2) 环形缓冲区（锁内保证 head/tail/count 一致性）
                lock (_sync)
                {
                    if (count > 0)
                    {
                        var inst = buffer[tail];
                        buffer[tail] = null;
                        tail = (tail + 1) % capacity;
                        count--;
                        inst.IsFromPool = true;
                        return inst;
                    }
                }

                // 3) 最后：新建
                var created = new T();
                created.IsFromPool = true;
                return created;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Return(T obj)
            {
                // 防御：空或非池对象直接忽略
                if (obj == null || !obj.IsFromPool)
                {
                    return;
                }

                // 约定：Recycle 后引用不可再用，因此先复位标志，再 Reset
                obj.IsFromPool = false;
                obj.Dispose(); // 池中保持干净对象

                // 1) 尝试放入 FastSlots（无锁，低冲突）
                for (int i = 0; i < fastSlots.Length; i++)
                {
#if UNITY
                    if (fastSlots[i] == null)
                    {
                        fastSlots[i] = obj;
                        return;
                    }
#elif DOTNET
                    if (Interlocked.CompareExchange(ref fastSlots[i], obj, null) == null)
                    {
                        return;
                    }
#endif
                }

                // 2) 放入环形缓冲区（锁内维护）
                lock (_sync)
                {
                    if (count < capacity)
                    {
                        buffer[head] = obj;
                        head = (head + 1) % capacity;
                        count++;
                        return;
                    }
                }

                // 3) 超容量丢弃
                obj.Dispose();
            }
        }
    }
}
