#if ENABLE_CONSOLE || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using GameUI;

namespace ET
{
    /// <summary>
    /// Console触发器 - 快捷键和浮动按钮
    /// </summary>
    public class ConsoleTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("快捷键（默认波浪号键 `）")]
        public KeyCode toggleKey = KeyCode.BackQuote;

        [Tooltip("是否显示浮动按钮")]
        public bool showFloatingButton = true;

        [Header("Floating Button")]
        public Button floatingButton;
        public GameObject floatingButtonPanel;
        private UIConsolePanel consolePanel;

        private bool isConsoleOpen = false;

        private void Awake()
        {
            // 设置为DontDestroyOnLoad
            DontDestroyOnLoad(gameObject);

            // 初始化浮动按钮
            if (floatingButton != null)
            {
                floatingButton.onClick.AddListener(ToggleConsole);
            }

            UpdateFloatingButtonVisibility();
        }

        private void Update()
        {
            // 检测快捷键
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleConsole();
            }
        }

        /// <summary>
        /// 切换Console显示
        /// </summary>
        private void ToggleConsole()
        {
            if (isConsoleOpen)
            {
                CloseConsole();
            }
            else
            {
                OpenConsole();
            }
        }

        /// <summary>
        /// 打开Console
        /// </summary>
        private void OpenConsole()
        {
            GameUIManager.Instance.OpenUI(LocalGameUIName.UIConsole, ConsoleManager.Instance.GetAllFilteredLogs()).Forget();
        }

        /// <summary>
        /// 关闭Console
        /// </summary>
        private void CloseConsole()
        {
            GameUIManager.Instance.CloseUI(LocalGameUIName.UIConsole);
            isConsoleOpen = false;
        }

        /// <summary>
        /// 更新浮动按钮可见性
        /// </summary>
        private void UpdateFloatingButtonVisibility()
        {
            if (floatingButtonPanel != null)
            {
                floatingButtonPanel.SetActive(showFloatingButton);
            }
        }

        /// <summary>
        /// 设置浮动按钮显示
        /// </summary>
        public void SetFloatingButtonVisible(bool visible)
        {
            showFloatingButton = visible;
            UpdateFloatingButtonVisibility();
        }
    }
}
#endif

