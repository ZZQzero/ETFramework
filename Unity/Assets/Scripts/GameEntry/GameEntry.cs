using UnityEngine;

namespace ET
{
    public class GameEntry : MonoBehaviour
    {
        private void Awake()
        {
            ETClient.Model.NinoGen.Deserializer.Init();
            ETClient.Model.NinoGen.Serializer.Init();
            ETClient.Model.NinoGen.NinoBuiltInTypesRegistration.Init();
            
            ETClient.Hotfix.NinoGen.Deserializer.Init();
            ETClient.Hotfix.NinoGen.Serializer.Init();
            ETClient.Hotfix.NinoGen.NinoBuiltInTypesRegistration.Init();
            
            GameRegister.RegisterSingleton();
            GameRegister.RegisterEntitySystem();
            
            GameRegisterHotfix.RegisterAIAuto();
            GameRegisterHotfix.RegisterEventAuto();
            GameRegisterHotfix.RegisterInvokeAuto();
            GameRegisterHotfix.RegisterMessageAuto();
            GameRegisterHotfix.RegisterMessageSessionAuto();
            GameRegisterHotfixView.RegisterEventAuto();
            StartAsync().NoContext();
        }
        
        private async ETTask StartAsync()
        {
            WinPeriod.Init();
            Log.Info("Entry Start");
            await FiberManager.Instance.Create(SchedulerType.Main, SceneType.Main, 0, SceneType.Main, "Main");
            await FiberManager.Instance.Create(SchedulerType.Main, SceneType.NetClient, 0, SceneType.NetClient, "NetClient");
            
        }
    }
}
