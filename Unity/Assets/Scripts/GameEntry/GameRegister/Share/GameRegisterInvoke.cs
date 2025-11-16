
namespace ET
{
    public static partial class GameRegister
    {
        public static void RegisterInvoke()
        {
            //客户端和服务端都需要的
#if SHARE
            EventSystem.Instance.RegisterInvoke<FiberInit_Main>(SceneType.Main);
            EventSystem.Instance.RegisterInvoke<AITimer>(TimerInvokeType.AITimer);
            EventSystem.Instance.RegisterInvoke<MailBoxType_UnOrderedMessageHandler>(MailBoxType.UnOrderedMessage);
            EventSystem.Instance.RegisterInvoke<SessionAcceptTimeoutInvoke>(TimerCoreInvokeType.SessionAcceptTimeout);
            EventSystem.Instance.RegisterInvoke<SessionIdleCheckerInvoke>(TimerCoreInvokeType.SessionIdleChecker);
            //EventSystem.Instance.RegisterInvoke<GetAllConfigBytes>();
            //EventSystem.Instance.RegisterInvoke<GetOneConfigBytes>();
            EventSystem.Instance.RegisterInvoke<WaitCoroutineLockTimerInvoke>(TimerCoreInvokeType.CoroutineTimeout);
#endif

            //服务端用
#if DOTNET
            EventSystem.Instance.RegisterInvoke<FiberInit_Location>(SceneType.Location);
            EventSystem.Instance.RegisterInvoke<FiberInit_Gate>(SceneType.Gate);
            EventSystem.Instance.RegisterInvoke<FiberInit_Realm>(SceneType.Realm);
            EventSystem.Instance.RegisterInvoke<FiberInit_LoginCenter>(SceneType.LoginCenter);
            EventSystem.Instance.RegisterInvoke<FiberInit_NetInner>(SceneType.NetInner);
            EventSystem.Instance.RegisterInvoke<FiberInit_Router>(SceneType.Router);
            EventSystem.Instance.RegisterInvoke<FiberInit_RouterManager>(SceneType.RouterManager);
            EventSystem.Instance.RegisterInvoke<FiberInit_Map>(SceneType.Map);
            EventSystem.Instance.RegisterInvoke<FiberInit_Robot>(SceneType.Robot);
            EventSystem.Instance.RegisterInvoke<LogInvoker_NLog>();
            EventSystem.Instance.RegisterInvoke<NetComponentOnReadInvoker_Realm>(SceneType.Realm);
            EventSystem.Instance.RegisterInvoke<MailBoxType_OrderedMessageHandler>(MailBoxType.OrderedMessage);
            EventSystem.Instance.RegisterInvoke<MessageLocationSenderComponentSystem.MessageLocationSenderChecker>(TimerInvokeType.MessageLocationSenderChecker);
            EventSystem.Instance.RegisterInvoke<MailBoxType_GateSessionHandler>(MailBoxType.GateSession);
            EventSystem.Instance.RegisterInvoke<MoveComponentSystem.MoveTimer>(TimerInvokeType.MoveTimer);
            EventSystem.Instance.RegisterInvoke<NetComponentOnReadInvoker_Gate>(SceneType.Gate);
            EventSystem.Instance.RegisterInvoke<RecastFileReader>();
#endif

            //客户端用
#if UNITY
            EventSystem.Instance.RegisterInvoke<UnityLogInvoke>();
            EventSystem.Instance.RegisterInvoke<FiberInit_NetClient>(SceneType.NetClient);
            EventSystem.Instance.RegisterInvoke<NetComponentOnReadInvoker_NetClient>(SceneType.NetClient);
            EventSystem.Instance.RegisterInvoke<MoveTimerInvoke>(TimerInvokeType.MoveTimer);
#endif
        }

    }
}