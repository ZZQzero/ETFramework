#if ENABLE_CONSOLE || DEVELOPMENT_BUILD
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

        /// <summary>
        /// 是否展开显示堆栈
        /// </summary>
        public bool IsExpanded { get; set; }

        public LogEntry()
        {
            Timestamp = DateTime.Now;
            Count = 1;
            IsExpanded = false;
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

        /// <summary>
        /// 获取简短的消息（用于列表显示）
        /// </summary>
        public string GetShortMessage(int maxLength = 100)
        {
            if (string.IsNullOrEmpty(Message))
            {
                return string.Empty;
            }

            string msg = Message;
            int newLineIndex = msg.IndexOf('\n');
            if (newLineIndex > 0)
            {
                msg = msg.Substring(0, newLineIndex);
            }

            if (msg.Length > maxLength)
            {
                msg = msg.Substring(0, maxLength) + "...";
            }

            return msg;
        }

        /// <summary>
        /// 检查日志内容是否匹配搜索关键字
        /// </summary>
        public bool MatchesSearch(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return true;
            }

            searchText = searchText.ToLower();

            if (!string.IsNullOrEmpty(Message) && Message.ToLower().Contains(searchText))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(StackTrace) && StackTrace.ToLower().Contains(searchText))
            {
                return true;
            }

            return false;
        }
    }
}
#endif

