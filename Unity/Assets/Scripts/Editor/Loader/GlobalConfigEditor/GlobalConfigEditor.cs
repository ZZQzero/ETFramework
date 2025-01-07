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
            GlobalConfig globalConfig = (GlobalConfig)this.target;
            //globalConfig.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
            SaveAsset(globalConfig);
        }

        public override void OnInspectorGUI()
        {
            GlobalConfig globalConfig = (GlobalConfig)this.target;
            CodeMode codeMode = (CodeMode)EditorGUILayout.EnumPopup("CodeMode", globalConfig.CodeMode);
            if (codeMode != globalConfig.CodeMode)
            {
                globalConfig.CodeMode = codeMode;
                AssetDatabase.Refresh();
            }
            
            EPlayMode playMode = (EPlayMode)EditorGUILayout.EnumPopup("PlayMode", globalConfig.PlayMode);
            if (playMode != globalConfig.PlayMode)
            {
                globalConfig.PlayMode = playMode;
                AssetDatabase.Refresh();
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
        }

        private void SaveAsset(UnityEngine.Object obj)
        {
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}