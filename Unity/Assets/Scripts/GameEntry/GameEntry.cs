using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HybridCLR;


namespace ET
{
    public class GameEntry : MonoBehaviour
    {
        private async void Awake()
        {
#if UNITY_EDITOR
            // 1. 先加载 AOT 程序集用于补充元数据（必须在加载热更DLL之前）
            foreach (var aotDllName in GlobalConfigManager.AOTDllNames)
            {
                try
                {
                    string location = $"{aotDllName}.dll";
                    byte[] aotDllBytes = await ResourcesComponent.Instance.LoadByteAsync(location);
                    if (aotDllBytes != null)
                    {
                        HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(aotDllBytes, HomologousImageMode.SuperSet);
                    }
                }
                catch (System.Exception e)
                {
                    Log.Warning($"加载 AOT 补充元数据失败: {aotDllName}, 错误: {e.Message}");
                }
            }
            
            // 2. 加载热更 DLL
            foreach (var dllName in GlobalConfigManager.DllNames)
            {
                try
                {
                    string location = $"{dllName}.dll";
                    byte[] bytes = await ResourcesComponent.Instance.LoadByteAsync(location);
                    if (bytes != null)
                    {
                        Assembly.Load(bytes);
                    }
                }
                catch (System.Exception e)
                {
                    Log.Error($"加载热更DLL失败: {dllName}, 错误: {e}");
                    throw;
                }
            }
#endif
            
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
