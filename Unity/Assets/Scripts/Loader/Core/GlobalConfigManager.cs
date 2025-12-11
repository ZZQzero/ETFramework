using UnityEngine;

namespace ET
{
    public class GlobalConfigManager : Singleton<GlobalConfigManager>, ISingletonAwake
    {
        public GlobalConfig Config { set; get; }
        /// <summary>
        /// 程序集名字数组（与HybridCLR配置的热更新程序集保持一致）
        /// 注意：加载顺序很重要，必须先加载被依赖的程序集（Model），再加载依赖它的程序集（Hotfix）
        /// GameEntry放在最后，因为它依赖所有其他程序集，且在Awake中会立即使用它们的类型
        /// </summary>
        public static readonly string[] DllNames = { "ETClient.Model", "ETClient.ModelView", "ETClient.Hotfix", "ETClient.HotfixView", "GameEntry" };
        
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