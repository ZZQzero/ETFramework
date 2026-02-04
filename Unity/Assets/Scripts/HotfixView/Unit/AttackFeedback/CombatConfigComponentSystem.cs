namespace ET
{
    public static partial class CombatConfigComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CombatConfigComponent self,string assetName)
        {
            self.LoadCombatConfigAsync(assetName).NoContext();
        }
        
        private static async ETTask LoadCombatConfigAsync(this CombatConfigComponent self,string assetName)
        {
            var asset = await ResourcesLoadManager.Instance.LoadAssetAsync<HitReactionProfileAsset>(assetName);
            if (asset != null)
            {
                self.HitReactionAsset = asset;
                self.cached = HitReactionProfileProvider.BuildRules(asset.Rules);
                self.CachedFeedback = HitReactionProfileProvider.BuildFeedback(asset.Feedback);
                self.CachedAirCombo = HitReactionProfileProvider.BuildAirCombo(asset.AirCombo);
                self.CacheReady = true;
            }
            await ETTask.CompletedTask;
        }
    }
}