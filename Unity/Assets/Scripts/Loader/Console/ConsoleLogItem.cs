using System;
using UnityEngine;
using UnityEngine.UI;

namespace ET
{
	public class ConsoleLogItem : MonoBehaviour
	{
		#region Auto Generate Code
		[SerializeField] private UnityEngine.UI.Button consoleLogItemButton;
		[SerializeField] private TMPro.TextMeshProUGUI timeTextTextMeshProUGUI;
		[SerializeField] private TMPro.TextMeshProUGUI messageTextTextMeshProUGUI;
		[SerializeField] private TMPro.TextMeshProUGUI countTextTextMeshProUGUI;
		[SerializeField] private UnityEngine.GameObject stackTracePanelGameObject;
		[SerializeField] private TMPro.TextMeshProUGUI stackTraceTextTextMeshProUGUI;
		[SerializeField] private Image typeIcon;
		#endregion Auto Generate Code
		
		[Header("Colors")]
		public Color logColor = Color.white;
		public Color warningColor = Color.yellow;
		public Color errorColor = Color.red;

		[Header("Icons")]
		public Sprite logIcon;
		public Sprite warningIcon;
		public Sprite errorIcon;

		private LogEntry currentLog;
		private int currentIndex;
        private Action<string> clickAction;
		
		 private void Awake()
        {
            if (consoleLogItemButton != null)
            {
                consoleLogItemButton.onClick.AddListener(OnItemClicked);
            }
        }

        /// <summary>
        /// 设置日志数据
        /// </summary>
        public void SetData(LogEntry log, int index,Action<string> call)
        {
            currentLog = log;
            currentIndex = index;
            clickAction = call;
            if (log == null)
            {
                return;
            }

            // 设置时间
            if (timeTextTextMeshProUGUI != null)
            {
                timeTextTextMeshProUGUI.text = log.Timestamp.ToString("HH:mm:ss");
            }

            // 设置消息
            if (messageTextTextMeshProUGUI != null)
            {
                messageTextTextMeshProUGUI.text = log.GetShortMessage(200);
            }

            // 设置计数
            if (countTextTextMeshProUGUI != null)
            {
                if (log.Count > 1)
                {
                    countTextTextMeshProUGUI.gameObject.SetActive(true);
                    countTextTextMeshProUGUI.text = $"x{log.Count}";
                }
                else
                {
                    countTextTextMeshProUGUI.gameObject.SetActive(false);
                }
            }

            // 设置类型图标和颜色
            Color typeColor = logColor;
            Sprite icon = logIcon;

            switch (log.LogType)
            {
                case ConsoleLogType.Warning:
                    typeColor = warningColor;
                    icon = warningIcon;
                    break;
                case ConsoleLogType.Error:
                    typeColor = errorColor;
                    icon = errorIcon;
                    break;
                case ConsoleLogType.Log:
                default:
                    typeColor = logColor;
                    icon = logIcon;
                    break;
            }

            if (typeIcon != null)
            {
                typeIcon.sprite = icon;
                typeIcon.color = typeColor;
            }

            if (messageTextTextMeshProUGUI != null)
            {
                messageTextTextMeshProUGUI.color = typeColor;
            }

            // 设置堆栈跟踪
            UpdateStackTraceDisplay();
        }

        /// <summary>
        /// 更新堆栈跟踪显示
        /// </summary>
        private void UpdateStackTraceDisplay()
        {
            if (currentLog == null)
            {
                return;
            }

            bool hasStackTrace = !string.IsNullOrEmpty(currentLog.StackTrace);
            bool isExpanded = currentLog.IsExpanded;

            if (stackTracePanelGameObject != null)
            {
                stackTracePanelGameObject.SetActive(hasStackTrace && isExpanded);
            }

            if (stackTraceTextTextMeshProUGUI != null && hasStackTrace && isExpanded)
            {
                stackTraceTextTextMeshProUGUI.text = currentLog.StackTrace;
            }
        }

        /// <summary>
        /// 点击Item
        /// </summary>
        private void OnItemClicked()
        {
            if (currentLog == null)
            {
                return;
            }

            // 切换展开状态
            //ConsoleManager.Instance.ToggleLogExpanded(currentIndex);
            //UpdateStackTraceDisplay();

            // 复制到剪贴板
            string logText = currentLog.FormatOutput(true);
            GUIUtility.systemCopyBuffer = logText;
            clickAction?.Invoke(currentLog.StackTrace);
            UnityEngine.Debug.Log("日志已复制到剪贴板");
        }

        /// <summary>
        /// 刷新显示（用于展开状态改变）
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateStackTraceDisplay();
        }
	}
}
