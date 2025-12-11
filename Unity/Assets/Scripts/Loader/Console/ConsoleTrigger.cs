#if DEVELOPMENT_BUILD
using Cysharp.Threading.Tasks;
using UnityEngine;
using GameUI;

namespace ET
{
    /// <summary>
    /// Console触发器 - 快捷键和浮动按钮
    /// </summary>
    public class ConsoleTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("快捷键（默认F1）")]
        public KeyCode toggleKey = KeyCode.F1;
        private GameUIBase _loadUI;
        private void Awake()
        {
            // 设置为DontDestroyOnLoad
            DontDestroyOnLoad(gameObject);
        }

        private void OnGUI()
        {
            // 屏幕左上角绘制按钮
            if (GUI.Button(new Rect(10, 10, 100, 50), "Debug"))
            {
                ToggleConsole();
            }
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
            ConsoleManager.Instance.IsConsoleOpen = !ConsoleManager.Instance.IsConsoleOpen;
            if (ConsoleManager.Instance.IsConsoleOpen)
            {
                if(_loadUI == null)
                {
                    var parent = GameUIManager.Instance.GetUILayer(EGameUILayer.Mask);
                    var prefab = Resources.Load<GameObject>("UI/UIConsole");
                    if (prefab != null)
                    {
                        _loadUI = GameObject.Instantiate(prefab, parent).GetComponent<UIConsolePanel>();
                        _loadUI.Data = ConsoleManager.Instance.GetAllFilteredLogs();
                        _loadUI.OnInitUI();
                        _loadUI.OnOpenUI();
                    }
                }
                else
                {
                    GameUIManager.Instance.RefreshUI(_loadUI, ConsoleManager.Instance.GetAllFilteredLogs());
                }
                _loadUI.gameObject.SetActive(true);
            }
            else
            {
                if(_loadUI != null)
                {
                    _loadUI.OnCloseUI();
                }
            }
        }
        
        public GameUIBase GetUI() => _loadUI;
    }
}
#endif

