using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    public class ThreadSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly ConcurrentQueue<Action> queue = new();
#if DOTNET
        private readonly ThreadLocal<List<Action>> localDequeueList = new(() => new List<Action>(128));
#elif UNITY
        private readonly List<Action> localDequeueList = new List<Action>(128);
#endif
        // 0 = running, 1 = disposed
        private int disposedFlag = 0;
        private int queueCount = 0;

        public event Action<Exception> OnUnhandledException;

        public int QueueCount => Volatile.Read(ref queueCount);

        private const int BatchSize = 100;

        public void Update()
        {
            if (Volatile.Read(ref disposedFlag) == 1) return;
#if DOTNET
            var localList = localDequeueList.Value;
#elif UNITY
            var localList = localDequeueList;
#endif
            localList.Clear();
            
            // 批量出队
            int taken = 0;
            while (taken < BatchSize && queue.TryDequeue(out var action))
            {
                localList.Add(action);
                taken++;
            }

            foreach (var act in localList)
            {
                try
                {
                    act();
                }
                catch (Exception e)
                {
                    HandleException(e);
                }
                finally
                {
                    Interlocked.Decrement(ref queueCount);
                }
            }
        }

        public override void Post(SendOrPostCallback callback, object state)
        {
            Post(() => callback(state));
        }

        public void Post(Action action)
        {
            // 快速检查 disposed
            if (Volatile.Read(ref disposedFlag) == 1) return;

            // 增加计数后入队：尽量避免出现负数情况（多个方案可选）
            Interlocked.Increment(ref queueCount);
            // 可能存在微小的竞态：若已 disposed，你可以选择 decrement 并丢弃
            if (Volatile.Read(ref disposedFlag) == 1)
            {
                Interlocked.Decrement(ref queueCount);
                return;
            }
            queue.Enqueue(action);
        }

        private void HandleException(Exception e)
        {
            try
            {
                Log.Error(e);
                OnUnhandledException?.Invoke(e);
            }
            catch
            {
                // 防止异常处理再抛出导致崩溃
            }
        }

        public void Dispose()
        {
            // 原子标记已 disposed，防止重复 Dispose
            if (Interlocked.Exchange(ref disposedFlag, 1) == 1) return;

            // 清空队列并调整计数
            while (queue.TryDequeue(out _))
            {
                Interlocked.Decrement(ref queueCount);
            }
            Interlocked.Exchange(ref queueCount, 0);
#if DOTNET
            localDequeueList.Dispose();
#elif UNITY
            localDequeueList.Clear();
#endif
        }
    }
}
