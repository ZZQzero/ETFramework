using System;

namespace ET
{
    [Invoke(TimerCoreInvokeType.SessionAcceptTimeout)]
    public class SessionAcceptTimeoutInvoke: ATimer<SessionAcceptTimeoutComponent>
    {
        protected override void Run(SessionAcceptTimeoutComponent self)
        {
            try
            {
                self.Parent.Dispose();
            }
            catch (Exception e)
            {
                Log.Error($"move timer error: {self.Id}\n{e}");
            }
        }
    }
}