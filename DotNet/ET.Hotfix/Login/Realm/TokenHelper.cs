using System;

namespace ET
{
    /// <summary>
    /// Token工具类，轻量级Token生成
    /// 配合TokenComponent使用，只需保证唯一性和不可猜测性
    /// </summary>
    public static class TokenHelper
    {
        /// <summary>
        /// 生成Token（Guid方案，全球唯一）
        /// </summary>
        public static string GenerateToken()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}