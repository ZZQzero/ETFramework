namespace ET
{
    /// <summary>
    /// 战斗配置（最终执行参数）
    /// - 挂在 Unit 上，由上层组装（表/默认值/BUFF）
    /// - 受击系统只消费，不关心数据来源
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public sealed class CombatConfigComponent : Entity, IAwake<string>
    {
        /// <summary>受击配置资产（规则/反馈/空连）</summary>
        public HitReactionProfileAsset HitReactionAsset;

        /// <summary>运行时缓存：规则</summary>
        public HitReactionConfig HitReactionConfig;

        /// <summary>运行时缓存：反馈</summary>
        public HitFeedbackConfig Feedback;

        /// <summary>运行时缓存：空连</summary>
        public HitAirComboProfile CachedAirCombo;

        /// <summary>运行时缓存：拴系（地面+空中共用）</summary>
        public HitTetherProfile CachedTether;

        /// <summary>缓存是否已构建</summary>
        public bool CacheReady;

        /// <summary>缺失配置是否已告警</summary>
        public bool MissingProfileLogged;
    }
}