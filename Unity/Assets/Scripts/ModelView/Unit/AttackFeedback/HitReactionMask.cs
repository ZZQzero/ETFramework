using System;

namespace ET
{
    /// <summary>
    /// 受击类型掩码（用于 RulesProfile 的可接受类型集合）。
    /// </summary>
    [Flags]
    public enum HitReactionMask : int
    {
        None = 0,
        /// <summary>轻受击</summary>
        Light = 1 << 0,
        /// <summary>中受击</summary>
        Medium = 1 << 1,
        /// <summary>重受击</summary>
        Heavy = 1 << 2,
        /// <summary>踉跄</summary>
        Stagger = 1 << 3,
        /// <summary>眩晕</summary>
        Stun = 1 << 4,
        /// <summary>全选</summary>
        All = Light | Medium | Heavy | Stagger | Stun,
    }

    public static class HitReactionMaskExtensions
    {
        public static HitReactionMask ToMask(HitReactionType type)
        {
            switch (type)
            {
                case HitReactionType.Light: return HitReactionMask.Light;
                case HitReactionType.Medium: return HitReactionMask.Medium;
                case HitReactionType.Heavy: return HitReactionMask.Heavy;
                case HitReactionType.Stagger: return HitReactionMask.Stagger;
                case HitReactionType.Stun: return HitReactionMask.Stun;
                default: return HitReactionMask.None;
            }
        }
    }
}