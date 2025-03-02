﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace ET
{
    public class OnGenerateCSProjectProcessor: AssetPostprocessor
    {
        /// <summary>
        /// 对生成的C#项目文件(.csproj)进行处理
        /// 文档:https://learn.microsoft.com/zh-cn/visualstudio/gamedev/unity/extensibility/customize-project-files-created-by-vstu#%E6%A6%82%E8%A7%88
        /// </summary>
        /*public static string OnGeneratedCSProject(string path, string content)
        {
            GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
            // 判空原因:初次打开工程时会加载失败, 因为此时Unity的资源数据库(AssetDatabase)还未完成初始化
            BuildType buildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
            if (buildType == BuildType.Release)
            {
                content = content.Replace("<Optimize>false</Optimize>", "<Optimize>true</Optimize>");
                content = content.Replace(";DEBUG;", ";");
            }

            if (path.EndsWith("ETClient.Core.csproj"))
            {
                return GenerateCustomProject(content, globalConfig.CodeMode, "ETClient.Core");
            }
            
            if (path.EndsWith("ETClient.Model.csproj"))
            {
                return GenerateCustomProject(content, globalConfig.CodeMode, "ETClient.Model");
            }
            
            if (path.EndsWith("ETClient.Hotfix.csproj"))
            {
                return GenerateCustomProject(content, globalConfig.CodeMode, "ETClient.Hotfix");
            }

            if (path.EndsWith("ETClient.ModelView.csproj"))
            {
                return GenerateCustomProject(content, globalConfig.CodeMode, "ETClient.ModelView");
            }
            if (path.EndsWith("ETClient.HotfixView.csproj"))
            {
                return GenerateCustomProject(content, globalConfig.CodeMode, "ETClient.HotfixView");
            }

            return content;
        }*/

        /// <summary>
        /// 对生成的解决方案文件(.sln)进行处理, 此处主要为了隐藏一些没有作用的C#项目
        /// </summary>
        //public static string OnGeneratedSlnSolution(string _, string content)
        //{
        //    content = Regex.Replace(content, $"Project.*Ignore\\.ET.*\r\nEndProject", string.Empty);
        //    content = Regex.Replace(content, $"Project.*Ignore\\.ET.*\nEndProject", string.Empty);
        //    return content;
        //}

        /// <summary>
        /// 自定义C#项目配置
        /// 参考链接:
        /// https://zhuanlan.zhihu.com/p/509046784
        /// https://learn.microsoft.com/zh-cn/visualstudio/ide/reference/build-events-page-project-designer-csharp?view=vs-2022
        /// https://learn.microsoft.com/zh-cn/visualstudio/ide/how-to-specify-build-events-csharp?view=vs-2022
        /// </summary>
        /*static string GenerateCustomProject(string content, CodeMode codeMode, string dllName = "")
        {
            /*XmlDocument doc = new();
            doc.LoadXml(content);
            var newDoc = doc.Clone() as XmlDocument;
            var rootNode = newDoc.GetElementsByTagName("Project")[0];
            bool isSourcesGenerator = false;
            var itemGroup = newDoc.GetElementsByTagName("ItemGroup")[0];
            foreach (var item in itemGroup.ChildNodes)
            {
                var element = item as XmlElement;
                var str = element.GetAttribute("Include");
                if (str.EndsWith("ET.SourceGenerator.dll"))
                {
                    isSourcesGenerator = true;
                }
            }

            if (!isSourcesGenerator)
            {
                XmlElement newElement = newDoc.CreateElement("Analyzer", newDoc.DocumentElement.NamespaceURI);
               newElement.SetAttribute("Include", @"..\Unity\Assets\Plugins\ET.SourceGenerator.dll"); 
               itemGroup.AppendChild(newElement);
            }#1#

            /#1#/ AfterBuild(字符串替换后作用是编译后复制到CodeDir)
            {
                string afterBuild =
                        $"    <Copy SourceFiles=\"$(TargetDir)/$(TargetName).dll\" DestinationFiles=\"$(ProjectDir)/{Define.CodeDir}/$(TargetName).dll.bytes\" ContinueOnError=\"false\" />\n" +
                        $"    <Copy SourceFiles=\"$(TargetDir)/$(TargetName).pdb\" DestinationFiles=\"$(ProjectDir)/{Define.CodeDir}/$(TargetName).pdb.bytes\" ContinueOnError=\"false\" />\n" +
                        $"    <Copy SourceFiles=\"$(TargetDir)/$(TargetName).dll\" DestinationFiles=\"$(ProjectDir)/{Define.BuildOutputDir}/$(TargetName).dll\" ContinueOnError=\"false\" />\n" +
                        $"    <Copy SourceFiles=\"$(TargetDir)/$(TargetName).pdb\" DestinationFiles=\"$(ProjectDir)/{Define.BuildOutputDir}/$(TargetName).pdb\" ContinueOnError=\"false\" />\n";
                switch (dllName)
                {
                    case "ETClient.Model":
                    case "ETClient.Hotfix":
                    case "ETClient.HotfixView":
                    case "ETClient.ModelView":
                    {
                        var target = newDoc.CreateElement("Target", newDoc.DocumentElement.NamespaceURI);
                        target.SetAttribute("Name", "AfterBuild");
                        target.InnerXml = afterBuild;
                        rootNode.AppendChild(target);
                        break;
                    }
                }
            }#1#

            /*using StringWriter sw = new();
            using XmlTextWriter tx = new(sw);
            tx.Formatting = Formatting.Indented;
            newDoc.WriteTo(tx);
            tx.Flush();
            return sw.GetStringBuilder().ToString();#1#
        }*/
    }
}