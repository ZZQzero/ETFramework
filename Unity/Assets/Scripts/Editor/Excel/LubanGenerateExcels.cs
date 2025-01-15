using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class LubanGenerateExcels : MonoBehaviour
{
    [MenuItem("Tools/导表工具",false, 1)]
    public static void GenerateExcelTools()
    {
        string path = Application.dataPath;

        path = path.Replace("Unity/Assets", "");
        path = Path.Combine(path, "Config");
        string rootPath = path;
        

        Process process = new Process();
        // 启动时候隐藏 terminal 界面
        // process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        // 设置 bat 文件的路径
        process.StartInfo.WorkingDirectory = rootPath;
        // 设置重定向标准错误流
        // process.StartInfo.RedirectStandardOutput = true;
        // process.StartInfo.UseShellExecute = false;
#if UNITY_EDITOR_WIN
        path = Path.Combine(rootPath, "gen.bat");
#elif UNITY_EDITOR_OSX
        path = Path.Combine(rootPath, "gen.sh");
#endif
        process.StartInfo.FileName = path;
        process.Start();
        process.WaitForExit();
        
        // 获取 bat 文件的完成状态
        int exitCode = process.ExitCode;
        
        // 检查 bat 文件的完成状态
        if (exitCode == 0)
        {
            //  bat 文件成功完成
            Debug.Log("Gen Excel Success");
            GenerateTDProxyFiles();
        }
        else
        {
            //  bat 文件失败完成
            Debug.LogError("Gen Excel Failed!!!! 【报错看下一行】");
            Debug.LogError(process.StandardOutput.ReadToEnd());
        }
        process.Close();
    }

    static void GenerateTDProxyFiles()
    {
        string csFilePath = Path.Combine(Application.dataPath, @"Scripts\Model\Excel\Generated");
        string proxyTemplateFilePath = Path.Combine(Application.dataPath.Replace("Unity/Assets",""), @"Config\TableConfigCategoryTemp.template");
        string templateContent = File.ReadAllText(proxyTemplateFilePath);
        string templateOutputPath = Path.Combine(Application.dataPath, @"Scripts\Model\Excel\Category");
        string templateOutputFileName = "#Name#ConfigCategory.cs";
        DirectoryInfo dirInfo = new DirectoryInfo(csFilePath);

        if (!Directory.Exists(templateOutputPath))
        {
            Directory.CreateDirectory(templateOutputPath);
        }
        foreach (FileInfo fileInfo in dirInfo.GetFiles())
        {
            // 获取文件名
            string fileName = fileInfo.Name;
            if (fileName.StartsWith("Tb") && fileName.EndsWith(".cs") && !fileName.Contains("TableData"))
            {
                string name = fileName.Replace("Tb","").Replace(".cs", "");
    
                string outputContent = templateContent.Replace("#Name#", name);
                string outputFileName = templateOutputFileName.Replace("#Name#", name);
                string outputPath = Path.Combine(templateOutputPath, outputFileName);

                if (!File.Exists(outputPath))
                {
                    File.Create(outputPath).Dispose();
                }
                File.WriteAllText(outputPath, outputContent, Encoding.UTF8);
                Debug.Log($"Generate TDProxy File :{outputFileName}");
            }
    
        }
    }
}
