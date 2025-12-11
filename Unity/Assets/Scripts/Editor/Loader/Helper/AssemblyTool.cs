using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace ET
{
    public static class AssemblyTool
    {
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
            // 额外延迟一帧，确保所有程序集都已完全加载
            EditorApplication.delayCall += () =>
            {
                // 再次延迟，确保程序集完全初始化
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        GenerateEntity();
                    }
                    catch (Exception ex)
                    {
                        // 静默处理异常，避免影响Unity编辑器
                        Debug.LogWarning($"[AssemblyTool] OnAfterAssemblyReload 执行失败: {ex.Message}");
                    }
                };
            };
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
                //string sourcePdb = $"{hotUpdateDllDir}/{dllName}.pdb";
                
                if (!File.Exists(sourceDll))
                {
                    Debug.LogWarning($"热更DLL不存在: {sourceDll}，请先编译或使用HybridCLR生成");
                    continue;
                }
                
                File.Copy(sourceDll, $"{Define.CodeDir}/{dllName}.dll.bytes", true);
                
                /*if (File.Exists(sourcePdb))
                {
                    File.Copy(sourcePdb, $"{Define.CodeDir}/{dllName}.pdb.bytes", true);
                }*/
                Debug.Log($"Hotfix DLL 拷贝完成，{dllName}");
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
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("//-----------自动生成------------");
                sb.AppendLine("namespace ET");
                sb.AppendLine("{");
                sb.AppendLine("    public static partial class GameRegister");
                sb.AppendLine("    {");
                sb.AppendLine("        public static void RegisterEntitySystem()");
                sb.AppendLine("        {");

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    // 跳过动态程序集和系统程序集，避免类型加载问题
                    if (assembly.IsDynamic || assembly.FullName.StartsWith("System.") || assembly.FullName.StartsWith("Microsoft."))
                    {
                        continue;
                    }

                    try
                    {
                        // 安全地获取类型，处理 ReflectionTypeLoadException
                        Type[] types;
                        try
                        {
                            types = assembly.GetTypes();
                        }
                        catch (System.Reflection.ReflectionTypeLoadException ex)
                        {
                            // 如果某些类型加载失败，使用成功加载的类型
                            types = ex.Types.Where(t => t != null).ToArray();
                        }

                        foreach (var type in types)
                        {
                            // 跳过空类型（ReflectionTypeLoadException 可能返回 null）
                            if (type == null)
                            {
                                continue;
                            }

                            // 排除 abstract 类、接口
                            if (type.IsAbstract || type.IsInterface)
                            {
                                continue;
                            }

                            try
                            {
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
                            catch (Exception ex)
                            {
                                // 单个类型处理失败，记录日志但继续处理其他类型
                                Debug.LogWarning($"[AssemblyTool] 处理类型 {type?.FullName} 时出错: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // 单个程序集处理失败，记录日志但继续处理其他程序集
                        Debug.LogWarning($"[AssemblyTool] 处理程序集 {assembly.FullName} 时出错: {ex.Message}");
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
                    Debug.Log($"EntitySystemRegisterAll generated at {Define.EntitySystemRegisterDir}");
                }
            }
            catch (Exception ex)
            {
                // 整体失败时记录错误，但不抛出异常，避免影响其他编辑器功能
                Debug.LogError($"[AssemblyTool] GenerateEntity 失败: {ex}");
            }
        }
    }
}