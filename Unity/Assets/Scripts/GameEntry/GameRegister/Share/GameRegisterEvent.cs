
namespace ET
{
    public static partial class GameRegister
    {
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