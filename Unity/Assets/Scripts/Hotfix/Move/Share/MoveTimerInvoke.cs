using System;

namespace ET
{
    
    [Invoke(TimerInvokeType.MoveTimer)]
    public class MoveTimerInvoke: ATimer<MoveComponent>
    {
        protected override void Run(MoveComponent self)
        {
            try
            {
                self.MoveForward(true);
            }
            catch (Exception e)
            {
                Log.Error($"move timer error: {self.Id}\n{e}");
            }
        }
    }
}