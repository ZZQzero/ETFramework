using System;

namespace ET
{
    [EntitySystemOf(typeof(SessionAcceptTimeoutComponent))]
    public static partial class SessionAcceptTimeoutComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SessionAcceptTimeoutComponent self)
        {
            self.Timer = self.Root().GetComponent<TimerComponent>().NewOnceTimer(TimeInfo.Instance.ServerNow() + 5000, TimerCoreInvokeType.SessionAcceptTimeout, self);
        }
        
        [EntitySystem]
        private static void Destroy(this SessionAcceptTimeoutComponent self)
        {
            self.Root().GetComponent<TimerComponent>()?.Remove(ref self.Timer);
        }
        
    }
    
    
}