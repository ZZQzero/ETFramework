using System;
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
        private const int MAX_POOL_SIZE = 100;
        private const int INITIAL_POOL_SIZE = 10;
        
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
        
        // 统计信息
#if DOTNET
        private static readonly ThreadLocal<long> totalAlloc = new ThreadLocal<long>(() => 0);
        private static readonly ThreadLocal<long> hitCount = new ThreadLocal<long>(() => 0);
        private static readonly ThreadLocal<long> missCount = new ThreadLocal<long>(() => 0);
        private static readonly ThreadLocal<long> dupReturnCount = new ThreadLocal<long>(() => 0);
#else
        private static long totalAlloc = 0;
        private static long hitCount = 0;
        private static long missCount = 0;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ETTask Rent()
        {
            ETTask task;
#if DOTNET
            totalAlloc.Value++;
            var stack = localStack.Value;
            if (stack.Count > 0)
            {
                task = stack.Pop();
                task.IsPooled = false;
                hitCount.Value++;
            }
            else
            {
                task = new ETTask(true);
                task.IsPooled = false;
                missCount.Value++;
            }
#else
            totalAlloc++;
            if (poolCount > 0)
            {
                task = pool[--poolCount];
                pool[poolCount] = null;
                task.IsPooled = false;
                hitCount++;
            }
            else
            {
                task = new ETTask(true);
                task.IsPooled = false;
                missCount++;
            }
#endif
            return task;
        }

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
        /// 清空对象池
        /// </summary>
        public static void Clear()
        {
#if DOTNET
            var stack = localStack.Value;
            stack.Clear();
            totalAlloc.Value = 0;
            hitCount.Value = 0;
            missCount.Value = 0;
            dupReturnCount.Value = 0;
#else
            Array.Clear(pool, 0, poolCount);
            poolCount = 0;
            totalAlloc = 0;
            hitCount = 0;
            missCount = 0;
            dupReturnCount = 0;
#endif
        }

#if ENABLE_VIEW
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
        
        /// </summary>
        /// 获取对象池信息（供编辑器窗口使用）
        /// </summary>
        public static ETTaskPoolInfo GetPoolInfo()
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
            return new ETTaskPoolInfo
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
#endif
    }

    /// <summary>
    /// ETTask泛型对象池
    /// </summary>
    public static class ETTaskPool<T>
    {
        private const int MAX_POOL_SIZE = 100;
        private const int INITIAL_POOL_SIZE = 10;

#if DOTNET
        [StaticField]
        private static readonly ThreadLocal<Stack<ETTask<T>>> localStack = new ThreadLocal<Stack<ETTask<T>>>(
            () => new Stack<ETTask<T>>(INITIAL_POOL_SIZE)
        );
#else
        [StaticField]
        private static ETTask<T>[] pool = new ETTask<T>[MAX_POOL_SIZE];
        
        [StaticField]
        private static int poolCount = 0;
#endif
        
        // 统计信息
#if DOTNET
        private static readonly ThreadLocal<long> totalAlloc = new ThreadLocal<long>(() => 0);
        private static readonly ThreadLocal<long> hitCount = new ThreadLocal<long>(() => 0);
        private static readonly ThreadLocal<long> missCount = new ThreadLocal<long>(() => 0);
        private static readonly ThreadLocal<long> dupReturnCount = new ThreadLocal<long>(() => 0);
#else
        private static long totalAlloc = 0;
        private static long hitCount = 0;
        private static long missCount = 0;
        private static long dupReturnCount = 0;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ETTask<T> Rent()
        {
#if DOTNET
            totalAlloc.Value++;
            var stack = localStack.Value;
            if (stack.Count > 0)
            {
                ETTask<T> task = stack.Pop();
                task.IsPooled = false;
                hitCount.Value++;
                return task;
            }
            missCount.Value++;
#else
            totalAlloc++;
            if (poolCount > 0)
            {
                ETTask<T> task = pool[--poolCount];
                pool[poolCount] = null;
                task.IsPooled = false;
                hitCount++;
                return task;
            }
            missCount++;
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
                return;
            }
            
#if DOTNET
            dupReturnCount.Value++;
            var stack = localStack.Value;
            if (stack.Count >= MAX_POOL_SIZE)
            {
                return;
            }
            task.ResetState();
            task.IsPooled = true;
            stack.Push(task);
#else
            dupReturnCount++;
            if (poolCount >= MAX_POOL_SIZE)
            {
                return;
            }
            task.ResetState();
            task.IsPooled = true;
            pool[poolCount++] = task;
#endif
        }
    }
    
    /// <summary>
    /// 对象池信息
    /// </summary>

    public struct ETTaskPoolInfo
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
}



