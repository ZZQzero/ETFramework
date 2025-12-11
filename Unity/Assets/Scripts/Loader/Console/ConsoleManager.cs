#if DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameUI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ET
{
    /// <summary>
    /// Console管理器
    /// </summary>
    public class ConsoleManager : Singleton<ConsoleManager>,ISingletonAwake
    {
        /// <summary>
        /// 每种类型日志的最大数量（独立计数）
        /// </summary>
        private const int MAX_LOG_COUNT_PER_TYPE = 2000;
        
        /// <summary>
        /// 每次触发清理时移除的日志数量（批量移除优化性能）
        /// </summary>
        private const int BATCH_REMOVE_COUNT = 100;

        /// <summary>
        /// 所有日志列表（包含所有类型，最大容量 = 3 * MAX_LOG_COUNT_PER_TYPE）
        /// </summary>
        private readonly List<LogEntry> allLogs = new List<LogEntry>(MAX_LOG_COUNT_PER_TYPE * 3);

        /// <summary>
        /// 过滤后的日志列表
        /// </summary>
        private readonly List<LogEntry> filteredLogs = new List<LogEntry>(MAX_LOG_COUNT_PER_TYPE * 3);
        
        // 辅助队列，用于快速定位要移除的日志，避免O(N)查找
        private readonly Queue<LogEntry> _logEntries = new Queue<LogEntry>();
        private readonly Queue<LogEntry> _warningEntries = new Queue<LogEntry>();
        private readonly Queue<LogEntry> _errorEntries = new Queue<LogEntry>();
        private ConsoleTrigger _consoleTrigger;

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
        public bool IsDevelop { get; set;}
        public bool IsConsoleOpen;
        
        public void Awake()
        {
            // 注册Unity日志回调
            Application.logMessageReceived += OnLogMessageReceived;
            GameObject obj = new GameObject("ConsoleManager");
            _consoleTrigger = obj.AddComponent<ConsoleTrigger>();
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
            if (!IsDevelop)
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

            // 检查当前类型的日志是否超过限制，超过则批量移除该类型的旧日志
            RemoveOldLogsIfNeeded(logType);

            allLogs.Add(entry);
            
            // 加入对应辅助队列
            switch (logType)
            {
                case ConsoleLogType.Log:
                    _logEntries.Enqueue(entry);
                    break;
                case ConsoleLogType.Warning:
                    _warningEntries.Enqueue(entry);
                    break;
                case ConsoleLogType.Error:
                    _errorEntries.Enqueue(entry);
                    break;
            }

            UpdateLogCount(logType, 1);
            
            // 增量添加到过滤列表
            AddToFilteredList(entry);
            RefreshUIAsync().Forget();
        }

        /// <summary>
        /// 检查并移除该类型的旧日志（独立容量管理）
        /// </summary>
        private void RemoveOldLogsIfNeeded(ConsoleLogType logType)
        {
            Queue<LogEntry> targetQueue = logType switch
            {
                ConsoleLogType.Log => _logEntries,
                ConsoleLogType.Warning => _warningEntries,
                ConsoleLogType.Error => _errorEntries,
                _ => null
            };

            if (targetQueue == null || targetQueue.Count < MAX_LOG_COUNT_PER_TYPE)
            {
                return;
            }

            // 该类型已达上限，批量移除该类型的旧日志
            HashSet<LogEntry> logsToRemove = new HashSet<LogEntry>();
            int removeCount = Math.Min(BATCH_REMOVE_COUNT, targetQueue.Count);

            for (int i = 0; i < removeCount; i++)
            {
                logsToRemove.Add(targetQueue.Dequeue());
            }

            if (logsToRemove.Count > 0)
            {
                // 使用 RemoveAll 进行一次性 O(N) 清理
                allLogs.RemoveAll(x => logsToRemove.Contains(x));
                filteredLogs.RemoveAll(x => logsToRemove.Contains(x));

                // 更新计数
                UpdateLogCount(logType, -logsToRemove.Count);
            }
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

        private bool _isRefreshing = false;
        private async UniTaskVoid RefreshUIAsync()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            GameUIManager.Instance.RefreshUI(_consoleTrigger.GetUI(), GetAllFilteredLogs());
            _isRefreshing = false;
        }
        /// <summary>
        /// 清空所有日志
        /// </summary>
        public void Clear()
        {
            allLogs.Clear();
            filteredLogs.Clear();
            _logEntries.Clear();
            _warningEntries.Clear();
            _errorEntries.Clear();
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
        /// 获取所有过滤后的日志（零GC，只读视图）
        /// </summary>
        public IReadOnlyList<LogEntry> GetAllFilteredLogs()
        {
            return filteredLogs;
        }

        /// <summary>
        /// 获取过滤后的日志数据（用于UI刷新）
        /// 当过滤条件改变时，调用此方法获取最新数据
        /// </summary>
        public IReadOnlyList<LogEntry> GetFilteredLogsForRefresh()
        {
            // 确保过滤列表是最新的
            UpdateFilter();
            return filteredLogs;
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