﻿using System;
using CommandLine;
using GameUI;
using UnityEngine;

namespace ET
{
    public class Init: MonoBehaviour
    {
        private GameUIBase loadUI;
        private void Start()
        {
            this.StartAsync().NoContext();
        }
		
        private async ETTask StartAsync()
        {
            DontDestroyOnLoad(gameObject);
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

            GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
            Options.Instance.SceneName = globalConfig.SceneName;
			
            World.Instance.AddSingleton<Logger>().Log = new UnityLogger();
            ETTask.ExceptionHandler += Log.Error;
			
            World.Instance.AddSingleton<TimeInfo>();
            World.Instance.AddSingleton<FiberManager>();
            
            await World.Instance.AddSingleton<ResourcesComponent>().CreatePackageAsync(loadUI);
            Debug.LogError("init");
            //World.Instance.AddSingleton<CodeLoader>().Start().NoContext();
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