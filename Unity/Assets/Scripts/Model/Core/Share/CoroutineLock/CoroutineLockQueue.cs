using System;
using System.Collections.Generic;

namespace ET
{
    [ChildOf(typeof(CoroutineLockQueueType))]
    public class CoroutineLockQueue: Entity, IAwake<long>, IDestroy
    {
        public long type;

        public bool isStart;
        
        public Queue<WaitCoroutineLock> queue = new();

        public int Count => this.queue.Count;
    }
}