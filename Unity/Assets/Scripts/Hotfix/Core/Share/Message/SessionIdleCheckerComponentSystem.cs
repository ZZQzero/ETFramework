using System;

namespace ET
{
    [EntitySystemOf(typeof(SessionIdleCheckerComponent))]
    public static partial class SessionIdleCheckerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SessionIdleCheckerComponent self)
        {
            self.RepeatedTimer = self.Root().GetComponent<TimerComponent>().NewRepeatedTimer(CheckInteral, TimerCoreInvokeType.SessionIdleChecker, self);
        }
        
        [EntitySystem]
        private static void Destroy(this SessionIdleCheckerComponent self)
        {
            self.Root().GetComponent<TimerComponent>()?.Remove(ref self.RepeatedTimer);
        }

        private const int CheckInteral = 2000;

#if UNITY_EDITOR && DEBUG
        public const int SessionTimeoutTime = 400000;
#else
        public const int SessionTimeoutTime = 40000;
#endif

        public static void Check(this SessionIdleCheckerComponent self)
        {
            Session session = self.GetParent<Session>();
            long timeNow = TimeInfo.Instance.ClientNow();

            if (timeNow - session.LastRecvTime < SessionTimeoutTime && timeNow - session.LastSendTime < SessionTimeoutTime)
            {
                return;
            }

            Log.Info($"session timeout: {session.Id} {timeNow} {session.LastRecvTime} {session.LastSendTime} {timeNow - session.LastRecvTime} {timeNow - session.LastSendTime}");
            session.Error = ErrorCode.ERR_SessionSendOrRecvTimeout;

            session.Dispose();
        }
    }
}