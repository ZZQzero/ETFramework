using System;
using GameUI;
using UnityEngine;
using YooAsset;

namespace ET
{
    public class Launcher: MonoBehaviour
    {
        private GameUIBase _loadUI;
        private void Start()
        {
            this.StartAsync().NoContext();
        }
		
        private async ETTask StartAsync()
        {
            DontDestroyOnLoad(gameObject);
            World.Instance.AddSingleton<GlobalConfigManager>();
#if DEVELOPMENT_BUILD
            World.Instance.AddSingleton<ConsoleManager>();
            ConsoleManager.Instance.IsDevelop = GlobalConfigManager.Instance.Config.IsDevelop;
#endif
            var resourceLoad = World.Instance.AddSingleton<ResourcesLoadManager>();
            GameUIManager.Instance.Init();
            GameObjectPool.Instance.Init();
            LoadUI();
            AddSingleton();
            await resourceLoad.CreatePackageAsync(OnResourceCreate);
        }

        private void OnResourceCreate(ResourcePackage package)
        {
            GameUIManager.Instance.SetPackage(package);
            GameObjectPool.Instance.SetPackage(package);
            GameUIManager.Instance.RefreshUI(_loadUI, package);
        }

        private void AddSingleton()
        {
            World.Instance.AddSingleton<Options>();
            Options.Instance.SceneName = GlobalConfigManager.Instance.Config.SceneName;
            World.Instance.AddSingleton<Logger>().Log = new UnityLogger();
            ETTask.ExceptionHandler += Log.Error;
            World.Instance.AddSingleton<TimeInfo>();
            World.Instance.AddSingleton<FiberManager>();
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
        
        private void Update()
        {
            TimeInfo.Instance.Update();
            FiberManager.Instance.Update();
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