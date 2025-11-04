using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ET
{
    // 一个Fiber一个固定的线程
    internal class ThreadScheduler: IScheduler
    {
        private readonly ConcurrentDictionary<Fiber, Thread> fiberDic = new();
        
        private readonly FiberManager fiberManager;
        private volatile bool stopRequested;

        public ThreadScheduler(FiberManager fiberManager)
        {
            this.fiberManager = fiberManager;
        }

        private void Loop(Fiber fiber)
        {
            SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);
            
            while (!stopRequested)
            {
                if (fiberManager.IsDisposed() || stopRequested)
                {
                    return;
                }
                
                if (fiber.IsDisposed)
                {
                    fiberDic.Remove(fiber, out _);
                    return;
                }
                
                fiber.Update();
                fiber.LateUpdate();

                Thread.Sleep(1);
            }
        }

        public void Dispose()
        {
            stopRequested = true;
            foreach (var kv in this.fiberDic.ToArray())
            {
                kv.Value.Join();
            }
        }

        public void Add(Fiber fiber)
        {
            Thread thread = new(() => this.Loop(fiber));
            this.fiberDic.TryAdd(fiber, thread);
            thread.Start();
        }
    }
}