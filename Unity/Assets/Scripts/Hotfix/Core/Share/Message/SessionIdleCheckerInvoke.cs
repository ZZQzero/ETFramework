using System;

namespace ET
{
    [Invoke(TimerCoreInvokeType.SessionIdleChecker)]
    public class SessionIdleCheckerInvoke: ATimer<SessionIdleCheckerComponent>
    {
        protected override void Run(SessionIdleCheckerComponent self)
        {
            try
            {
                self.Check();
            }
            catch (Exception e)
            {
                Log.Error($"session idle checker timer error: {self.Id}\n{e}");
            }
        }
    }
}