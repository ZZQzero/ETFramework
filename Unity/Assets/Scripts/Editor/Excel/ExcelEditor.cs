using UnityEditor;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ET
{
    public static class ExcelEditor
    {
        [MenuItem("ET/Excel/ExcelExporter")]
        public static void Run()
        {
            string path = Application.dataPath.Replace("Unity/Assets", "DotNet/ET.ExcelExporter/Exe/ET.ExcelExporter.dll");
            
            Process process = ProcessHelper.DotNet(path, "./", true);

            UnityEngine.Debug.Log(process.StandardOutput.ReadToEnd());
        }

        public static void Init()
        {
            Run();
        }
    }
}