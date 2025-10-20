using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Build.Player;
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

        /// <summary>
        /// 程序集名字数组
        /// </summary>
        public static readonly string[] DllNames = { "ETClient.Hotfix", "ETClient.HotfixView", "ETClient.Model", "ETClient.ModelView" };

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            //unitySynchronizationContext = SynchronizationContext.Current;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(object obj)
        {
            GenerateEntity();
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
            
            Log.Info($"Compile Finish!");
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
        /// 将dll文件复制到加载目录
        /// </summary>
        static void CopyHotUpdateDlls()
        {
            FileHelper.CleanDirectory(Define.CodeDir);
            foreach (string dllName in DllNames)
            {
                string sourceDll = $"{Define.BuildOutputDir}/{dllName}.dll";
                string sourcePdb = $"{Define.BuildOutputDir}/{dllName}.pdb";
                File.Copy(sourceDll, $"{Define.CodeDir}/{dllName}.dll.bytes", true);
                File.Copy(sourcePdb, $"{Define.CodeDir}/{dllName}.pdb.bytes", true);
            }

            AssetDatabase.Refresh();
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

            File.WriteAllText(Define.EntitySystemRegisterDir, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            Debug.Log($"EntitySystemRegisterAll generated at {Define.EntitySystemRegisterDir}");
        }
    }
}