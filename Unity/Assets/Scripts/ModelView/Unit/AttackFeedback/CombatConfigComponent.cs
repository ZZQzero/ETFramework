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
    }
}