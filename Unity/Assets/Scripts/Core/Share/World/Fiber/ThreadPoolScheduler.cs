using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    internal class ThreadPoolScheduler : IScheduler
    {
        private readonly List<Thread> threads;
        private readonly ConcurrentQueue<Fiber> fiberQueue = new();
        private readonly FiberManager fiberManager;
        private volatile bool disposed;
        private volatile bool stopRequested;

        public ThreadPoolScheduler(FiberManager fiberManager)
        {
            this.fiberManager = fiberManager;
            int threadCount = Environment.ProcessorCount;
            this.threads = new List<Thread>(threadCount);
            
            for (int i = 0; i < threadCount; ++i)
            {
                Thread thread = new Thread(Loop)
                {
                    IsBackground = true,
                    Name = $"ThreadPoolScheduler-{i}"
                };
                this.threads.Add(thread);
                thread.Start();
            }
        }

        private void Loop()
        {
            int count = 0;
            while (!stopRequested)
            {
                if (count <= 0)
                {
                    Thread.Sleep(1);
                    // count最小为1
                    count = this.fiberQueue.Count / this.threads.Count + 1;
                }

                --count;
                if (fiberManager.IsDisposed() || stopRequested)
                {
                    return;
                }
                
                if (!fiberQueue.TryDequeue(out Fiber fiber))
                {
                    Thread.Sleep(1);
                    continue;
                }
                if(fiber == null || fiber.IsDisposed)
                {
                    continue;
                }
                var prevCtx = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                fiber.Update();
                fiber.LateUpdate();
                SynchronizationContext.SetSynchronizationContext(prevCtx);
                fiberQueue.Enqueue(fiber);
            }
        }

        public void Add(Fiber fiber)
        {
            fiberQueue.Enqueue(fiber);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            stopRequested = true;
            foreach (Thread thread in this.threads)
            {
                thread.Join();
            }
        }
    }
}
