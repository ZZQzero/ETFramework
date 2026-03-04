using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 动画资源目录（Catalog）
    /// - 挂在 Unit 上：生命周期/调试/热更更友好
    /// - 上层负责构建（玩家/怪物/皮肤/变身/回放均可）
    /// - 表现层（AnimatorComponentSystem）只消费，不关心来源
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public sealed class AnimationCatalogComponent : Entity, IAwake
    {
        /// <summary>
        /// 动画语义 Key（只表达“用途”，不表达“来源”）
        /// 后续扩展：Hit、Death、SkillCast、TurnInPlace...
        /// </summary>
        public enum AnimKey : byte
        {
            Locomotion_Move = 1,
            Locomotion_Jump = 2,

            // 受击系列 (Hit Reactions)
            Hit_Light,
            Hit_Heavy,
            Hit_GroundToAir,     // 击飞表现（进入空中）
            Hit_AirCombo,   // 空中受击（非终结）
            Hit_AirToGround,   // 砸地表现(从空中落地)
            Hit_Knockdown,
            Hit_Pull,       // 拉拽
            Hit_GetUp,
        }

        private readonly Dictionary<AnimKey, string> _assets = new Dictionary<AnimKey, string>(8);

        public void Set(AnimKey key, string assetName)
        {
            // 允许覆盖：皮肤/变身/运行时切换时更方便
            _assets[key] = assetName ?? string.Empty;
        }

        public bool TryGet(AnimKey key, out string assetName)
        {
            if (_assets.TryGetValue(key, out assetName))
            {
                return !string.IsNullOrWhiteSpace(assetName);
            }

            return false;
        }
    }
}