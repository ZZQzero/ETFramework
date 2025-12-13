using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 配置文件生成工具
    /// 在打包时自动生成 config.json 到 StreamingAssets 目录
    /// </summary>
    public static class ConfigJsonGenerator
    {
        private const string ConfigFileName = "config.json";
        private const string StreamingAssetsPath = "Assets/StreamingAssets";
        
        /// <summary>
        /// 生成 config.json 文件到 StreamingAssets 目录
        /// </summary>
        [MenuItem("ET/Loader/生成 config.json", false, ETMenuItemPriority.BuildTool + 2)]
        public static void GenerateConfigJson()
        {
            try
            {
                // 确保 StreamingAssets 目录存在
                if (!Directory.Exists(StreamingAssetsPath))
                {
                    Directory.CreateDirectory(StreamingAssetsPath);
                    AssetDatabase.Refresh();
                }
                
                // 从 GlobalConfig 读取配置
                GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
                if (globalConfig == null)
                {
                    EditorUtility.DisplayDialog("错误", "无法加载 GlobalConfig，请先创建 GlobalConfig 资源", "确定");
                    return;
                }
                
                // 创建 ClientConfig 对象
                GlobalStartConfig clientConfig = new GlobalStartConfig();
                clientConfig.Version = globalConfig.Version;
                clientConfig.ResourcePath = globalConfig.ResourcePath;
                clientConfig.IsDevelop = globalConfig.IsDevelop;
                
                // 序列化为 JSON
                string jsonContent = JsonUtility.ToJson(clientConfig, true);
                // 写入文件
                string configPath = Path.Combine(StreamingAssetsPath, ConfigFileName);
                File.WriteAllText(configPath, jsonContent);
                
                // 刷新资源数据库
                AssetDatabase.Refresh();
                
                Debug.Log($"成功生成 config.json: {configPath}\n内容:\n{jsonContent}");
                EditorUtility.DisplayDialog("成功", $"已生成 config.json 到:\n{configPath}", "确定");
            }
            catch (System.Exception e)
            {
                string errorMsg = $"生成 config.json 失败: {e.Message}";
                Debug.LogError($"{errorMsg}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("错误", errorMsg, "确定");
            }
        }
        
        // 构建前自动生成 config.json 的功能已通过 PreprocessBuildConfig 类实现
        // 该类实现了 IPreprocessBuildWithReport 接口，会在构建前自动调用
        
        /// <summary>
        /// 内部方法：生成 config.json（不显示对话框）
        /// </summary>
        public static void GenerateConfigJsonInternal()
        {
            try
            {
                // 确保 StreamingAssets 目录存在
                if (!Directory.Exists(StreamingAssetsPath))
                {
                    Directory.CreateDirectory(StreamingAssetsPath);
                }
                
                // 从 GlobalConfig 读取配置
                GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
                if (globalConfig == null)
                {
                    Debug.LogWarning("无法加载 GlobalConfig，跳过生成 config.json");
                    return;
                }

                GlobalStartConfig clientConfig = new GlobalStartConfig();
                clientConfig.Version = globalConfig.Version;
                clientConfig.ResourcePath = globalConfig.ResourcePath;
                clientConfig.IsDevelop = globalConfig.IsDevelop;
                // 序列化为 JSON
                string jsonContent = JsonUtility.ToJson(clientConfig, true);
                
                // 写入文件
                string configPath = Path.Combine(StreamingAssetsPath, ConfigFileName);
                File.WriteAllText(configPath, jsonContent);
                
                Debug.Log($"[构建前] 自动生成 config.json: {configPath}, 版本号: {clientConfig.Version}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"构建前生成 config.json 失败: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
