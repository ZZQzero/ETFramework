﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HybridCLR;
using UnityEngine;

namespace ET
{
    public class CodeLoader: Singleton<CodeLoader>, ISingletonAwake
    {
        private Assembly modelAssembly;
        private Assembly modelViewAssembly;

        private Dictionary<string, TextAsset> dlls;
        private Dictionary<string, TextAsset> aotDlls;

        public void Awake()
        {
            Start().NoContext();
        }

        private async ETTask DownloadAsync()
        {
            if (!Define.IsEditor)
            {
                this.dlls = await ResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>($"Assets/Bundles/Code/ETClient.Model.dll.bytes");
                if (Define.EnableIL2CPP)
                {
                    this.aotDlls = await ResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>($"Assets/Bundles/AotDlls/mscorlib.dll.bytes");
                }
            }
        }

        public async ETTask Start()
        {
            await DownloadAsync();
            
            if (!Define.IsEditor)
            {
                byte[] modelAssBytes = this.dlls["ETClient.Model.dll"].bytes;
                byte[] modelPdbBytes = this.dlls["ETClient.Model.pdb"].bytes;
                byte[] modelViewAssBytes = this.dlls["ETClient.ModelView.dll"].bytes;
                byte[] modelViewPdbBytes = this.dlls["ETClient.ModelView.pdb"].bytes;
                // 如果需要测试，可替换成下面注释的代码直接加载Packages/cn.etetet.loader/Code/ETClient.Model.dll.bytes，但真正打包时必须使用上面的代码
                //modelAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.Model.dll.bytes"));
                //modelPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.Model.pdb.bytes"));
                //modelViewAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.ModelView.dll.bytes"));
                //modelViewPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.ModelView.pdb.bytes"));

                if (Define.EnableIL2CPP)
                {
                    foreach (var kv in this.aotDlls)
                    {
                        TextAsset textAsset = kv.Value;
                        RuntimeApi.LoadMetadataForAOTAssembly(textAsset.bytes, HomologousImageMode.SuperSet);
                    }
                }
                this.modelAssembly = Assembly.Load(modelAssBytes, modelPdbBytes);
                this.modelViewAssembly = Assembly.Load(modelViewAssBytes, modelViewPdbBytes);
            }
            else
            {
                byte[] modelAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.Model.dll.bytes"));
                byte[] modelPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.Model.pdb.bytes"));
                byte[] modelViewAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.ModelView.dll.bytes"));
                byte[] modelViewPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.ModelView.pdb.bytes"));
                this.modelAssembly = Assembly.Load(modelAssBytes, modelPdbBytes);
                this.modelViewAssembly = Assembly.Load(modelViewAssBytes, modelViewPdbBytes);
            }
            
            (Assembly hotfixAssembly, Assembly hotfixViewAssembly) = this.LoadHotfix();

            /*World.Instance.AddSingleton<CodeTypes, Assembly[]>(new[]
            {
                typeof (World).Assembly, typeof (Init).Assembly, this.modelAssembly, this.modelViewAssembly, hotfixAssembly,
                hotfixViewAssembly
            });*/

            Debug.LogError("code load");
            IStaticMethod start = new StaticMethod(this.modelAssembly, "ET.Entry", "Start");
            start.Run();
        }

        private (Assembly, Assembly) LoadHotfix()
        {
            byte[] hotfixAssBytes;
            byte[] hotfixPdbBytes;
            byte[] hotfixViewAssBytes;
            byte[] hotfixViewPdbBytes;
            Assembly hotfixAssembly = null;
            Assembly hotfixViewAssembly = null;
            if (!Define.IsEditor)
            {
                hotfixAssBytes = this.dlls["ETClient.Hotfix.dll"].bytes;
                hotfixPdbBytes = this.dlls["ETClient.Hotfix.pdb"].bytes;
                hotfixViewAssBytes = this.dlls["ETClient.HotfixView.dll"].bytes;
                hotfixViewPdbBytes = this.dlls["ETClient.HotfixView.pdb"].bytes;
                // 如果需要测试，可替换成下面注释的代码直接加载Packages/cn.etetet.loader/Code/Hotfix.dll.bytes，但真正打包时必须使用上面的代码
                //hotfixAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.Hotfix.dll.bytes"));
                //hotfixPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.Hotfix.pdb.bytes"));
                //hotfixViewAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.HotfixView.dll.bytes"));
                //hotfixViewPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.HotfixView.pdb.bytes"));
                hotfixAssembly = Assembly.Load(hotfixAssBytes, hotfixPdbBytes);
                hotfixViewAssembly = Assembly.Load(hotfixViewAssBytes, hotfixViewPdbBytes);
            }
            else
            {
                hotfixAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.Hotfix.dll.bytes"));
                hotfixPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.Hotfix.pdb.bytes"));
                hotfixViewAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.HotfixView.dll.bytes"));
                hotfixViewPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ETClient.HotfixView.pdb.bytes"));
                hotfixAssembly = Assembly.Load(hotfixAssBytes, hotfixPdbBytes);
                hotfixViewAssembly = Assembly.Load(hotfixViewAssBytes, hotfixViewPdbBytes);
            }
            
            return (hotfixAssembly, hotfixViewAssembly);
        }

        public void Reload()
        {
            (Assembly hotfixAssembly, Assembly hotfixViewAssembly) = this.LoadHotfix();

            /*CodeTypes codeTypes = World.Instance.AddSingleton<CodeTypes, Assembly[]>(new[]
            {
                typeof (World).Assembly, typeof (Init).Assembly, this.modelAssembly, this.modelViewAssembly, hotfixAssembly,
                hotfixViewAssembly
            });
            codeTypes.CodeProcess();*/

            Log.Info($"reload dll finish!");
        }
    }
}