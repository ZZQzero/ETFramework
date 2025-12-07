using System;
using CommandLine;
using GameUI;
using UnityEngine;

namespace ET
{
    public class Launcher: MonoBehaviour
    {
        private GameUIBase loadUI;
        private void Start()
        {
            this.StartAsync().NoContext();
        }
		
        private async ETTask StartAsync()
        {
            DontDestroyOnLoad(gameObject);
            World.Instance.AddSingleton<ConsoleManager>();
            GameUIManager.Instance.Init();
            LoadUI();
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Error(e.ExceptionObject.ToString());
            };
            
            // 命令行参数
            string[] args = "".Split(" ");
            Parser.Default.ParseArguments<Options>(args)
                .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                .WithParsed((o)=>World.Instance.AddSingleton(o, typeof(Options)));
            
            AddSingleton();
            await World.Instance.AddSingleton<ResourcesComponent>().CreatePackageAsync(loadUI);
        }

        private void AddSingleton()
        {
            World.Instance.AddSingleton<GlobalConfigManager>();
            Options.Instance.SceneName = GlobalConfigManager.Instance.Config.SceneName;
            
            World.Instance.AddSingleton<Logger>().Log = new UnityLogger();
            ETTask.ExceptionHandler += Log.Error;
            World.Instance.AddSingleton<TimeInfo>();
            World.Instance.AddSingleton<FiberManager>();
            
#if ENABLE_CONSOLE || DEVELOPMENT_BUILD
            // 初始化Console管理器
            ConsoleManager.Instance.IsEnabled = true;
            // 注册Unity日志回调
            Application.logMessageReceived += OnLogMessageReceived;
#endif
        }
        private void LoadUI()
        {
            var parent = GameUIManager.Instance.GetUILayer(EGameUILayer.Loading);
            var prefab = Resources.Load<GameObject>("UI/PatchPanel");
            if (prefab != null)
            {
                loadUI = GameObject.Instantiate(prefab, parent).GetComponent<PatchPanelPanel>();
            }
        }
        
        
#if ENABLE_CONSOLE || DEVELOPMENT_BUILD
        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            ConsoleLogType consoleLogType = ConsoleLogType.Log;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    consoleLogType = ConsoleLogType.Error;
                    break;
                case LogType.Warning:
                    consoleLogType = ConsoleLogType.Warning;
                    break;
                case LogType.Log:
                default:
                    consoleLogType = ConsoleLogType.Log;
                    break;
            }

            if (ConsoleManager.Instance == null)
            {
                return;
            }
            ConsoleManager.Instance.AddLog(consoleLogType, logString, stackTrace);
        }
#endif
        
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