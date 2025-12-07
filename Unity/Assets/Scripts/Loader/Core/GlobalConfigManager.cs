using UnityEngine;

namespace ET
{
    public class GlobalConfigManager : Singleton<GlobalConfigManager>, ISingletonAwake
    {
        public GlobalConfig Config { set; get; }
        /// <summary>
        /// 程序集名字数组（与HybridCLR配置的热更新程序集保持一致）
        /// </summary>
        public static readonly string[] DllNames = { "GameEntry", "ETClient.Hotfix", "ETClient.HotfixView", "ETClient.Model", "ETClient.ModelView" };
        
        /// <summary>
        /// AOT 程序集名字数组（用于补充元数据，与 AOTGenericReferences.PatchedAOTAssemblyList 保持一致）
        /// 注意：需要与 HybridCLR 生成的 AOTGenericReferences.cs 中的列表同步
        /// </summary>
        public static readonly string[] AOTDllNames = new[]
        {
            "ETClient.Core",
            "Luban.Runtime",
            "Nino.Core",
            "System.Core",
            "System.Runtime.CompilerServices.Unsafe",
            "System",
            "UnityEngine.CoreModule",
            "YooAsset",
            "mscorlib",
        };
        public void Awake()
        {
            Config = Resources.Load<GlobalConfig>("GlobalConfig");
        }

        protected override void Destroy()
        {
            base.Destroy();
            Config = null;
        }
    }
}