using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameUI;
using Luban;
using UnityEngine;
using YooAsset;

namespace ET
{
    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    public class RemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }

        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }

        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackHostServer}/{fileName}";
        }
    }

    public class ResourcesComponent : Singleton<ResourcesComponent>, ISingletonAwake
    {
        private GameUIBase _uiBase;
        public void Awake()
        {
            YooAssets.Initialize();
        }

        public async ETTask CreatePackageAsync(GameUIBase uiBase)
        {
            _uiBase = uiBase;
            var config = GlobalConfigManager.Instance.Config;
            ResourcePackage package = YooAssets.CreatePackage(config.PackageName);
            InitializationOperation initializationOperation = null;
            switch (config.PlayMode)
            {
                case EPlayMode.EditorSimulateMode:
                {
                    var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(package.PackageName);
                    var packageRoot = simulateBuildResult.PackageRootDirectory;
                    var createParameters = new EditorSimulateModeParameters();
                    createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                    initializationOperation = package.InitializeAsync(createParameters);
                    break;
                }
                case EPlayMode.OfflinePlayMode:
                {
                    var createParameters = new OfflinePlayModeParameters();
                    createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    initializationOperation = package.InitializeAsync(createParameters);
                    break;
                }
                case EPlayMode.HostPlayMode:
                {
                    string defaultHostServer = GetHostServerURL(config);
                    string fallbackHostServer = GetHostServerURL(config);
                    IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                    var createParameters = new HostPlayModeParameters();
                    createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                    initializationOperation = package.InitializeAsync(createParameters);
                    break;
                }
                case EPlayMode.WebPlayMode:
                {
                    var createParameters = new WebPlayModeParameters();
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
			        string defaultHostServer = GetHostServerURL(config);
                    string fallbackHostServer = GetHostServerURL(config);
                    IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                    createParameters.WebServerFileSystemParameters = WechatFileSystemCreater.CreateWechatFileSystemParameters(remoteServices);
#else
                    createParameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
#endif
                    initializationOperation = package.InitializeAsync(createParameters);
                    break;
                }
            }

            if (initializationOperation != null)
            {
                await initializationOperation.Task;
                if (initializationOperation.Status != EOperationStatus.Succeed)
                {
                    Log.Error($"{initializationOperation.Error}");
                    return;
                }

                var succeed = await UpdatePackageVersion(package);
                if (!succeed)
                {
                    Log.Error($"{initializationOperation.Error}");
                }
            }
        }

        private async ETTask<bool> UpdatePackageVersion(ResourcePackage package)
        {
            var operation = package.RequestPackageVersionAsync();
            await operation.Task;
            if (operation.Status == EOperationStatus.Succeed)
            {
                return await UpdatePackageManifest(package,operation.PackageVersion);
            }
            Log.Error($"资源版本号请求失败：{operation.Error}");
            return false;
        }

        private async ETTask<bool> UpdatePackageManifest(ResourcePackage package,string version)
        {
            var manifest = package.UpdatePackageManifestAsync(version);
            await manifest.Task;
            if (manifest.Status == EOperationStatus.Succeed)
            {
                YooAssets.SetDefaultPackage(package);
                // 此时已经可以使用yooAssets加载资源了
                GameUIManager.Instance.SetPackage(package);
                GameUIManager.Instance.RefreshUI(_uiBase, package);
                return true;
            }
            Log.Error($"资源清单请求失败：{manifest.Error}");
            return false;
        }

        public async ETTask<ResourceDownloaderOperation> OnCreateDownLoad(ResourcePackage package)
        {
            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
            await ETTask.CompletedTask;
            return downloader;
        }

        public void BeginDownload(ResourceDownloaderOperation downloaderOperation,
            DownloaderOperation.DownloaderFinish downloaderFinish,
            DownloaderOperation.DownloadUpdate progressCallback,
            DownloaderOperation.DownloadError errorCallback)
        {
            if (downloaderOperation.TotalDownloadCount == 0)
            {
                return;
            }

            downloaderOperation.BeginDownload();
        }

        private string GetHostServerURL(GlobalConfig config)
        {
            string hostServerIP = config.IPAddress;
            string appVersion = config.Version;
            
#if UNITY_EDITOR
            switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
            {
                case UnityEditor.BuildTarget.Android:
                    return $"{hostServerIP}/CDN/Android/{appVersion}";
                case UnityEditor.BuildTarget.iOS:
                    return $"{hostServerIP}/CDN/IPhone/{appVersion}";
                case UnityEditor.BuildTarget.WebGL:
                    return $"{hostServerIP}/StreamingAssets/Bundles/{config.PackageName}";
                default:
                    return $"{hostServerIP}/CDN/PC/{appVersion}";
            }
#else
		        switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                        return $"{hostServerIP}/CDN/Android/{appVersion}";
                    case RuntimePlatform.IPhonePlayer:
                        return $"{hostServerIP}/CDN/IPhone/{appVersion}";
                    case RuntimePlatform.WebGLPlayer:
                        return $"{hostServerIP}/StreamingAssets/Bundles/{config.PackageName}";
                    default:
                        return $"{hostServerIP}/CDN/PC/{appVersion}";
                }
#endif
        }

        
        public void DestroyPackage(string packageName)
        {
            ResourcePackage package = YooAssets.GetPackage(packageName);
            package.UnloadUnusedAssetsAsync();
        }
        
        public ByteBuf LoadConfigByte(string file)
        {
            AssetHandle handle = YooAssets.LoadAssetSync<TextAsset>(file);
            var textAsset = (TextAsset)handle.AssetObject;
            handle.Release();
            return new ByteBuf(textAsset.bytes);
        }

        /// <summary>
        /// 主要用来加载dll config aotdll，因为这时候纤程还没创建，无法使用ResourcesLoaderComponent。
        /// 游戏中的资源应该使用ResourcesLoaderComponent来加载
        /// </summary>
        public async ETTask<T> LoadAssetAsync<T>(string location) where T : UnityEngine.Object
        {
            AssetHandle handle = YooAssets.LoadAssetAsync<T>(location);
            await handle.Task;
            T t = (T)handle.AssetObject;
            handle.Release();
            return t;
        }

        /// <summary>
        /// 主要用来加载dll config aotdll，因为这时候纤程还没创建，无法使用ResourcesLoaderComponent。
        /// 游戏中的资源应该使用ResourcesLoaderComponent来加载
        /// </summary>
        public async ETTask<Dictionary<string, T>> LoadAllAssetsAsync<T>(string location) where T : UnityEngine.Object
        {
            AllAssetsHandle allAssetsOperationHandle = YooAssets.LoadAllAssetsAsync<T>(location);
            await allAssetsOperationHandle.Task;
            Dictionary<string, T> dictionary = new Dictionary<string, T>();
            foreach (UnityEngine.Object assetObj in allAssetsOperationHandle.AllAssetObjects)
            {
                T t = assetObj as T;
                dictionary.Add(t.name, t);
            }

            allAssetsOperationHandle.Release();
            return dictionary;
        }
    }
}