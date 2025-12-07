#if ENABLE_CONSOLE || DEVELOPMENT_BUILD
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
        public TextMeshProUGUI logCountText;

        [Header("Item Pool")]
        private Stack<Transform> itemPool = new Stack<Transform>();

        /// <summary>
        /// 日志数据列表
        /// </summary>
        private List<LogEntry> logList = new List<LogEntry>();

        private bool isInitialized = false;

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
                closeButton.onClick.AddListener(CloseSelf);
            }

            /*// 注册Console事件
            ConsoleManager.Instance.OnLogAdded += OnLogAdded;
            ConsoleManager.Instance.OnLogCleared += OnLogCleared;*/

            isInitialized = true;

            // 初始化过滤状态
            UpdateFilterToggles();
            
            // 刷新列表
            RefreshLogList();
        }

        public override void OnOpenUI()
        {
            base.OnOpenUI();
            
            // 从Data获取日志列表（打开UI时应该通过Data传递）
            if (Data is List<LogEntry> data)
            {
                logList = data;
            }
            else
            {
                // 如果Data为null，清空列表（不应该发生，但作为容错处理）
                logList.Clear();
            }
            
            if (isInitialized)
            {
                RefreshLogList();
            }
        }

        public override void OnCloseUI()
        {
            base.OnCloseUI();
        }

        public override void OnRefreshUI()
        {
            base.OnRefreshUI();
            
            // 从Data获取最新的日志数据（通过RefreshUI传递）
            if (Data is List<LogEntry> data)
            {
                logList = data;
            }
            else
            {
                // 如果Data为null，从ConsoleManager获取最新过滤后的日志
                logList = ConsoleManager.Instance.GetFilteredLogsForRefresh();
            }
            
            // 刷新列表
            RefreshLogList();
        }

        public override void OnDestroyUI()
        {
            base.OnDestroyUI();

            // 取消注册事件
            if (ConsoleManager.Instance != null)
            {
                ConsoleManager.Instance.OnLogAdded -= OnLogAdded;
                ConsoleManager.Instance.OnLogCleared -= OnLogCleared;
            }
        }

        /// <summary>
        /// 日志添加回调
        /// </summary>
        private void OnLogAdded()
        {
            // 通过RefreshUI刷新数据，保持数据获取的一致性
            var newData = ConsoleManager.Instance.GetFilteredLogsForRefresh();
            GameUIManager.Instance.RefreshUI(UIName, newData);
            UpdateStatistics();
        }

        /// <summary>
        /// 日志清空回调
        /// </summary>
        private void OnLogCleared()
        {
            // 通过RefreshUI刷新数据，传递空列表
            var emptyData = new List<LogEntry>();
            GameUIManager.Instance.RefreshUI(UIName, emptyData);
            UpdateStatistics();
        }

        /// <summary>
        /// 过滤改变
        /// </summary>
        private void OnFilterChanged(bool value)
        {
            bool showLog = logToggle != null && logToggle.isOn;
            bool showWarning = warningToggle != null && warningToggle.isOn;
            bool showError = errorToggle != null && errorToggle.isOn;

            ConsoleManager.Instance.SetLogTypeFilter(showLog, showWarning, showError);
            // 通过RefreshUI刷新数据，保持数据获取的一致性
            var newData = ConsoleManager.Instance.GetFilteredLogsForRefresh();
            GameUIManager.Instance.RefreshUI(UIName, newData);
        }

        /// <summary>
        /// 搜索改变
        /// </summary>
        private void OnSearchChanged(string searchText)
        {
            ConsoleManager.Instance.SetSearchText(searchText);
            // 通过RefreshUI刷新数据，保持数据获取的一致性
            var newData = ConsoleManager.Instance.GetFilteredLogsForRefresh();
            GameUIManager.Instance.RefreshUI(UIName, newData);
        }

        /// <summary>
        /// 清空按钮点击
        /// </summary>
        private void OnClearClicked()
        {
            ConsoleManager.Instance.Clear();
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

            // 使用本地日志列表的数量
            ScrollRect.totalCount = logList.Count;
            ScrollRect.RefillCells();

            UpdateStatistics();
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            if (logCountText != null)
            {
                logCountText.text = $"{ConsoleManager.Instance.LogCount}/{ConsoleManager.Instance.WarningCount}/{ConsoleManager.Instance.ErrorCount}";
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
            // 从本地日志列表中获取数据
            if (idx >= 0 && idx < logList.Count)
            {
                LogEntry log = logList[idx];
                ConsoleLogItem logItem = trans.GetComponent<ConsoleLogItem>();
                if (logItem != null)
                {
                    logItem.SetData(log, idx,OnClickItem);
                }
            }
        }

        private void OnClickItem(string log)
        {
            logCountText.text = log;
        }

        #endregion
    }
}
#endif

