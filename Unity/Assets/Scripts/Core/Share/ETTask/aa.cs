/*using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ET
{
    /// <summary>
    /// ETTask对象池
    /// </summary>
    public static class ETTaskPool
    {
        // 配置参数
        private const int MAX_POOL_SIZE = 1000;
        private const int INITIAL_POOL_SIZE = 50;
        
#if DOTNET
        [StaticField]
        private static readonly ThreadLocal<Stack<ETTask>> localStack = new ThreadLocal<Stack<ETTask>>(
            () => new Stack<ETTask>(INITIAL_POOL_SIZE)
        );
#else
        [StaticField]
        private static ETTask[] pool = new ETTask[MAX_POOL_SIZE];
        
        [StaticField]
        private static int poolCount = 0;
#endif

        // 统计信息（始终收集，供编辑器窗口使用）
#if DOTNET
        [StaticField]
        private static readonly ThreadLocal<long> totalAlloc = new ThreadLocal<long>(() => 0);
        [StaticField]
        private static readonly ThreadLocal<long> hitCount = new ThreadLocal<long>(() => 0);
        [StaticField]
        private static readonly ThreadLocal<long> missCount = new ThreadLocal<long>(() => 0);
        [StaticField]
        private static readonly ThreadLocal<long> dupReturnCount = new ThreadLocal<long>(() => 0);
#else
        [StaticField]
        private static long totalAlloc = 0;
        [StaticField]
        private static long hitCount = 0;
        [StaticField]
        private static long missCount = 0;
        [StaticField]
        private static long dupReturnCount = 0;
#endif

        /// <summary>
        /// 预热对象池
        /// </summary>
        public static void Warmup(int count = INITIAL_POOL_SIZE)
        {
            int target = Math.Min(count, MAX_POOL_SIZE);
            
#if DOTNET
            var stack = localStack.Value;
            
            while (stack.Count < target)
            {
                var task = new ETTask(true);
                task.ResetState();
                task.IsPooled = true;
                stack.Push(task);
            }
#else
            // 客户端：预热到目标数量
            while (poolCount < target)
            {
                var task = new ETTask(true);
                task.ResetState();
                task.IsPooled = true;
                pool[poolCount++] = task;
            }
#endif
        }

        /// <summary>
        /// 获取ETTask
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ETTask Rent()
        {
#if DOTNET
            totalAlloc.Value++;
            var stack = localStack.Value;
            
            if (stack.Count > 0)
            {
                ETTask task = stack.Pop();
                task.IsPooled = false;
                hitCount.Value++;
                return task;
            }
            
            missCount.Value++;
#else
            totalAlloc++;
            
            if (poolCount > 0)
            {
                ETTask task = pool[--poolCount];
                pool[poolCount] = null;
                task.IsPooled = false;
                hitCount++;
                return task;
            }
            
            missCount++;
#endif
            
            var newTask = new ETTask(true);
            newTask.IsPooled = false;
            return newTask;
        }

        /// <summary>
        /// 归还ETTask
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(ETTask task)
        {
            if (task.IsPooled)
            {
#if DOTNET
                dupReturnCount.Value++;
#else
                dupReturnCount++;
#endif
                return;
            }
            
#if DOTNET
            var stack = localStack.Value;
            
            if (stack.Count >= MAX_POOL_SIZE)
            {
                return;
            }
            
            task.ResetState();
            task.IsPooled = true;
            stack.Push(task);
#else
            if (poolCount >= MAX_POOL_SIZE)
            {
                return;
            }
            
            task.ResetState();
            task.IsPooled = true;
            pool[poolCount++] = task;
#endif
        }

        /// <summary>
        /// 获取对象池信息（供编辑器窗口使用）
        /// </summary>
        public static PoolInfo GetPoolInfo()
        {
#if DOTNET
            var stack = localStack.Value;
            return new PoolInfo
            {
                PoolSize = stack.Count,
                MaxSize = MAX_POOL_SIZE,
                TotalAllocations = totalAlloc.Value,
                PoolHits = hitCount.Value,
                PoolMisses = missCount.Value,
                DuplicateReturns = dupReturnCount.Value
            };
#else
            return new PoolInfo
            {
                PoolSize = poolCount,
                MaxSize = MAX_POOL_SIZE,
                TotalAllocations = totalAlloc,
                PoolHits = hitCount,
                PoolMisses = missCount,
                DuplicateReturns = dupReturnCount
            };
#endif
        }
        
        /// <summary>
        /// 对象池信息
        /// </summary>
        public struct PoolInfo
        {
            public int PoolSize;
            public int MaxSize;
            public long TotalAllocations;
            public long PoolHits;
            public long PoolMisses;
            public long DuplicateReturns;
            
            public float HitRate => TotalAllocations > 0 
                ? (float)PoolHits / TotalAllocations * 100 
                : 0;
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public static void Clear()
        {
#if DOTNET
            var stack = localStack.Value;
            
#if DEBUG
            // Debug模式：重置IsPooled标记
            while (stack.Count > 0)
            {
                var task = stack.Pop();
                task.IsPooled = false;
            }
#else
            stack.Clear();
#endif
            
            totalAlloc.Value = 0;
            hitCount.Value = 0;
            missCount.Value = 0;
            dupReturnCount.Value = 0;
#else
#if DEBUG
            for (int i = 0; i < poolCount; i++)
            {
                if (pool[i] != null)
                {
                    pool[i].IsPooled = false;
                }
            }
#endif
            
            Array.Clear(pool, 0, poolCount);
            poolCount = 0;
            
            totalAlloc = 0;
            hitCount = 0;
            missCount = 0;
            dupReturnCount = 0;
#endif
        }

#if DEBUG
        /// <summary>
        /// 检查对象池健康状态（仅Debug模式）
        /// 用于发现重复对象、未标记对象等潜在问题
        /// </summary>
        public static void CheckPoolHealth()
        {
#if DOTNET
            var stack = localStack.Value;
#endif
            
            HashSet<ETTask> uniqueTasks = new HashSet<ETTask>();
            int duplicates = 0;
            int unmarkedCount = 0;
            int totalTasks = 0;

#if DOTNET
            foreach (var task in stack)
            {
                totalTasks++;
                
                if (!uniqueTasks.Add(task))
                {
                    duplicates++;
                    Log.Error($"发现重复对象: HashCode={task.GetHashCode()}");
                }

                if (!task.IsPooled)
                {
                    unmarkedCount++;
                    Log.Error($"池中对象未标记IsPooled: HashCode={task.GetHashCode()}");
                }
            }
#else
            for (int i = 0; i < poolCount; i++)
            {
                if (pool[i] == null)
                {
                    Log.Error($"池中索引{i}为null");
                    continue;
                }
                
                totalTasks++;
                var task = pool[i];
                
                if (!uniqueTasks.Add(task))
                {
                    duplicates++;
                    Log.Error($"发现重复对象: HashCode={task.GetHashCode()}");
                }

                if (!task.IsPooled)
                {
                    unmarkedCount++;
                    Log.Error($"池中对象未标记IsPooled: HashCode={task.GetHashCode()}");
                }
            }
#endif

            if (duplicates > 0 || unmarkedCount > 0)
            {
                Log.Error($"池健康检查失败: 总数={totalTasks}, 重复={duplicates}, 未标记={unmarkedCount}");
            }
            else
            {
                Log.Info($"池健康检查通过: {totalTasks}个对象全部正常");
            }
        }
#endif
    }

    /// <summary>
    /// ETTask泛型对象池
    /// </summary>
    public static class ETTaskPool<T>
    {
        private const int MAX_POOL_SIZE = 500;  // 泛型池减半
        private const int INITIAL_POOL_SIZE = 25;

#if DOTNET
        // 服务端：ThreadLocal + Stack（LIFO，缓存局部性更好）
        [StaticField]
        private static readonly ThreadLocal<Stack<ETTask<T>>> localStack = new ThreadLocal<Stack<ETTask<T>>>(
            () => new Stack<ETTask<T>>(INITIAL_POOL_SIZE)
        );
#else
        // 客户端：数组 + Stack语义（LIFO，缓存局部性更好）
        [StaticField]
        private static ETTask<T>[] pool = new ETTask<T>[MAX_POOL_SIZE];
        
        [StaticField]
        private static int poolCount = 0;
#endif

#if ENABLE_PROFILER || DEBUG
#if DOTNET
        [StaticField]
        private static readonly ThreadLocal<long> totalAlloc = new ThreadLocal<long>(() => 0);
        [StaticField]
        private static readonly ThreadLocal<long> hitCount = new ThreadLocal<long>(() => 0);
        [StaticField]
        private static readonly ThreadLocal<long> dupReturnCount = new ThreadLocal<long>(() => 0);
#else
        [StaticField]
        private static long totalAlloc = 0;
        [StaticField]
        private static long hitCount = 0;
        [StaticField]
        private static long dupReturnCount = 0;
#endif
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ETTask<T> Rent()
        {
#if ENABLE_PROFILER || DEBUG
#if DOTNET
            totalAlloc.Value++;
#else
            totalAlloc++;
#endif
#endif

#if DOTNET
            var stack = localStack.Value;
            
            if (stack.Count > 0)
            {
                ETTask<T> task = stack.Pop();
                
#if DEBUG
                if (!task.IsPooled)
                {
                    Log.Error($"ETTaskPool<{typeof(T).Name}>内部错误：池中对象未标记IsPooled");
                }
#endif
                
                task.IsPooled = false;

#if ENABLE_PROFILER || DEBUG
                hitCount.Value++;
#endif
                return task;
            }
#else
            if (poolCount > 0)
            {
                ETTask<T> task = pool[--poolCount];
                pool[poolCount] = null;
                
#if DEBUG
                if (!task.IsPooled)
                {
                    Log.Error($"ETTaskPool<{typeof(T).Name}>内部错误：池中对象未标记IsPooled");
                }
#endif
                
                task.IsPooled = false;

#if ENABLE_PROFILER || DEBUG
                hitCount++;
#endif
                return task;
            }
#endif
            
            var newTask = new ETTask<T>(true);
            newTask.IsPooled = false;
            return newTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(ETTask<T> task)
        {
            if (task.IsPooled)
            {
#if DEBUG
                Log.Warning($"ETTask<{typeof(T).Name}>重复归还被阻止!");
#endif

#if ENABLE_PROFILER || DEBUG
#if DOTNET
                dupReturnCount.Value++;
#else
                dupReturnCount++;
#endif
#endif
                return;
            }
            
#if DOTNET
            var stack = localStack.Value;
            
            if (stack.Count >= MAX_POOL_SIZE)
            {
                return;
            }

            task.ResetState();
            task.IsPooled = true;
            stack.Push(task);
#else
            if (poolCount >= MAX_POOL_SIZE)
            {
                return;
            }

            task.ResetState();
            task.IsPooled = true;
            pool[poolCount++] = task;
#endif
        }

        public static string GetStatistics()
        {
#if DOTNET
            var stack = localStack.Value;
            int size = stack.Count;
#else
            int size = poolCount;
#endif

#if ENABLE_PROFILER || DEBUG
#if DOTNET
            long tot = totalAlloc.Value;
            long hit = hitCount.Value;
            long dup = dupReturnCount.Value;
#else
            long tot = totalAlloc;
            long hit = hitCount;
            long dup = dupReturnCount;
#endif
            
            float hitRate = tot > 0 ? (hit * 100f / tot) : 0f;
            
            return $"ETTaskPool<{typeof(T).Name}>: " +
                   $"Size={size}/{MAX_POOL_SIZE}, Alloc={tot}, HitRate={hitRate:F2}%, DupReturn={dup}";
#else
            return $"ETTaskPool<{typeof(T).Name}>: Size={size}/{MAX_POOL_SIZE}";
#endif
        }
    }
}*/