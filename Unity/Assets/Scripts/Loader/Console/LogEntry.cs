#if DEVELOPMENT_BUILD
using System;

namespace ET
{
    /// <summary>
    /// 日志类型枚举
    /// </summary>
    public enum ConsoleLogType
    {
        Log = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>
    /// 日志条目
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// 日志类型
        /// </summary>
        public ConsoleLogType LogType { get; set; }

        /// <summary>
        /// 日志消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 堆栈跟踪
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 日志计数（相同日志重复出现的次数）
        /// </summary>
        public int Count { get; set; }

        public LogEntry()
        {
            Timestamp = DateTime.Now;
            Count = 1;
        }

        /// <summary>
        /// 格式化输出（用于复制和保存）
        /// </summary>
        public string FormatOutput(bool includeStackTrace = true)
        {
            string timeStr = Timestamp.ToString("HH:mm:ss.fff");
            string typeStr = LogType.ToString().ToUpper();
            string result = $"[{timeStr}] [{typeStr}] {Message}";
            
            if (Count > 1)
            {
                result += $" (x{Count})";
            }
            
            if (includeStackTrace && !string.IsNullOrEmpty(StackTrace))
            {
                result += $"\n{StackTrace}";
            }
            
            return result;
        }
    }
}
#endif