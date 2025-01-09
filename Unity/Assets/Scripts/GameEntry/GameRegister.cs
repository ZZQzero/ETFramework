using ET.Client;

namespace ET
{
    public static class GameRegister
    {
        public static void RegisterSingleton()
        {
            World.Instance.AddSingleton<EventSystem>();
            World.Instance.AddSingleton<EntitySystemSingleton>();
            World.Instance.AddSingleton<UIEventComponent>();
            World.Instance.AddSingleton<MessageSessionDispatcher>();
            World.Instance.AddSingleton<MessageDispatcher>();
            World.Instance.AddSingleton<AIDispatcherComponent>();
            World.Instance.AddSingleton<NumericWatcherComponent>();
        }

        public static void RegisterInvoke()
        {
            EventSystem.Instance.RegisterInvoke<UnityLogInvoke>();
            EventSystem.Instance.RegisterInvoke<FiberInit_Main>(SceneType.Main);
            EventSystem.Instance.RegisterInvoke<AITimer>(TimerInvokeType.AITimer);
            EventSystem.Instance.RegisterInvoke<MailBoxType_UnOrderedMessageHandler>(MailBoxType.UnOrderedMessage);
            EventSystem.Instance.RegisterInvoke<SessionAcceptTimeoutInvoke>(TimerCoreInvokeType.SessionAcceptTimeout);
            EventSystem.Instance.RegisterInvoke<SessionIdleCheckerInvoke>(TimerCoreInvokeType.SessionIdleChecker);
            EventSystem.Instance.RegisterInvoke<FiberInit_NetClient>(SceneType.NetClient);
            EventSystem.Instance.RegisterInvoke<NetComponentOnReadInvoker_NetClient>(SceneType.NetClient);
            EventSystem.Instance.RegisterInvoke<MoveTimerInvoke>(TimerInvokeType.MoveTimer);
            EventSystem.Instance.RegisterInvoke<GetAllConfigBytes>();
            EventSystem.Instance.RegisterInvoke<GetOneConfigBytes>();
            EventSystem.Instance.RegisterInvoke<WaitCoroutineLockTimerInvoke>(TimerCoreInvokeType.CoroutineTimeout);
        }

        public static void RegisterEvent()
        {
            EventSystem.Instance.RegisterEvent<NumericChangeEvent_NotifyWatcher>(SceneType.All);
            EventSystem.Instance.RegisterEvent<EntryEvent1_InitShare>(SceneType.StateSync);
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
            
        }
    }
}