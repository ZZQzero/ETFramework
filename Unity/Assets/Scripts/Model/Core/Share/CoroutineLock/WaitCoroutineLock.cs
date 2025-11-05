using System;
using System.Threading;

namespace ET
{
    public class WaitCoroutineLock
    {
        public static WaitCoroutineLock Create()
        {
            WaitCoroutineLock waitCoroutineLock = new WaitCoroutineLock();
            waitCoroutineLock.tcs = ETTask<CoroutineLock>.Create(true);
            return waitCoroutineLock;
        }
        
        private ETTask<CoroutineLock> tcs;

        public void SetResult(CoroutineLock coroutineLock)
        {
            // 使用原子交换，只有一个线程能成功获得非 null 的 tcs
            // 防止超时和正常获取锁的并发竞态
            ETTask<CoroutineLock> t = Interlocked.Exchange(ref this.tcs, null);
            if (t == null)
            {
                // tcs 已经被处理（超时或重复调用），静默返回
                return;
            }
            t.SetResult(coroutineLock);
        }

        public void SetException(Exception exception)
        {
            // 使用原子交换，只有一个线程能成功获得非 null 的 tcs
            // 防止超时和正常获取锁的并发竞态
            ETTask<CoroutineLock> t = Interlocked.Exchange(ref this.tcs, null);
            if (t == null)
            {
                // tcs 已经被处理（正常获取或重复调用），静默返回
                return;
            }
            t.SetException(exception);
        }

        public bool IsDisposed()
        {
            // 使用 Volatile 读取，确保内存可见性
            return Volatile.Read(ref this.tcs) == null;
        }

        public async ETTask<CoroutineLock> Wait()
        {
            // 使用 Volatile 读取 tcs 引用，确保内存可见性
            ETTask<CoroutineLock> t = Volatile.Read(ref this.tcs);
            if (t == null)
            {
                throw new NullReferenceException("Wait called on disposed WaitCoroutineLock");
            }
            return await t;
        }
    }
}