using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace ET
{
    public static class AssemblyTool
    {
        /// <summary>
        /// Unity线程的同步上下文
        /// </summary>
        static SynchronizationContext unitySynchronizationContext { get; set; }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
        }

        private static void OnCompilationStarted(object obj)
        {
            if (Application.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }
        }

        [DidReloadScripts]
        public static void OnAfterAssemblyReload()
        {
            // 使用延迟调用，避免在编译过程中立即触发重新编译
            EditorApplication.delayCall += GenerateEntity;
        }

        /// <summary>
        /// 菜单和快捷键编译按钮
        /// </summary>
        [MenuItem("ET/Loader/Compile _F6", false, ETMenuItemPriority.Compile)]
        static void MenuItemOfCompile()
        {
            // 强制刷新一下，防止关闭auto refresh，文件修改时间不准确
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            DoCompile();
        }

        /// <summary>
        /// 菜单和快捷键热重载按钮
        /// </summary>
        [MenuItem("ET/Loader/Reload _F7", false, ETMenuItemPriority.Compile)]
        static void MenuItemOfReload()
        {
            if (Application.isPlaying)
            {
                //CodeLoader.Instance?.Reload();
            }
        }

        /// <summary>
        /// 执行编译代码流程
        /// </summary>
        public static void DoCompile()
        {
            // 强制刷新一下，防止关闭auto refresh，编译出老代码
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            /*GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
            CodeModeChangeHelper.ChangeToCodeMode(globalConfig.CodeMode.ToString());*/

            bool isCompileOk = CompileDlls();
            if (!isCompileOk)
            {
                return;
            }

            //CopyHotUpdateDlls();
            
            Debug.Log($"Compile Finish!");
        }

        /// <summary>
        /// 编译成dll
        /// </summary>
        static bool CompileDlls()
        {
            // 运行时编译需要先设置为UnitySynchronizationContext, 编译完再还原为CurrentContext
            SynchronizationContext lastSynchronizationContext = Application.isPlaying ? SynchronizationContext.Current : null;
            SynchronizationContext.SetSynchronizationContext(unitySynchronizationContext);

            bool isCompileOk = false;

            try
            {
                Directory.CreateDirectory(Define.BuildOutputDir);
                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
                ScriptCompilationSettings scriptCompilationSettings = new()
                {
                    group = group,
                    target = target,
                    extraScriptingDefines = new[] { "IS_COMPILING" },
                    options = EditorUserBuildSettings.development ? ScriptCompilationOptions.DevelopmentBuild : ScriptCompilationOptions.None
                };
                ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(scriptCompilationSettings, Define.BuildOutputDir);
                isCompileOk = result.assemblies.Count > 0;
                EditorUtility.ClearProgressBar();
            }
            finally
            {
                if (lastSynchronizationContext != null)
                {
                    SynchronizationContext.SetSynchronizationContext(lastSynchronizationContext);
                }
            }

            return isCompileOk;
        }

        /// <summary>
        /// 将热更dll文件复制到加载目录
        /// </summary>
        [MenuItem("ET/Loader/CopyHotUpdateDlls _F8")]
        public static void CopyHotUpdateDlls()
        {
            // 根据当前构建目标获取HybridCLR的输出目录
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string platformName = GetPlatformName(target);
            string hotUpdateDllDir = $"HybridCLRData/HotUpdateDlls/{platformName}";
            
            FileHelper.CleanDirectory(Define.CodeDir);
            
            foreach (string dllName in GlobalConfigManager.DllNames)
            {
                string sourceDll = $"{hotUpdateDllDir}/{dllName}.dll";
                string sourcePdb = $"{hotUpdateDllDir}/{dllName}.pdb";
                
                if (!File.Exists(sourceDll))
                {
                    Debug.LogWarning($"热更DLL不存在: {sourceDll}，请先编译或使用HybridCLR生成");
                    continue;
                }
                
                File.Copy(sourceDll, $"{Define.CodeDir}/{dllName}.dll.bytes", true);
                
                if (File.Exists(sourcePdb))
                {
                    File.Copy(sourcePdb, $"{Define.CodeDir}/{dllName}.pdb.bytes", true);
                }
            }

            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// 将 AOT 补充元数据 DLL 复制到加载目录
        /// </summary>
        [MenuItem("ET/Loader/CopyAOTDlls")]
        public static void CopyAOTDlls()
        {
            // 根据当前构建目标获取HybridCLR的AOT输出目录
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string platformName = GetPlatformName(target);
            string aotDllDir = $"HybridCLRData/AssembliesPostIl2CppStrip/{platformName}";
            
            Directory.CreateDirectory(Define.AOTDllDir);
            FileHelper.CleanDirectory(Define.AOTDllDir);
            
            foreach (string aotDllName in GlobalConfigManager.AOTDllNames)
            {
                string sourceDll = $"{aotDllDir}/{aotDllName}.dll";
                
                if (!File.Exists(sourceDll))
                {
                    Debug.LogWarning($"AOT DLL不存在: {sourceDll}，请先使用HybridCLR生成");
                    continue;
                }
                
                // 直接拷贝 DLL 文件，不需要 .bytes 后缀（因为使用 PackRawFile）
                File.Copy(sourceDll, $"{Define.AOTDllDir}/{aotDllName}.dll.bytes", true);
                Debug.Log($"拷贝 AOT DLL: {aotDllName}");
            }

            AssetDatabase.Refresh();
            Debug.Log($"AOT DLL 拷贝完成，共 {GlobalConfigManager.AOTDllNames.Length} 个文件");
        }
        
        /// <summary>
        /// 将BuildTarget转换为HybridCLR平台目录名
        /// </summary>
        static string GetPlatformName(BuildTarget target)
        {
            return target switch
            {
                BuildTarget.StandaloneWindows => "StandaloneWindows",
                BuildTarget.StandaloneWindows64 => "StandaloneWindows64",
                BuildTarget.StandaloneOSX => "StandaloneOSX",
                BuildTarget.StandaloneLinux64 => "StandaloneLinux64",
                BuildTarget.Android => "Android",
                BuildTarget.iOS => "iOS",
                BuildTarget.WebGL => "WebGL",
                _ => target.ToString()
            };
        }
        
        [MenuItem("ET/Loader/生成EntitySystem注册代码")]
        public static void GenerateEntity()
        {
            var sb = new StringBuilder();
            sb.AppendLine("//-----------自动生成------------");
            sb.AppendLine("namespace ET");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class GameRegister");
            sb.AppendLine("    {");
            sb.AppendLine("        public static void RegisterEntitySystem()");
            sb.AppendLine("        {");

            var assemblis = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblis)
            {
                foreach (var type in assembly.GetTypes())
                {
                    // 排除 abstract 类、接口
                    if (type.IsAbstract || type.IsInterface)
                    {
                        continue;
                    }

                    // 找到标记 [EntitySystem] 的类
                    var hasAttr = type.GetCustomAttributes(false).Any(a => a.GetType().Name == "EntitySystemAttribute");
                    if (!hasAttr)
                    {
                        continue;
                    }

                    // 修正内部类名称，用 . 替代 +
                    var typeName = type.FullName.Replace('+', '.');
                    sb.AppendLine($"            EntitySystemSingleton.RegisterEntitySystem<{typeName}>();");
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            // 保存到文件
            var dir = Path.GetDirectoryName(Define.EntitySystemRegisterDir);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string newContent = sb.ToString();
            string filePath = Define.EntitySystemRegisterDir;
            
            // 检查文件是否存在，以及内容是否真的有变化
            bool needWrite = true;
            if (File.Exists(filePath))
            {
                string oldContent = File.ReadAllText(filePath, Encoding.UTF8);
                // 如果内容相同，不写入文件，避免触发重新编译
                if (oldContent == newContent)
                {
                    needWrite = false;
                }
            }
            
            // 只有内容真的有变化时才写入文件并刷新
            if (needWrite)
            {
                File.WriteAllText(filePath, newContent, Encoding.UTF8);
                AssetDatabase.Refresh();
            }

            Debug.Log($"EntitySystemRegisterAll generated at {Define.EntitySystemRegisterDir}");
        }
    }
}