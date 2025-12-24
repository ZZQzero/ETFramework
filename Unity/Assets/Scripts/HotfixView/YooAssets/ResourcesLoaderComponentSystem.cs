using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using SceneHandle = YooAsset.SceneHandle;

namespace ET
{
    [EntitySystemOf(typeof(ResourcesLoaderComponent))]
    public static partial class ResourcesLoaderComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ResourcesLoaderComponent self)
        {
            self.package = YooAssets.GetPackage("DefaultPackage");
        }

        [EntitySystem]
        private static void Awake(this ResourcesLoaderComponent self, string packageName)
        {
            self.package = YooAssets.GetPackage(packageName);
        }

        [EntitySystem]
        private static void Destroy(this ResourcesLoaderComponent self)
        {
            foreach (var kv in self.handlers)
            {
                switch (kv.Value)
                {
                    case AssetHandle handle:
                        handle.Release();
                        break;
                    case AllAssetsHandle handle:
                        handle.Release();
                        break;
                    case SubAssetsHandle handle:
                        handle.Release();
                        break;
                    case RawFileHandle handle:
                        handle.Release();
                        break;
                    case SceneHandle handle:
                        /*if (!handle.IsMainScene())
                        {
                            handle.UnloadAsync();
                        }*/
                        break;
                }
            }
        }

        public static async ETTask<T> LoadAssetAsync<T>(this ResourcesLoaderComponent self, string location) where T : UnityEngine.Object
        {
            using CoroutineLock coroutineLock = await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.ResourcesLoader, location.GetHashCode());
            HandleBase handler;
            if (!self.handlers.TryGetValue(location, out handler))
            {
                handler = self.package.LoadAssetAsync<T>(location);

                await handler.Task;

                self.handlers.Add(location, handler);
            }

            var assetHandle = (AssetHandle)handler;
            return assetHandle.InstantiateSync() as T;
        }

        public static async ETTask<Dictionary<string, T>> LoadAllAssetsAsync<T>(this ResourcesLoaderComponent self, string location) where T : UnityEngine.Object
        {
            using CoroutineLock coroutineLock = await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.ResourcesLoader, location.GetHashCode());

            HandleBase handler;
            if (!self.handlers.TryGetValue(location, out handler))
            {
                handler = self.package.LoadAllAssetsAsync<T>(location);
                await handler.Task;
                self.handlers.Add(location, handler);
            }

            Dictionary<string, T> dictionary = new Dictionary<string, T>();
            foreach (UnityEngine.Object assetObj in ((AllAssetsHandle)handler).AllAssetObjects)
            {
                T t = assetObj as T;
                dictionary.Add(t.name, t);
            }

            return dictionary;
        }

        public static async ETTask LoadSceneAsync(this ResourcesLoaderComponent self, string location, LoadSceneMode loadSceneMode)
        {
            using CoroutineLock coroutineLock = await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.ResourcesLoader, location.GetHashCode());

            HandleBase handler;
            if (self.handlers.TryGetValue(location, out handler))
            {
                // 检查句柄是否已被释放（LoadSceneMode.Single 会自动卸载之前的场景）
                SceneHandle sceneHandle = (SceneHandle)handler;
                if (sceneHandle.IsValid)
                {
                    Log.Error($"场景 {location} 正在加载或已加载，等待完成...");
                    await handler.Task; // 等待之前的加载完成
                    return;
                }
                else
                {
                    // 句柄已失效，从字典中移除，重新加载
                    Log.Warning($"场景 {location} 的句柄已失效（可能被 LoadSceneMode.Single 卸载），重新加载");
                    self.handlers.Remove(location);
                }
            }

            Log.Error($"开始加载场景: {location}");
            handler = (SceneHandle)self.package.LoadSceneAsync(location);
            await handler.Task;
            self.handlers.Add(location, handler);
        }
    }
}

