using System;
using GameUI;
using Nino.Core;
using UnityEngine;
using YooAsset;

namespace ET
{
    public class Launcher: MonoBehaviour
    {
        private GameUIBase _loadUI;
        private void Awake()
        {
            this.StartAsync().NoContext();
        }
		
        private async ETTask StartAsync()
        {
            DontDestroyOnLoad(gameObject);
            
            World.Instance.AddSingleton<Logger>().Log = new UnityLogger();
            ETTask.ExceptionHandler += Log.Error;
            World.Instance.AddSingleton<Options>();
            World.Instance.AddSingleton<TimeInfo>();
            World.Instance.AddSingleton<FiberManager>();
            
            World.Instance.AddSingleton<GlobalConfigManager>();
            await GetVersion();
#if DEVELOPMENT_BUILD
            World.Instance.AddSingleton<ConsoleManager,bool>(GlobalConfigManager.Instance.Config.IsDevelop);
#endif
            var resourceLoad = World.Instance.AddSingleton<ResourcesLoadManager>();
            GameUIManager.Instance.Init();
            GameObjectPool.Instance.Init();
            LoadUI();
            await resourceLoad.CreatePackageAsync(OnResourceCreate);
        }

        private void OnResourceCreate(ResourcePackage package)
        {
            GameUIManager.Instance.SetPackage(package);
            GameObjectPool.Instance.SetPackage(package);
            GameUIManager.Instance.RefreshUI(_loadUI, package);
        }
        
        private void LoadUI()
        {
            var parent = GameUIManager.Instance.GetUILayer(EGameUILayer.Loading);
            var prefab = Resources.Load<GameObject>("UI/PatchPanel");
            if (prefab != null)
            {
                _loadUI = GameObject.Instantiate(prefab, parent).GetComponent<PatchPanelPanel>();
            }
        }
        
        private async ETTask GetVersion()
        {
            string address = GlobalConfigManager.Instance.Config.IPAddress;
            string url = $"http://{address}/get_resource_versions?v={RandomGenerator.RandUInt32()}";
            var info = await HttpClientHelper.GetJson(url);
            if (string.IsNullOrEmpty(info))
            {
                Log.Error("请求版本信息为空！");
                return;
            }
            var config = JsonUtility.FromJson<GlobalStartConfig>(info);
            GlobalConfigManager.Instance.Config.Version = config.Version;
            GlobalConfigManager.Instance.Config.ResourcePath = config.ResourcePath;
            GlobalConfigManager.Instance.Config.IsDevelop = config.IsDevelop;
        }
        
        private void Update()
        {
            TimeInfo.Instance.Update();
            FiberManager.Instance.Update();
        }

        private void FixedUpdate()
        {
            FiberManager.Instance.FixedUpdate();
        }
        
        private void LateUpdate()
        {
            FiberManager.Instance.LateUpdate();
        }

        private void OnApplicationQuit()
        {
            World.Instance.Dispose();
        }
    }
}