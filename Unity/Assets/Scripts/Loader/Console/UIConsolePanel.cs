#if DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using GameUI;
using TMPro;

namespace ET
{
    /// <summary>
    /// Console面板
    /// </summary>
    public partial class UIConsolePanel : GameUILoopScrollBase
    {
        [Header("Toolbar")]
        public Toggle logToggle;
        public Toggle warningToggle;
        public Toggle errorToggle;
        public TMP_InputField searchInput;
        public Button clearButton;
        public Button saveButton;
        public Button closeButton;
        public LoopVerticalScrollRect scrollRect;

        [Header("Statistics")]
        public TextMeshProUGUI logNumText;
        public TextMeshProUGUI warningNumText;
        public TextMeshProUGUI errorNumText;
        public TextMeshProUGUI logCountText;
        public Image logBg;
        public Image warningBg;
        public Image errorBg;
        
        [Header("Item Pool")]
        private Stack<Transform> itemPool = new Stack<Transform>();

        /// <summary>
        /// 日志数据列表
        /// </summary>
        private IReadOnlyList<LogEntry> logList = Array.Empty<LogEntry>();
        private List<LogEntry> filterLogList = new List<LogEntry>();

        private bool isfilter = false;

        public override void OnInitUI()
        {
            ScrollRect = scrollRect;
            base.OnInitUI();
            // 初始化按钮事件
            if (logToggle != null)
            {
                logToggle.onValueChanged.AddListener(OnFilterChanged);
            }

            if (warningToggle != null)
            {
                warningToggle.onValueChanged.AddListener(OnFilterChanged);
            }

            if (errorToggle != null)
            {
                errorToggle.onValueChanged.AddListener(OnFilterChanged);
            }

            if (searchInput != null)
            {
                searchInput.onValueChanged.AddListener(OnSearchChanged);
            }

            if (clearButton != null)
            {
                clearButton.onClick.AddListener(OnClearClicked);
            }

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(OnSaveClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseUI);
            }

            logCountText.text = "";
            isfilter = false;
            // 初始化过滤状态
            UpdateFilterToggles();
            
            // 刷新列表
            RefreshLogList();
        }

        public override void OnOpenUI()
        {
            base.OnOpenUI();
            if (Data is IReadOnlyList<LogEntry> data)
            {
                logList = data;
                RefreshLogList();
            }
        }

        public override void OnCloseUI()
        {
            gameObject.SetActive(false);
            base.OnCloseUI();
            searchInput.text = "";
            isfilter = false;
            ConsoleManager.Instance.IsConsoleOpen = false;
        }

        public override void OnRefreshUI()
        {
            base.OnRefreshUI();
            if (Data is IReadOnlyList<LogEntry> data)
            {
                logList = data;
            }
            else
            {
                logList = ConsoleManager.Instance.GetFilteredLogsForRefresh();
            }
            if (isfilter && searchInput != null && !string.IsNullOrEmpty(searchInput.text))
            {
                filterLogList.Clear();
                foreach (var log in logList)
                {
                    if (log.Message.Contains(searchInput.text, StringComparison.OrdinalIgnoreCase))
                    {
                        filterLogList.Add(log);
                    }
                }
            }
            if (ScrollRect == null)
            {
                return;
            }

            var count = isfilter ? filterLogList.Count : logList.Count;
            ScrollRect.totalCount = count;
           
            ScrollRect.RefreshCells();
            UpdateStatistics();
        }

        public override void OnDestroyUI()
        {
            base.OnDestroyUI();
        }

        /// <summary>
        /// 过滤改变
        /// </summary>
        private void OnFilterChanged(bool value)
        {
            bool showLog = logToggle != null && logToggle.isOn;
            bool showWarning = warningToggle != null && warningToggle.isOn;
            bool showError = errorToggle != null && errorToggle.isOn;

            // 更新背景颜色
            if (logBg != null)
            {
                logBg.color = showLog ? Color.cyan : Color.white;
            }
            
            if (warningBg != null)
            {
                warningBg.color = showWarning ? Color.cyan : Color.white;
            }
            
            if (errorBg != null)
            {
                errorBg.color = showError ? Color.cyan : Color.white;
            }
            
            // 更新过滤器并获取新数据
            ConsoleManager.Instance.SetLogTypeFilter(showLog, showWarning, showError);
            logList = ConsoleManager.Instance.GetFilteredLogsForRefresh();
            
            if (isfilter && searchInput != null && !string.IsNullOrEmpty(searchInput.text))
            {
                // 使用循环代替 FindAll
                filterLogList.Clear();
                foreach (var log in logList)
                {
                    if (log.Message.Contains(searchInput.text, StringComparison.OrdinalIgnoreCase))
                    {
                        filterLogList.Add(log);
                    }
                }
            }
            
            RefreshLogList();
        }
        
        /// <summary>
        /// 搜索改变
        /// </summary>
        private void OnSearchChanged(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                isfilter = false;
                RefreshLogList();
                return;
            }

            isfilter = true;
            if (logList != null && logList.Count > 0)
            {
                // 使用循环代替 FindAll
                filterLogList.Clear();
                foreach (var log in logList)
                {
                    if (log.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        filterLogList.Add(log);
                    }
                }
            }
            RefreshLogList();
        }

        /// <summary>
        /// 清空按钮点击
        /// </summary>
        private void OnClearClicked()
        {
            ConsoleManager.Instance.Clear();
            logList = Array.Empty<LogEntry>();
            filterLogList.Clear();
            isfilter = false;
            Data = null;
            logCountText.text = "";
            RefreshLogList();
        }

        /// <summary>
        /// 保存按钮点击
        /// </summary>
        private void OnSaveClicked()
        {
            SaveLogsToFile();
        }

        /// <summary>
        /// 刷新日志列表
        /// </summary>
        private void RefreshLogList()
        {
            if (ScrollRect == null)
            {
                return;
            }

            if (isfilter)
            {
                ScrollRect.totalCount = filterLogList.Count;
            }
            else
            {
                ScrollRect.totalCount = logList.Count;
            }
            ScrollRect.RefillCells();
            UpdateStatistics();
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            if (logNumText != null)
            {
                if (ConsoleManager.Instance.LogCount > 999)
                {
                    logNumText.text = "999+";
                }
                else
                {
                    logNumText.text = $"{ConsoleManager.Instance.LogCount}";
                }
            }
            
            if(warningNumText != null)
            {
                if (ConsoleManager.Instance.WarningCount > 999)
                {
                    warningNumText.text = "999+";
                }
                else
                {
                    warningNumText.text = $"{ConsoleManager.Instance.WarningCount}";
                }
            }
            
            if(errorNumText != null)
            {
                if (ConsoleManager.Instance.ErrorCount > 999)
                {
                    errorNumText.text = "999+";
                }
                else
                {
                    errorNumText.text = $"{ConsoleManager.Instance.ErrorCount}";
                }
            }
        }

        /// <summary>
        /// 更新过滤Toggle状态
        /// </summary>
        private void UpdateFilterToggles()
        {
            ConsoleManager.Instance.GetLogTypeFilter(out bool showLog, out bool showWarning, out bool showError);

            if (logToggle != null)
            {
                logToggle.isOn = showLog;
            }

            if (warningToggle != null)
            {
                warningToggle.isOn = showWarning;
            }

            if (errorToggle != null)
            {
                errorToggle.isOn = showError;
            }
        }

        /// <summary>
        /// 保存日志到文件
        /// </summary>
        private void SaveLogsToFile()
        {
            string logText = ConsoleManager.Instance.ExportToText(true);
            string fileName = $"console_log_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                File.WriteAllText(filePath, logText);
                UnityEngine.Debug.Log($"日志已保存到: {filePath}");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"保存日志失败: {e.Message}");
            }
        }

        #region LoopScrollRect Implementation

        public override GameObject GetObject(int index)
        {
            GameObject itemObj;
            if (itemPool.Count > 0)
            {
                Transform trans = itemPool.Pop();
                itemObj = trans.gameObject;
                itemObj.SetActive(true);
            }
            else
            {
                itemObj = GameObject.Instantiate(item);
            }
            return itemObj;
        }

        public override void ReturnObject(Transform trans)
        {
            trans.gameObject.SetActive(false);
            trans.SetParent(transform, false);
            itemPool.Push(trans);
        }

        public override void ProvideData(Transform trans, int idx)
        {
            LogEntry log = null;
            if (isfilter)
            {
                log = filterLogList[idx];
            }
            else
            {
                log = logList[idx];
            }
            ConsoleLogItem logItem = trans.GetComponent<ConsoleLogItem>();
            if (logItem != null)
            {
                logItem.SetData(log, idx,OnClickItem);
            }
        }

        private void OnClickItem(LogEntry log)
        {
            logCountText.text = $"{log.Message}\n{log.StackTrace}";
        }

        #endregion
    }
}
#endif

