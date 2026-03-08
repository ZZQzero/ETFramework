namespace ET
{
    public static partial class CombatConfigComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CombatConfigComponent self, string assetName)
        {
            self.LoadCombatConfigAsync(assetName).NoContext();
        }

        private static async ETTask LoadCombatConfigAsync(this CombatConfigComponent self, string assetName)
        {
            var asset = await ResourcesLoadManager.Instance.LoadAssetAsync<HitReactionProfileAsset>(assetName);
            if (asset != null)
            {
                self.HitReactionAsset = asset;
                self.HitReactionConfig = BuildRules(asset.Rules);
                self.Feedback = BuildFeedback(asset.Feedback);
                self.CachedAirCombo = BuildAirCombo(asset.AirCombo);
                self.CacheReady = true;
            }

            await ETTask.CompletedTask;
        }

        private static HitReactionConfig BuildRules(in HitReactionRulesData data)
        {
            var scales = new HitReactionConfig.Scales(
                data.Scales.Knockback,
                data.Scales.Knockup);

            var limits = new HitReactionConfig.Limits(
                data.Limits.MaxHitStunMs,
                data.Limits.MaxKnockbackForce,
                data.Limits.MaxKnockupForce);

            return new HitReactionConfig(scales, limits);
        }

        private static HitFeedbackConfig BuildFeedback(in HitReactionFeedbackData data)
        {
            return new HitFeedbackConfig(new HitFeedbackConfig.Options(
                data.Option.AllowVictimHitStop,
                data.Option.VictimHitStopScale,
                data.Option.AllowScreenShake,
                data.Option.ScreenShakeScale,
                data.Option.AllowTimeScale,
                data.Option.TimeScaleScale));
        }

        private static HitAirComboProfile BuildAirCombo(in HitAirComboData data)
        {
            return new HitAirComboProfile(
                data.Enable,
                data.MaxAirOffsetMs,
                data.GravityScaleDuringCombo,
                data.MinFallSpeedAbs,
                data.DesiredComboHeight,
                data.ExitLerpMs,
                data.AbsoluteMaxHeight);
        }

        private static readonly HitReactionConfig DefaultRules = new HitReactionConfig(
            new HitReactionConfig.Scales(1f, 1f),
            new HitReactionConfig.Limits(int.MaxValue, float.MaxValue, float.MaxValue));

        private static readonly HitFeedbackConfig DefaultFeedback = new HitFeedbackConfig(
            new HitFeedbackConfig.Options(
                allowVictimHitStop: false,
                victimHitStopScale: 0f,
                allowScreenShake: false,
                screenShakeScale: 0f,
                allowTimeScale: false,
                timeScaleScale: 0f));
        
    }
}