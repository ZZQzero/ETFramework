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
        /// <summary>击退</summary>
        Knockback = 1 << 3,
        /// <summary>击飞</summary>
        Knockup = 1 << 4,
        /// <summary>击倒</summary>
        Knockdown = 1 << 5,
        /// <summary>全选</summary>
        All = Light | Medium | Heavy | Knockback | Knockup | Knockdown,
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
                case HitReactionType.Knockback: return HitReactionMask.Knockback;
                case HitReactionType.Knockup: return HitReactionMask.Knockup;
                case HitReactionType.Knockdown: return HitReactionMask.Knockdown;
                default: return HitReactionMask.None;
            }
        }
    }
}