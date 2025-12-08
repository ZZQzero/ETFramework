#if ENABLE_CONSOLE || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameUI;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// Console管理器
    /// </summary>
    public class ConsoleManager : Singleton<ConsoleManager>,ISingletonAwake
    {
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
        /// 日志统计
        /// </summary>
        public int LogCount { get; private set; }
        public int WarningCount { get; private set; }
        public int ErrorCount { get; private set; }
        
        public void Awake()
        {
            // 注册Unity日志回调
            Application.logMessageReceived += OnLogMessageReceived;
        }
        
        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            ConsoleLogType consoleLogType = ConsoleLogType.Log;
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    consoleLogType = ConsoleLogType.Error;
                    break;
                case LogType.Warning:
                    consoleLogType = ConsoleLogType.Warning;
                    break;
                case LogType.Log:
                default:
                    consoleLogType = ConsoleLogType.Log;
                    break;
            }
            
            AddLog(consoleLogType, logString, stackTrace);
        }
        
        /// <summary>
        /// 添加日志
        /// </summary>
        private void AddLog(ConsoleLogType logType, string message, string stackTrace)
        {
            if (GlobalConfigManager.Instance == null || !GlobalConfigManager.Instance.Config.IsDevelop)
            {
                return;
            }

            // 检查是否与最后一条日志相同（合并连续相同日志）
            if (allLogs.Count > 0)
            {
                LogEntry lastLog = allLogs[^1];
                if (lastLog.LogType == logType && lastLog.Message.Equals(message, StringComparison.Ordinal))
                {
                    lastLog.Count++;
                    lastLog.Timestamp = DateTime.Now;
                    RefreshUIAsync().Forget();
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
            };

            // 添加到列表（如果超过最大数量，移除最旧的）
            if (allLogs.Count >= MAX_LOG_COUNT)
            {
                LogEntry removedLog = allLogs[0];
                allLogs.RemoveAt(0);
                
                // 从过滤列表中移除
                filteredLogs.Remove(removedLog);
                
                UpdateLogCount(removedLog.LogType, -1);
            }

            allLogs.Add(entry);
            UpdateLogCount(logType, 1);
            
            // 增量添加到过滤列表
            AddToFilteredList(entry);
            RefreshUIAsync().Forget();
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

        private async UniTaskVoid RefreshUIAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            GameUIManager.Instance.RefreshUI(LocalGameUIName.UIConsole, GetAllFilteredLogs());
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
        /// 更新过滤列表（全量重建，仅在过滤条件改变时调用）
        /// </summary>
        private void UpdateFilter()
        {
            filteredLogs.Clear();

            foreach (LogEntry log in allLogs)
            {
                if (ShouldShowLog(log))
                {
                    filteredLogs.Add(log);
                }
            }
        }
        
        /// <summary>
        /// 判断日志是否应该显示
        /// </summary>
        private bool ShouldShowLog(LogEntry log)
        {
            switch (log.LogType)
            {
                case ConsoleLogType.Log:
                    return showLog;
                case ConsoleLogType.Warning:
                    return showWarning;
                case ConsoleLogType.Error:
                    return showError;
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// 增量添加到过滤列表（新日志添加时调用）
        /// </summary>
        private void AddToFilteredList(LogEntry log)
        {
            if (ShouldShowLog(log))
            {
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
    }
}
#endif