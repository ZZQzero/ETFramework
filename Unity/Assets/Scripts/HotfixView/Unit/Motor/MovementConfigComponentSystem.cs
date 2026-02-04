using UnityEngine;

namespace ET
{
    public static partial class MovementConfigComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MovementConfigComponent self,string assetName)
        {
            self.LoadGroundConfigAsync(assetName).NoContext();
        }
        
        private static async ETTask LoadGroundConfigAsync(this MovementConfigComponent self,string assetName)
        {
            var asset = await ResourcesLoadManager.Instance.LoadAssetAsync<GroundDetectorConfigAsset>(assetName);
            if (asset != null)
            {
                self.GroundConfigAsset = asset;
            }
            await ETTask.CompletedTask;
        }
    }
}