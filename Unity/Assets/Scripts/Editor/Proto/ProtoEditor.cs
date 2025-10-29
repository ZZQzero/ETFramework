using UnityEditor;
using System.Diagnostics;
using UnityEngine;

namespace ET
{
    public static class ProtoEditor
    {
        [MenuItem("ET/Proto/Proto2CS")]
        public static void Run()
        {
            string path = Application.dataPath.Replace("Unity/Assets", "DotNet/ET.Proto2CS/Exe/ET.Proto2CS.dll");
            
            Process process = ProcessHelper.DotNet(path, "./", true);

            UnityEngine.Debug.Log(process.StandardOutput.ReadToEnd());
        }
        
        public static void Init()
        {
            Run();
        }
    }
}