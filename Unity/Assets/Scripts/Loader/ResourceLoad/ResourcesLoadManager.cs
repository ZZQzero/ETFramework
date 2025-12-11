using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HybridCLR;
using Luban;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using SceneHandle = YooAsset.SceneHandle;

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

    public class ResourcesLoadManager : Singleton<ResourcesLoadManager>, ISingletonAwake
    {
        private Action<ResourcePackage> _onInitAction;
        private const int DownloadingMaxNum = 10;
        private const int FailedTryAgain = 3;
        public void Awake()
        {
            YooAssets.Initialize();
        }

        #region 资源下载

        public async ETTask CreatePackageAsync(Action<ResourcePackage> call)
        {
            _onInitAction = call;
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
                    Log.Error($"{initializationOperation.Error}  {initializationOperation.Status}");
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
                _onInitAction?.Invoke(package);
                return true;
            }
            Log.Error($"资源清单请求失败：{manifest.Error}");
            return false;
        }

        public async ETTask<ResourceDownloaderOperation> OnCreateDownLoad(ResourcePackage package)
        {
            var downloader = package.CreateResourceDownloader(DownloadingMaxNum, FailedTryAgain);
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

            downloaderOperation.DownloadFinishCallback = downloaderFinish;
            downloaderOperation.DownloadErrorCallback = errorCallback;
            downloaderOperation.DownloadUpdateCallback = progressCallback;
            downloaderOperation.BeginDownload();
        }

        private string GetHostServerURL(GlobalConfig config)
        {
            string hostServerIP = config.ResourcePath;
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
                    return $"{hostServerIP}/{appVersion}";
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
                    return $"{hostServerIP}/{appVersion}";
            }
#endif
        }

        
        public void DestroyPackage(string packageName)
        {
            ResourcePackage package = YooAssets.GetPackage(packageName);
            package.UnloadUnusedAssetsAsync();
        }

        #endregion
        //（加载少量资源或者单个不重复的资源使用这些方法）加载大量重复的使用对象池
        #region 加载资源
        
        /// <summary>
        /// 加载鲁班配置文件
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public ByteBuf LoadConfigByte(string file)
        {
            AssetHandle handle = YooAssets.LoadAssetSync<TextAsset>(file);
            var textAsset = (TextAsset)handle.AssetObject;
            handle.Release();
            return new ByteBuf(textAsset.bytes);
        }
        
        /// <summary>
        /// 异步加载非实例化资源
        /// </summary>
        /// <param name="location"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async ETTask<T> LoadAssetAsync<T>(string location) where T : UnityEngine.Object
        {
            AssetHandle handle = YooAssets.LoadAssetAsync<T>(location);
            await handle.Task;
            if (handle.AssetObject != null)
            {
                T asset = handle.AssetObject as T;
                handle.Release();
                return asset;
            }
            Log.Error($"异步加载资源失败{location}");
            handle.Release();
            return null;
        }

        /// <summary>
        /// 异步加载GameObject
        /// </summary>
        public async ETTask<GameObject> LoadGameObjectAsync(string location)
        {
            AssetHandle handle = YooAssets.LoadAssetAsync<GameObject>(location);
            await handle.Task;
            var option = handle.InstantiateAsync();
            await option.Task;
            handle.Release();
            return option.Result;
        }
        
        /// <summary>
        /// 同步加载GameObject
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public GameObject LoadGameObjectSync(string location)
        {
            AssetHandle handle = YooAssets.LoadAssetSync<GameObject>(location);
            if (handle.Status == EOperationStatus.Succeed)
            {
                var obj = handle.InstantiateSync();
                handle.Release();
                return obj;
            }
            return null;
        }
        
        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public async ETTask<byte[]> LoadByteAsync(string location)
        {
            RawFileHandle handle = YooAssets.LoadRawFileAsync(location);
            if (handle != null)
            {
                await handle.Task;
                byte[] t = handle.GetRawFileData();
                handle.Release();
                return t;
            }

            return null;
        }
        
        /// <summary>
        /// 异步加载所有资源
        /// </summary>
        public async ETTask<Dictionary<string, T>> LoadAllAssetsAsync<T>(string location) where T : UnityEngine.Object
        {
            AllAssetsHandle allAssetsOperationHandle = YooAssets.LoadAllAssetsAsync<T>(location);
            await allAssetsOperationHandle.Task;
            Dictionary<string, T> dictionary = new Dictionary<string, T>();
            foreach (UnityEngine.Object assetObj in allAssetsOperationHandle.AllAssetObjects)
            {
                T t = assetObj as T;
                if (t != null)
                {
                    dictionary.Add(t.name, t);
                }
            }

            allAssetsOperationHandle.Release();
            return dictionary;
        }
        
        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="loadSceneMode"></param>
        public async ETTask LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode,Action<float> call = null)
        {
            SceneHandle handler = YooAssets.LoadSceneAsync(sceneName, loadSceneMode);
            if (handler.Status == EOperationStatus.Failed)
            {
                Log.Error($"Load Scene Failed: {sceneName} | {loadSceneMode} | {handler.LastError}");
                return;
            }
            
            float lastReportedProgress = 0f;
            while (!handler.IsDone)
            {
                if (handler.Progress - lastReportedProgress > 0.01f)
                {
                    call?.Invoke(handler.Progress);
                    lastReportedProgress = handler.Progress;
                }
                await Task.Yield();
            }
            await handler.Task;
            handler.Release();
        }
        
        #endregion
        
        #region 加载Dll

        public async ETTask LoadHotfixDll()
        {
#if !UNITY_EDITOR
            // 1. 先加载 AOT 程序集用于补充元数据（必须在加载热更DLL之前）
            foreach (var aotDllName in GlobalConfigManager.AOTDllNames)
            {
                try
                {
                    string location = $"{aotDllName}.dll";
                    var asset = await LoadAssetAsync<TextAsset>(location);
                    if (asset != null)
                    {
                        byte[] aotDllBytes = asset.bytes;
                        if (aotDllBytes != null)
                        {
                            HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(aotDllBytes, HomologousImageMode.SuperSet);
                        }
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
                    var asset = await LoadAssetAsync<TextAsset>(location);
                    if (asset != null)
                    {
                        var bytes = asset.bytes;
                        if (bytes != null)
                        {
                            Assembly.Load(bytes);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Log.Error($"加载热更DLL失败: {dllName}, 错误: {e}");
                    throw;
                }
            }
#endif
            await ETTask.CompletedTask;
        }

        #endregion
    }
}