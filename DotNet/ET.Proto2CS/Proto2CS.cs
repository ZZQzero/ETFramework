using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ET
{
    internal class OpcodeInfo
    {
        public string Name;
        public int Opcode;
    }

    public static class Proto2CS
    {
        public static void Export()
        {
            InnerProto2CS.Proto2CS();
        }
    }

    public static partial class InnerProto2CS
    {
        private static string protoDir => GetProtoDir();
        private static string clientServerMessagePath;
        private static readonly char[] splitChars = [' ', '\t'];
        private static readonly List<OpcodeInfo> msgOpcode = new();

        // --- 新增：全局已用集合，保证跨文件不重复 ---
        private static readonly HashSet<int> usedOpcodes = new();
        
        // --- 全局message类型集合，用于判断类型是否需要Dispose ---
        private static readonly HashSet<string> messageTypeNames = new();

        private static string GetProtoDir()
        {
            string[] tryPaths =
            {
                Path.Combine(AppContext.BaseDirectory, "Config/Proto/"),
                Path.Combine(AppContext.BaseDirectory, "../Config/Proto/"),
                Path.Combine(Directory.GetCurrentDirectory(), "Config/Proto/"),
                Path.Combine(Directory.GetCurrentDirectory(), "../Config/Proto/"),
            };

            foreach (var path in tryPaths)
            {
                if (Directory.Exists(path))
                    return path;
            }

            throw new DirectoryNotFoundException("找不到 Proto 目录！");
        }
        
        private static string GetSolutionRoot()
        {
            string current = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(current); i++)
            {
                string unityDir = Path.Combine(current, "Unity");
                string dotnetDir = Path.Combine(current, "DotNet");
                if (Directory.Exists(unityDir) && Directory.Exists(dotnetDir))
                {
                    return current;
                }

                DirectoryInfo parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }
                current = parent.FullName;
            }

            // 兜底：尝试从 AppContext.BaseDirectory 往上找
            current = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(current); i++)
            {
                string unityDir = Path.Combine(current, "Unity");
                string dotnetDir = Path.Combine(current, "DotNet");
                if (Directory.Exists(unityDir) && Directory.Exists(dotnetDir))
                {
                    return current;
                }

                DirectoryInfo parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }
                current = parent.FullName;
            }

            // 如果仍未找到，就返回当前工作目录
            return Directory.GetCurrentDirectory();
        }
        public static void Proto2CS()
        {
            usedOpcodes.Clear();
            msgOpcode.Clear();
            messageTypeNames.Clear();

            string solutionRoot = GetSolutionRoot();
            clientServerMessagePath = Path.Combine(
                solutionRoot,
                "Unity",
                "Assets",
                "Scripts",
                "Model",
                "Core",
                "Share",
                "Proto",
                "ClientServer");

            List<string> list = FileHelper.GetAllFiles(protoDir, "*proto");
            
            // 第一遍扫描：收集所有message类型名
            foreach (string s in list)
            {
                if (!s.EndsWith(".proto"))
                {
                    continue;
                }
                CollectMessageTypes(s);
            }
            
            // 第二遍扫描：生成代码
            foreach (string s in list)
            {
                if (!s.EndsWith(".proto"))
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(s);
                string[] ss2 = fileName.Split('_');
                string protoName = ss2[0];
                string cs = ss2[1];
                int startOpcode = int.Parse(ss2[2]);
                ProtoFile2CS(s, "", protoName, cs, startOpcode);
            }
        }
        
        /// <summary>
        /// 收集proto文件中的所有message类型名
        /// </summary>
        private static void CollectMessageTypes(string path)
        {
            string content = File.ReadAllText(path);
            foreach (string line in content.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("message"))
                {
                    string msgName = trimmed.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                    messageTypeNames.Add(msgName);
                }
            }
        }

        private static void ProtoFile2CS(string path, string module, string protoName, string cs, int startOpcode)
        {
            msgOpcode.Clear();

            string proto = path;
            string s = File.ReadAllText(proto);

            StringBuilder sb = new();
            sb.Append("using Nino.Core;\n");
            sb.Append("using System.Collections.Generic;\n\n");
            sb.Append($"namespace ET\n");
            sb.Append("{\n");

            bool isMsgStart = false;
            string msgName = "";
            string responseType = "";
            StringBuilder sbDispose = new();
            Regex responseTypeRegex = ResponseTypeRegex();
            foreach (string line in s.Split('\n'))
            {
                string newline = line.Trim();
                if (string.IsNullOrEmpty(newline))
                {
                    continue;
                }

                if (responseTypeRegex.IsMatch(newline))
                {
                    responseType = responseTypeRegex.Replace(newline, string.Empty);
                    responseType = responseType.Trim().Split(' ')[0].TrimEnd('\r', '\n');
                    continue;
                }

                if (!isMsgStart && newline.StartsWith("//"))
                {
                    if (newline.StartsWith("///"))
                    {
                        sb.Append("\t/// <summary>\n");
                        sb.Append($"\t/// {newline.TrimStart('/', ' ')}\n");
                        sb.Append("\t/// </summary>\n");
                    }
                    else
                    {
                        sb.Append($"\t// {newline.TrimStart('/', ' ')}\n");
                    }

                    continue;
                }

                if (newline.StartsWith("message"))
                {
                    isMsgStart = true;

                    string parentClass = "";
                    msgName = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                    string[] ss = newline.Split(["//"], StringSplitOptions.RemoveEmptyEntries);
                    if (ss.Length == 2)
                    {
                        parentClass = ss[1].Trim();
                    }

                    int assignedOpcode = ++startOpcode;

                    while (usedOpcodes.Contains(assignedOpcode))
                    {
                        assignedOpcode++;
                    }
                    
                    usedOpcodes.Add(assignedOpcode);
                    msgOpcode.Add(new OpcodeInfo() { Name = msgName, Opcode = assignedOpcode });

                    sb.Append($"\t[NinoType(false)]\n");
                    sb.Append($"\t[Message({protoName}.{msgName})]\n");
                    if (!string.IsNullOrEmpty(responseType))
                    {
                        sb.Append($"\t[ResponseType(nameof({responseType}))]\n");
                    }

                    sb.Append($"\tpublic partial class {msgName} : MessageObject");

                    if (parentClass is "IActorMessage" or "IActorRequest" or "IActorResponse")
                    {
                        sb.Append($", {parentClass}\n");
                    }
                    else if (parentClass != "")
                    {
                        sb.Append($", {parentClass}\n");
                    }
                    else
                    {
                        sb.Append('\n');
                    }

                    continue;
                }

                if (isMsgStart)
                {
                    if (newline.StartsWith('{'))
                    {
                        sbDispose.Clear();
                        sb.Append("\t{\n");
                        sb.Append($"\t\tpublic static {msgName} Create(bool isFromPool = false)\n\t\t{{\n\t\t\treturn ObjectPool.Fetch<{msgName}>(isFromPool);\n\t\t}}\n\n");
                        continue;
                    }

                    if (newline.StartsWith('}'))
                    {
                        isMsgStart = false;
                        responseType = "";

                        if (!newline.Contains("// no dispose"))
                        {
                            sb.Append($"\t\tpublic override void Dispose()\n\t\t{{\n\t\t\tif (!this.IsFromPool)\n\t\t\t{{\n\t\t\t\treturn;\n\t\t\t}}\n\n\t\t\t{sbDispose.ToString().TrimEnd('\t')}\n\t\t\tObjectPool.Recycle(this);\n\t\t}}\n");
                        }

                        sb.Append("\t}\n\n");
                        continue;
                    }

                    if (newline.StartsWith("//"))
                    {
                        sb.Append("\t\t/// <summary>\n");
                        sb.Append($"\t\t/// {newline.TrimStart('/', ' ')}\n");
                        sb.Append("\t\t/// </summary>\n");
                        continue;
                    }

                    string memberStr;
                    if (newline.Contains("//"))
                    {
                        string[] lineSplit = newline.Split("//");
                        memberStr = lineSplit[0].Trim();
                        sb.Append("\t\t/// <summary>\n");
                        sb.Append($"\t\t/// {lineSplit[1].Trim()}\n");
                        sb.Append("\t\t/// </summary>\n");
                    }
                    else
                    {
                        memberStr = newline;
                    }

                    if (memberStr.StartsWith("map<"))
                    {
                        Map(sb, memberStr, sbDispose);
                    }
                    else if (memberStr.StartsWith("repeated"))
                    {
                        Repeated(sb, memberStr, sbDispose);
                    }
                    else
                    {
                        Members(sb, memberStr, sbDispose);
                    }
                }
            }

            sb.Append("\tpublic static class " + protoName + "\n\t{\n");
            foreach (OpcodeInfo info in msgOpcode)
            {
                sb.Append($"\t\tpublic const ushort {info.Name} = {info.Opcode};\n");
            }

            sb.Append("\t}\n");

            sb.Append('}');

            sb.Replace("\t", "    ");
            string result = sb.ToString().ReplaceLineEndings("\r\n");
            GenerateCS(result, clientServerMessagePath, proto);
        }

        private static void GenerateCS(string result, string path, string proto)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string csPath = Path.Combine(path, Path.GetFileNameWithoutExtension(proto) + ".cs");
            using FileStream txt = new(csPath, FileMode.Create, FileAccess.ReadWrite);
            using StreamWriter sw = new(txt);
            sw.Write(result);
        }

        private static void Map(StringBuilder sb, string newline, StringBuilder sbDispose)
        {
            int start = newline.IndexOf('<') + 1;
            int end = newline.IndexOf('>');
            string types = newline.Substring(start, end - start);
            string[] ss = types.Split(',');
            string keyType = ConvertType(ss[0].Trim());
            string valueType = ConvertType(ss[1].Trim());
            string tail = newline[(end + 1)..];
            ss = tail.Trim().Replace(";", "").Split(' ');
            string v = ss[0];
            int n = int.Parse(ss[2]);

            sb.Append("\t\t#if DOTNET\n");
            sb.Append("\t\t[MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.ArrayOfArrays)]\n");
            sb.Append("\t\t#endif\n");
            sb.Append($"\t\t[NinoMember({n - 1})]\n");
            sb.Append($"\t\tpublic Dictionary<{keyType}, {valueType}> {v} {{ get; set; }} = new();\n");

            sbDispose.Append($"this.{v}.Clear();\n\t\t\t");
        }

        private static void Repeated(StringBuilder sb, string newline, StringBuilder sbDispose)
        {
            try
            {
                int index = newline.IndexOf(';');
                newline = newline.Remove(index);
                string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                string type = ss[1];
                type = ConvertType(type);
                string name = ss[2];
                int n = int.Parse(ss[4]);

                sb.Append($"\t\t[NinoMember({n - 1})]\n");
                sb.Append($"\t\tpublic List<{type}> {name} {{ get; set; }} = new();\n\n");

                sbDispose.Append($"this.{name}.Clear();\n\t\t\t");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{newline}\n {e}");
            }
        }

        private static string ConvertType(string type)
        {
            return type switch
            {
                "int16" => "short",
                "int32" => "int",
                "bytes" => "byte[]",
                "uint32" => "uint",
                "long" => "long",
                "int64" => "long",
                "uint64" => "ulong",
                "uint16" => "ushort",
                _ => type
            };
        }

        private static void Members(StringBuilder sb, string newline, StringBuilder sbDispose)
        {
            try
            {
                int index = newline.IndexOf(';');
                newline = newline.Remove(index);
                string[] ss = newline.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                string type = ss[0];
                string name = ss[1];
                int n = int.Parse(ss[3]);
                string typeCs = ConvertType(type);

                sb.Append($"\t\t[NinoMember({n - 1})]\n");
                sb.Append($"\t\tpublic {typeCs} {name} {{ get; set; }}\n\n");

                // 生成Dispose代码
                sbDispose.Append(GetDisposeCode(typeCs, name));
            }
            catch (Exception e)
            {
                Console.WriteLine($"{newline}\n {e}");
            }
        }

        /// <summary>
        /// 根据类型生成Dispose代码
        /// </summary>
        private static string GetDisposeCode(string typeCs, string fieldName)
        {
            // 优先判断message类型（避免在switch的when子句中执行Contains）
            if (messageTypeNames.Contains(typeCs))
            {
                // message类型（class，继承MessageObject）：先Dispose回收到对象池，再设置为null
                return $"this.{fieldName}?.Dispose();\n\t\t\tthis.{fieldName} = null;\n\t\t\t";
            }

            // 使用switch表达式处理其他类型
            return typeCs switch
            {
                // 基础值类型：设置为0（bool设置为false）
                "int" or "long" or "short" or "ushort" or "uint" or "ulong" or 
                "byte" or "sbyte" or "float" or "double" or "decimal" => $"this.{fieldName} = 0;\n\t\t\t",
                "bool" => $"this.{fieldName} = false;\n\t\t\t",
                
                // 引用类型：设置为null
                "string" or "byte[]" => $"this.{fieldName} = null;\n\t\t\t",
                
                // 其他类型（struct或class）：使用default
                // struct类型（如ActorId, Address）不能设置为null，使用default安全
                // class类型使用default也是null，同样安全
                _ => $"this.{fieldName} = default;\n\t\t\t"
            };
        }

        [GeneratedRegex(@"//\s*ResponseType")]
        private static partial Regex ResponseTypeRegex();
    }
}