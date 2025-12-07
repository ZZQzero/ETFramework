#if ENABLE_CONSOLE || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    /// <summary>
    /// Console管理器 - 单例
    /// </summary>
    public class ConsoleManager : Singleton<ConsoleManager>,ISingletonAwake
    {
        /// <summary>
        /// 日志更新事件
        /// </summary>
        public event Action OnLogAdded;

        /// <summary>
        /// 日志清空事件
        /// </summary>
        public event Action OnLogCleared;

        /// <summary>
        /// 最大日志数量
        /// </summary>
        private const int MAX_LOG_COUNT = 1000;

        /// <summary>
        /// 所有日志列表（循环缓冲区）
        /// </summary>
        private readonly List<LogEntry> allLogs = new List<LogEntry>(MAX_LOG_COUNT);

        /// <summary>
        /// 过滤后的日志列表
        /// </summary>
        private readonly List<LogEntry> filteredLogs = new List<LogEntry>(MAX_LOG_COUNT);

        /// <summary>
        /// 日志类型过滤器
        /// </summary>
        private bool showLog = true;
        private bool showWarning = true;
        private bool showError = true;

        /// <summary>
        /// 搜索文本
        /// </summary>
        private string searchText = string.Empty;

        /// <summary>
        /// 日志统计
        /// </summary>
        public int LogCount { get; private set; }
        public int WarningCount { get; private set; }
        public int ErrorCount { get; private set; }

        /// <summary>
        /// 是否启用Console（运行时配置）
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        public ConsoleManager()
        {
        }

        /// <summary>
        /// 添加日志
        /// </summary>
        public void AddLog(ConsoleLogType logType, string message, string stackTrace)
        {
            if (!IsEnabled)
            {
                return;
            }

            // 检查是否与最后一条日志相同（合并重复日志）
            if (allLogs.Count > 0)
            {
                LogEntry lastLog = allLogs[allLogs.Count - 1];
                if (lastLog.LogType == logType && lastLog.Message == message)
                {
                    lastLog.Count++;
                    UpdateFilter();
                    OnLogAdded?.Invoke();
                    return;
                }
            }

            // 创建新日志条目
            LogEntry entry = new LogEntry
            {
                LogType = logType,
                Message = message,
                StackTrace = stackTrace,
                Timestamp = DateTime.Now,
                Count = 1,
                IsExpanded = false
            };

            // 添加到列表（如果超过最大数量，移除最旧的）
            if (allLogs.Count >= MAX_LOG_COUNT)
            {
                LogEntry removedLog = allLogs[0];
                allLogs.RemoveAt(0);

                // 更新计数
                UpdateLogCount(removedLog.LogType, -1);
            }

            allLogs.Add(entry);

            // 更新计数
            UpdateLogCount(logType, 1);

            // 更新过滤列表
            UpdateFilter();

            // 触发事件
            OnLogAdded?.Invoke();
        }

        /// <summary>
        /// 更新日志计数
        /// </summary>
        private void UpdateLogCount(ConsoleLogType logType, int delta)
        {
            switch (logType)
            {
                case ConsoleLogType.Log:
                    LogCount += delta;
                    break;
                case ConsoleLogType.Warning:
                    WarningCount += delta;
                    break;
                case ConsoleLogType.Error:
                    ErrorCount += delta;
                    break;
            }
        }

        /// <summary>
        /// 清空所有日志
        /// </summary>
        public void Clear()
        {
            allLogs.Clear();
            filteredLogs.Clear();
            LogCount = 0;
            WarningCount = 0;
            ErrorCount = 0;
            OnLogCleared?.Invoke();
        }

        /// <summary>
        /// 设置日志类型过滤
        /// </summary>
        public void SetLogTypeFilter(bool log, bool warning, bool error)
        {
            showLog = log;
            showWarning = warning;
            showError = error;
            UpdateFilter();
        }

        /// <summary>
        /// 获取日志类型过滤状态
        /// </summary>
        public void GetLogTypeFilter(out bool log, out bool warning, out bool error)
        {
            log = showLog;
            warning = showWarning;
            error = showError;
        }

        /// <summary>
        /// 设置搜索文本
        /// </summary>
        public void SetSearchText(string text)
        {
            searchText = text ?? string.Empty;
            UpdateFilter();
        }

        /// <summary>
        /// 获取搜索文本
        /// </summary>
        public string GetSearchText()
        {
            return searchText;
        }

        /// <summary>
        /// 更新过滤列表
        /// </summary>
        private void UpdateFilter()
        {
            filteredLogs.Clear();

            foreach (LogEntry log in allLogs)
            {
                // 类型过滤
                bool typeMatch = false;
                switch (log.LogType)
                {
                    case ConsoleLogType.Log:
                        typeMatch = showLog;
                        break;
                    case ConsoleLogType.Warning:
                        typeMatch = showWarning;
                        break;
                    case ConsoleLogType.Error:
                        typeMatch = showError;
                        break;
                }

                if (!typeMatch)
                {
                    continue;
                }

                // 搜索过滤
                if (!log.MatchesSearch(searchText))
                {
                    continue;
                }

                filteredLogs.Add(log);
            }
        }

        /// <summary>
        /// 获取过滤后的日志数量
        /// </summary>
        public int GetFilteredLogCount()
        {
            return filteredLogs.Count;
        }

        /// <summary>
        /// 获取过滤后的日志
        /// </summary>
        public LogEntry GetFilteredLog(int index)
        {
            if (index >= 0 && index < filteredLogs.Count)
            {
                return filteredLogs[index];
            }
            return null;
        }

        /// <summary>
        /// 获取所有过滤后的日志（返回新的列表副本）
        /// </summary>
        public List<LogEntry> GetAllFilteredLogs()
        {
            return new List<LogEntry>(filteredLogs);
        }

        /// <summary>
        /// 获取过滤后的日志数据（用于UI刷新）
        /// 当过滤条件改变时，调用此方法获取最新数据
        /// </summary>
        public List<LogEntry> GetFilteredLogsForRefresh()
        {
            // 确保过滤列表是最新的
            UpdateFilter();
            return new List<LogEntry>(filteredLogs);
        }

        /// <summary>
        /// 导出日志到文本
        /// </summary>
        public string ExportToText(bool filteredOnly = true)
        {
            List<LogEntry> logsToExport = filteredOnly ? filteredLogs : allLogs;
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Console Log Export - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Logs: {logsToExport.Count}");
            sb.AppendLine(new string('=', 80));
            sb.AppendLine();

            foreach (LogEntry log in logsToExport)
            {
                sb.AppendLine(log.FormatOutput(true));
                sb.AppendLine(new string('-', 80));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 切换日志展开状态
        /// </summary>
        public void ToggleLogExpanded(int index)
        {
            LogEntry log = GetFilteredLog(index);
            if (log != null)
            {
                log.IsExpanded = !log.IsExpanded;
            }
        }

        public void Awake()
        {
            
        }
    }
}
#endif