using System;

namespace ET
{
    
    [Invoke(TimerCoreInvokeType.CoroutineTimeout)]
    public class WaitCoroutineLockTimerInvoke: ATimer<WaitCoroutineLock>
    {
        protected override void Run(WaitCoroutineLock waitCoroutineLock)
        {
            if (waitCoroutineLock.IsDisposed())
            {
                return;
            }
            waitCoroutineLock.SetException(new Exception("coroutine is timeout!"));
        }
    }
}