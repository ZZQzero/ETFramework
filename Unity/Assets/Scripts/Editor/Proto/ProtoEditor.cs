using UnityEditor;
using System.Diagnostics;
using UnityEngine;
using System.IO;

namespace ET
{
    public static class ProtoEditor
    {
        [MenuItem("ET/Proto/Proto2CS")]
        public static void Run()
        {
            string path = Application.dataPath.Replace("Unity/Assets", "DotNet/ET.Proto2CS/Exe/ET.Proto2CS.dll");
            // 显式指定工作目录为解决方案根目录，避免相对路径导致的重复“Unity”问题
            string solutionRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
          
            Process process = ProcessHelper.DotNet(path, solutionRoot, true);

            UnityEngine.Debug.Log(process.StandardOutput.ReadToEnd());
        }
    }
}