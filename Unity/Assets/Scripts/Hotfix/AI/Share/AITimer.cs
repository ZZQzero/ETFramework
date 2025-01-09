using System;

namespace ET
{
    [Invoke(TimerInvokeType.AITimer)]
    public class AITimer: ATimer<AIComponent>
    {
        protected override void Run(AIComponent self)
        {
            try
            {
                self.Check();
            }
            catch (Exception e)
            {
                Log.Error($"move timer error: {self.Id}\n{e}");
            }
        }
    }
}