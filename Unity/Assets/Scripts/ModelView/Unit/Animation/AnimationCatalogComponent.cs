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
            Hit_Light = 10,
            Hit_Medium = 11,
            Hit_Heavy = 12,
            Hit_Knockback = 13,
            Hit_Airborne = 14,
            Hit_Falling = 15,
            Hit_Knockdown = 16,
            Hit_GetUp = 17,
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

            assetName = null;
            return false;
        }
    }
}