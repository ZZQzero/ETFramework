using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    internal class MainThreadScheduler: IScheduler
    {
        private readonly ConcurrentQueue<Fiber> fiberQueue = new();
        private readonly ConcurrentQueue<Fiber> addfiberQueue = new();
        
        private readonly FiberManager fiberManager;
        private readonly ThreadSynchronizationContext mainThreadSynchronizationContext = new();
        private Fiber firstFiber;

        public MainThreadScheduler(FiberManager fiberManager)
        {
            SynchronizationContext.SetSynchronizationContext(this.mainThreadSynchronizationContext);
            this.fiberManager = fiberManager;
        }

        public void Dispose()
        {
            foreach (var fiber in fiberQueue)
            {
                if (fiber == null || fiber.IsDisposed)
                {
                    continue;
                }
                fiber.Dispose();
            }

            foreach (var fiber in addfiberQueue)
            {
                if (fiber == null || fiber.IsDisposed)
                {
                    continue;
                }
                fiber.Dispose();
            }
            fiberQueue.Clear();
            addfiberQueue.Clear();
        }

        public void Update()
        {
            SynchronizationContext.SetSynchronizationContext(this.mainThreadSynchronizationContext);
            mainThreadSynchronizationContext.Update();

            int count = fiberQueue.Count;
            while (count-- > 0)
            {
                if (!fiberQueue.TryDequeue(out var fiber))
                {
                    continue;
                }

                if (fiber == null)
                {
                    continue;
                }
                SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                fiber.Update();
                fiberQueue.Enqueue(fiber);
            }
            SynchronizationContext.SetSynchronizationContext(mainThreadSynchronizationContext);
        }

        public void LateUpdate()
        {
            int count = fiberQueue.Count;
            while (count-- > 0)
            {
                if (!fiberQueue.TryDequeue(out var fiber))
                {
                    continue;
                }

                if (fiber == null)
                {
                    continue;
                }

                if (fiber.IsDisposed)
                {
                    continue;
                }
                SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
                fiber.LateUpdate();
                fiberQueue.Enqueue(fiber);
            }
            
            while (this.addfiberQueue.Count > 0)
            {
                this.addfiberQueue.TryDequeue(out Fiber result);
                this.fiberQueue.Enqueue(result);
            }
            // Fiber调度完成，要还原成默认的上下文，否则unity的回调会找不到正确的上下文
            SynchronizationContext.SetSynchronizationContext(this.mainThreadSynchronizationContext);
        }
        
        public void Add(Fiber fiber)
        {
            addfiberQueue.Enqueue(fiber);
        }
    }
}