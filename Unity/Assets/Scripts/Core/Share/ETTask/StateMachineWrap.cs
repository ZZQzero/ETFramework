using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ET
{
    public interface IStateMachineWrap
    {
        Action MoveNext { get; }
        void Recycle();
    }
    
    public class StateMachineWrap<T>: IStateMachineWrap where T: IAsyncStateMachine
    {
        [StaticField]
        private static readonly ConcurrentQueue<StateMachineWrap<T>> queue = new();
        private static int poolSize;
        public static StateMachineWrap<T> Fetch(ref T stateMachine)
        {
            if (queue.TryDequeue(out var wrap))
            {
                Interlocked.Decrement(ref poolSize);
            }
            else
            {
                wrap = new StateMachineWrap<T>();
            }
            wrap.StateMachine = stateMachine;
            return wrap;
        }
        
        public void Recycle()
        {
            // 使用 Volatile.Read 读取当前值，避免不必要的 CompareExchange
            if (Volatile.Read(ref poolSize) >= 100)
            {
                return;
            }
            this.StateMachine = default;
            queue.Enqueue(this);
            Interlocked.Increment(ref poolSize);
        }

        private readonly Action moveNext;

        public Action MoveNext => this.moveNext;

        private T StateMachine;

        private StateMachineWrap()
        {
            this.moveNext = this.Run;
        }

        private void Run()
        {
            this.StateMachine.MoveNext();
        }
    }
}