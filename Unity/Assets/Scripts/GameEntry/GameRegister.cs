using System;
using ET.Client;
#if DOTNET
using ET.Server;
#endif

namespace ET
{
    public static class GameRegister
    {
        public static void RegisterSingleton()
        {
            World.Instance.AddSingleton<CodeTypes>();
            World.Instance.AddSingleton<SceneTypeSingleton, Type>(typeof(SceneType));
            World.Instance.AddSingleton<ObjectPool>();
            World.Instance.AddSingleton<IdGenerater>();
            World.Instance.AddSingleton<OpcodeType>();
            
            World.Instance.AddSingleton<MessageQueue>();
            World.Instance.AddSingleton<NetServices>();
            
            LogMsg logMsg = World.Instance.AddSingleton<LogMsg>();
            logMsg.AddIgnore(LoginOuter.C2G_Ping);
            logMsg.AddIgnore(LoginOuter.G2C_Ping);
            
            ReloadSingleton();
        }

        private static void ReloadSingleton()
        {
            World.Instance.AddSingleton<EventSystem>();
            World.Instance.AddSingleton<EntitySystemSingleton>();
            World.Instance.AddSingleton<MessageSessionDispatcher>();
            World.Instance.AddSingleton<MessageDispatcher>();
            World.Instance.AddSingleton<AIDispatcherComponent>();
            World.Instance.AddSingleton<NumericWatcherComponent>();
#if UNITY
            World.Instance.AddSingleton<UIEventComponent>();
#endif

#if DOTNET
            World.Instance.AddSingleton<ConsoleDispatcher>();
            World.Instance.AddSingleton<HttpDispatcher>();
#endif
        }

        public static void RegisterInvoke()
        {
            //客户端和服务端都需要的
#if SHARE
            EventSystem.Instance.RegisterInvoke<FiberInit_Main>(SceneType.Main);
            EventSystem.Instance.RegisterInvoke<AITimer>(TimerInvokeType.AITimer);
            EventSystem.Instance.RegisterInvoke<MailBoxType_UnOrderedMessageHandler>(MailBoxType.UnOrderedMessage);
            EventSystem.Instance.RegisterInvoke<SessionAcceptTimeoutInvoke>(TimerCoreInvokeType.SessionAcceptTimeout);
            EventSystem.Instance.RegisterInvoke<SessionIdleCheckerInvoke>(TimerCoreInvokeType.SessionIdleChecker);
            EventSystem.Instance.RegisterInvoke<GetAllConfigBytes>();
            EventSystem.Instance.RegisterInvoke<GetOneConfigBytes>();
            EventSystem.Instance.RegisterInvoke<WaitCoroutineLockTimerInvoke>(TimerCoreInvokeType.CoroutineTimeout);
#endif

            //服务端用
#if DOTNET
            EventSystem.Instance.RegisterInvoke<FiberInit_Location>(SceneType.Location);
            EventSystem.Instance.RegisterInvoke<FiberInit_Gate>(SceneType.Gate);
            EventSystem.Instance.RegisterInvoke<FiberInit_Realm>(SceneType.Realm);
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

        public static void RegisterEvent()
        {
#if SHARE
            EventSystem.Instance.RegisterEvent<NumericChangeEvent_NotifyWatcher>(SceneType.All);
            EventSystem.Instance.RegisterEvent<EntryEvent1_InitShare>(SceneType.StateSync);
#endif

#if DOTNET
            EventSystem.Instance.RegisterEvent<EntryEvent2_InitServer>(SceneType.StateSync);
#endif
            
#if UNITY
            EventSystem.Instance.RegisterEvent<EntryEvent3_InitClient>(SceneType.StateSync);
            EventSystem.Instance.RegisterEvent<AfterCreateClientScene_AddComponent>(SceneType.StateSync);
            EventSystem.Instance.RegisterEvent<AfterCreateCurrentScene_AddComponent>(SceneType.Current);
            EventSystem.Instance.RegisterEvent<SceneChangeStart_AddComponent>(SceneType.StateSync);
            EventSystem.Instance.RegisterEvent<SceneChangeFinishEvent_CreateUIHelp>(SceneType.Current);
            EventSystem.Instance.RegisterEvent<LoginFinish_CreateLobbyUI>(SceneType.StateSync);
            EventSystem.Instance.RegisterEvent<LoginFinish_RemoveLoginUI>(SceneType.StateSync);
            EventSystem.Instance.RegisterEvent<AfterUnitCreate_CreateUnitView>(SceneType.Current);
            EventSystem.Instance.RegisterEvent<ChangePosition_SyncGameObjectPos>(SceneType.Current);
            EventSystem.Instance.RegisterEvent<ChangeRotation_SyncGameObjectRotation>(SceneType.Current);
            EventSystem.Instance.RegisterEvent<AppStartInitFinish_CreateLoginUI>(SceneType.StateSync);
#endif
            
        }
    }
}