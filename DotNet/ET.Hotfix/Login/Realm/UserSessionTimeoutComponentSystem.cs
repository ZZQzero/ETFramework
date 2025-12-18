namespace ET
{
    [EntitySystemOf(typeof(UserSessionTimeoutComponent))]
    public static partial class UserSessionTimeoutComponentSystem
    {
        [EntitySystem]
        private static void Awake(this UserSessionTimeoutComponent self, long userId)
        {
            self.UserId = userId;
            CheckOutTime(self).NoContext();
        }

        [EntitySystem]
        private static void Destroy(this UserSessionTimeoutComponent self)
        {
            self.Root().GetComponent<TimerComponent>()?.Remove(ref self.Timer);
        }

        private static async ETTask CheckOutTime(UserSessionTimeoutComponent self)
        {
            TimerComponent timerComponent = self.Root().GetComponent<TimerComponent>();
            self.Timer = timerComponent.NewOnceTimer(TimeInfo.Instance.ServerNow() + 600000, TimerInvokeType.UserSessionTimeout, self);
            await ETTask.CompletedTask;
        }

        [Invoke(TimerInvokeType.UserSessionTimeout)]
        public class UserSessionTimeout : ATimer<UserSessionTimeoutComponent>
        {
            protected override void Run(UserSessionTimeoutComponent self)
            {
                var userSessionComponent = self.Root().GetComponent<UserSessionComponent>();
                userSessionComponent.Remove(self.UserId);
                
                Session session = self.GetParent<Session>();
                session?.Disconnect().NoContext();
            }
        }
    }
}