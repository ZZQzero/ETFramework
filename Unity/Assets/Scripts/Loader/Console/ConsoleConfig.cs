#if ENABLE_CONSOLE || DEVELOPMENT_BUILD
namespace ET
{
    /// <summary>
    /// Console配置类（可根据需要扩展）
    /// </summary>
    public static class ConsoleConfig
    {
        /// <summary>
        /// 默认快捷键
        /// </summary>
        public const string DEFAULT_TOGGLE_KEY = "BackQuote"; // ` 键

        /// <summary>
        /// 最大日志数量（可在ConsoleManager中修改）
        /// </summary>
        public const int MAX_LOG_COUNT = 1000;

        /// <summary>
        /// 默认对象池大小
        /// </summary>
        public const int DEFAULT_POOL_SIZE = 20;

        /// <summary>
        /// 日志保存路径（相对于Application.persistentDataPath）
        /// </summary>
        public const string LOG_SAVE_PATH = "";

        /// <summary>
        /// 日志文件名格式
        /// </summary>
        public const string LOG_FILE_NAME_FORMAT = "console_log_{0:yyyyMMdd_HHmmss}.txt";

        /// <summary>
        /// 是否在启动时自动启用Console
        /// </summary>
        public const bool AUTO_ENABLE_ON_START = true;

        /// <summary>
        /// 是否显示浮动按钮
        /// </summary>
        public const bool SHOW_FLOATING_BUTTON = false;

        /// <summary>
        /// 日志消息最大显示长度
        /// </summary>
        public const int MAX_MESSAGE_LENGTH = 200;

        /// <summary>
        /// 是否合并重复日志
        /// </summary>
        public const bool MERGE_REPEATED_LOGS = true;
    }
}
#endif