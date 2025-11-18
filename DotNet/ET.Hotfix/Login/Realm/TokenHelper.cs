using System;
using System.Security.Cryptography;
using System.Text;

namespace ET
{
    /// <summary>
    /// Token工具类，轻量级Token生成和验证（性能优化版本）
    /// 格式：account:timestamp:random:signature
    /// </summary>
    public static class TokenHelper
    {
        // Token密钥（应该从配置中读取）
        private static string GetSecretKey()
        {
            // TODO: 从GlobalConfig读取TokenSecretKey
            return "ETFramework-Token-Secret-Key-2024";
        }

        // Token过期时间（毫秒）
        private static long GetTokenExpireMs()
        {
            // TODO: 从GlobalConfig读取TokenExpireMinutes
            return 10 * 60 * 1000; // 10分钟
        }

        /// <summary>
        /// 生成Token（轻量级，性能优化）
        /// 格式：account:timestamp:random:signature
        /// </summary>
        public static string GenerateToken(string account, int zone)
        {
            long timestamp = TimeInfo.Instance.ServerNow();
            long random = RandomGenerator.RandomNumber(0, int.MaxValue);
            
            // 构建数据：account:timestamp:random:zone
            string data = $"{account}:{timestamp}:{random}:{zone}";
            
            // 生成HMAC-SHA256签名
            string signature = GenerateSignature(data);
            
            // 返回：account:timestamp:random:zone:signature
            return $"{data}:{signature}";
        }

        /// <summary>
        /// 验证Token
        /// </summary>
        public static bool ValidateToken(string token, out string account, out int zone)
        {
            account = null;
            zone = 0;

            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            try
            {
                // 分割Token：account:timestamp:random:zone:signature
                string[] parts = token.Split(':');
                if (parts.Length != 5)
                {
                    return false;
                }

                account = parts[0];
                long timestamp = long.Parse(parts[1]);
                long random = long.Parse(parts[2]);
                zone = int.Parse(parts[3]);
                string signature = parts[4];

                // 检查过期时间
                long now = TimeInfo.Instance.ServerNow();
                if (now - timestamp > GetTokenExpireMs())
                {
                    return false;
                }

                // 重新构建数据并验证签名
                string data = $"{account}:{timestamp}:{random}:{zone}";
                string expectedSignature = GenerateSignature(data);
                
                if (signature != expectedSignature)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 生成HMAC-SHA256签名（简化版，只对关键数据签名）
        /// </summary>
        private static string GenerateSignature(string data)
        {
            string secretKey = GetSecretKey();
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                // 使用Base64编码，但只取前16个字符（足够安全且更短）
                return Convert.ToBase64String(hashBytes).Substring(0, 16).Replace('+', '-').Replace('/', '_');
            }
        }
    }
}