using UnityEditor;
using UnityEngine;

namespace ET
{
    public class ScriptingDefine
    {
        [MenuItem("ET/Add Scripting Define")]
        public static void AddScriptingDefine()
        {
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
    
            if (!currentDefines.Contains("ENABLE_CONSOLE"))
            {
                if (string.IsNullOrEmpty(currentDefines))
                {
                    currentDefines = "ENABLE_CONSOLE";
                }
                else
                {
                    currentDefines += ";ENABLE_CONSOLE";
                }
        
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, currentDefines);
                Debug.Log("已添加ENABLE_CONSOLE宏定义");
                EditorUtility.DisplayDialog("完成", "已添加 ENABLE_CONSOLE 宏定义\n\n请等待Unity重新编译...", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "ENABLE_CONSOLE 宏定义已存在", "确定");
            }
        }

        [MenuItem("ET/Remove Scripting Define (Release)")]
        public static void RemoveScriptingDefine()
        {
            if (EditorUtility.DisplayDialog("移除Console", 
                    "这将移除ENABLE_CONSOLE宏定义，\nConsole功能将被禁用。\n\n用于发布Release版本。", 
                    "确定", "取消"))
            {
                string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
                currentDefines = currentDefines.Replace("ENABLE_CONSOLE;", "").Replace(";ENABLE_CONSOLE", "").Replace("ENABLE_CONSOLE", "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, currentDefines);
                Debug.Log("已移除ENABLE_CONSOLE宏定义");
                EditorUtility.DisplayDialog("完成", "已移除 ENABLE_CONSOLE 宏定义\n\nConsole功能已禁用，适合Release版本。", "确定");
            }
        }
    }
}