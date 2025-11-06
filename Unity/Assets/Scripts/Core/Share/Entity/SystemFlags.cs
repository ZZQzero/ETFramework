using System;
using System.Runtime.CompilerServices;

namespace ET
{
    /// <summary>
    /// Entity系统能力位标记
    /// 存储在TypeSystems.OneTypeSystems中，按Entity类型缓存
    /// 用于高性能的能力检查，避免运行时接口查询
    /// </summary>
    [Flags]
    public enum SystemFlags : byte
    {
        None        = 0,
        Awake       = 1 << 0,  // 0x01 - 有IAwakeSystem
        Update      = 1 << 1,  // 0x02 - 有UpdateSystem
        LateUpdate  = 1 << 2,  // 0x04 - 有LateUpdateSystem
        Destroy     = 1 << 3,  // 0x08 - 有DestroySystem
        // 还可扩展4个位
    }
    
    /// <summary>
    /// SystemFlags扩展方法
    /// </summary>
    public static class SystemFlagsExtensions
    {
        /// <summary>
        /// 检查是否包含指定能力
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has(this SystemFlags flags, SystemFlags flag)
        {
            return (flags & flag) != 0;
        }
    }
}