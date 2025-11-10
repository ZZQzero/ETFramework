
namespace ET
{
    public static partial class GameRegister
    {
        public static void RegisterEvent()
        {
#if SHARE
            EventSystem.Instance.RegisterEvent<NumericChangeEvent_NotifyWatcher>(SceneType.All);
            EventSystem.Instance.RegisterEvent<EntryEvent1_InitShare>(SceneType.Main);
#endif

#if DOTNET
            EventSystem.Instance.RegisterEvent<EntryEvent2_InitServer>(SceneType.Main);
#endif
            
#if UNITY
            EventSystem.Instance.RegisterEvent<EntryEvent3_InitClient>(SceneType.Main);
            EventSystem.Instance.RegisterEvent<AfterCreateClientScene_AddComponent>(SceneType.Main);
            EventSystem.Instance.RegisterEvent<AfterCreateCurrentScene_AddComponent>(SceneType.Current);
            EventSystem.Instance.RegisterEvent<SceneChangeStart_AddComponent>(SceneType.Main);
            EventSystem.Instance.RegisterEvent<SceneChangeFinishEvent_CreateUIHelp>(SceneType.Current);
            EventSystem.Instance.RegisterEvent<LoginFinish_CreateLobbyUI>(SceneType.Main);
            EventSystem.Instance.RegisterEvent<LoginFinish_RemoveLoginUI>(SceneType.Main);
            EventSystem.Instance.RegisterEvent<AfterUnitCreate_CreateUnitView>(SceneType.Current);
            EventSystem.Instance.RegisterEvent<ChangePosition_SyncGameObjectPos>(SceneType.Current);
            EventSystem.Instance.RegisterEvent<ChangeRotation_SyncGameObjectRotation>(SceneType.Current);
            EventSystem.Instance.RegisterEvent<AppStartInitFinish_CreateLoginUI>(SceneType.Main);
#endif
            
        }
    }
}