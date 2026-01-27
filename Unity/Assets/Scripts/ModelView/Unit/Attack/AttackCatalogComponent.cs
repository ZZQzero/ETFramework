using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 攻击目录（Catalog）
    /// - 挂在 Unit 上，由上层构建（玩家/怪物/皮肤/回放/网络）
    /// - AttackComponentSystem 只消费，不关心“是谁/从哪来”
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public sealed class AttackCatalogComponent : Entity, IAwake
    {
        /// <summary>基础攻击技能ID（用于选择 AttackConfigAsset）</summary>
        public int BasicAttackSkillId;

        /// <summary>可选：技能列表（后续扩展到技能AI/技能栏）</summary>
        public readonly List<int> SkillIds = new List<int>(8);

        /// <summary>
        /// 攻击目标 LayerMask（Unity bitmask）。
        /// 由上层按阵营/单位类型组装；攻击底层只消费。
        /// </summary>
        public int TargetLayerMask;
    }
}