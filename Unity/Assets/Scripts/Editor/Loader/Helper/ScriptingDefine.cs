using UnityEditor;
using UnityEngine;

namespace ET
{
    public class ScriptingDefine
    {
        [MenuItem("ET/ScriptingDefine/Add Scripting Define")]
        public static void AddScriptingDefine()
        {
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
    
            if (!currentDefines.Contains("DEVELOPMENT_BUILD"))
            {
                if (string.IsNullOrEmpty(currentDefines))
                {
                    currentDefines = "DEVELOPMENT_BUILD";
                }
                else
                {
                    currentDefines += ";DEVELOPMENT_BUILD";
                }
        
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, currentDefines);
                Debug.Log("已添加DEVELOPMENT_BUILD宏定义");
                EditorUtility.DisplayDialog("完成", "已添加 DEVELOPMENT_BUILD 宏定义\n\n请等待Unity重新编译...", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "DEVELOPMENT_BUILD 宏定义已存在", "确定");
            }
        }

        [MenuItem("ET/ScriptingDefine/Remove Scripting Define (Release)")]
        public static void RemoveScriptingDefine()
        {
            if (EditorUtility.DisplayDialog("移除Console", 
                    "这将移除DEVELOPMENT_BUILD宏定义，\nConsole功能将被禁用。\n\n用于发布Release版本。", 
                    "确定", "取消"))
            {
                string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
                currentDefines = currentDefines.Replace("DEVELOPMENT_BUILD;", "").Replace(";DEVELOPMENT_BUILD", "").Replace("DEVELOPMENT_BUILD", "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, currentDefines);
                Debug.Log("已移除ENABLE_CONSOLE宏定义");
                EditorUtility.DisplayDialog("完成", "已移除 DEVELOPMENT_BUILD 宏定义\n\nConsole功能已禁用，适合Release版本。", "确定");
            }
        }
    }
}