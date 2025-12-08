using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using YooAsset;

namespace ET
{
    [CustomEditor(typeof(GlobalConfig))]
    public class GlobalConfigEditor : Editor
    {
        private void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            GlobalConfig globalConfig = (GlobalConfig)this.target;
            CodeMode codeMode = (CodeMode)EditorGUILayout.EnumPopup("CodeMode", globalConfig.CodeMode);
            if (codeMode != globalConfig.CodeMode)
            {
                globalConfig.CodeMode = codeMode;
                SaveAsset(globalConfig);
                // 使用延迟调用避免阻塞UI，CodeMode改变可能需要重新编译
                EditorApplication.delayCall += () => AssetDatabase.Refresh();
            }
            
            EPlayMode playMode = (EPlayMode)EditorGUILayout.EnumPopup("PlayMode", globalConfig.PlayMode);
            if (playMode != globalConfig.PlayMode)
            {
                globalConfig.PlayMode = playMode;
                SaveAsset(globalConfig);
                // 使用延迟调用避免阻塞UI，PlayMode改变可能需要刷新资源
                EditorApplication.delayCall += () => AssetDatabase.Refresh();
            }
            
            string version = EditorGUILayout.TextField($"Version", globalConfig.Version);
            if (version != globalConfig.Version)
            {
                globalConfig.Version = version;
                SaveAsset(globalConfig);
            }
            
            string sceneName = EditorGUILayout.TextField($"SceneName", globalConfig.SceneName);
            if (sceneName != globalConfig.SceneName)
            {
                globalConfig.SceneName = sceneName;
                SaveAsset(globalConfig);
            }
            
            string package = EditorGUILayout.TextField($"PackageName", globalConfig.PackageName);
            if (package != globalConfig.PackageName)
            {
                globalConfig.PackageName = package;
                SaveAsset(globalConfig);
            }
            
            string address = EditorGUILayout.TextField($"Address", globalConfig.IPAddress);
            if (address != globalConfig.IPAddress)
            {
                globalConfig.IPAddress = address;
                SaveAsset(globalConfig);
            }
            
            string resourcePath = EditorGUILayout.TextField($"ResourcePath", globalConfig.ResourcePath);
            if (resourcePath != globalConfig.ResourcePath)
            {
                globalConfig.ResourcePath = resourcePath;
                SaveAsset(globalConfig);
            }
            
            bool isDev = EditorGUILayout.Toggle($"IsDevelop", globalConfig.IsDevelop);
            if (isDev != globalConfig.IsDevelop)
            {
                globalConfig.IsDevelop = isDev;
                SaveAsset(globalConfig);
            }
        }

        private void SaveAsset(UnityEngine.Object obj)
        {
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }
    }
}