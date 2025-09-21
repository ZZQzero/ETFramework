using System.Text;

namespace ET;

public class GenerateEntitySystem
{
    private static string path = "DotNet/ET.App/GameRegister/GameRegisterEventSystem.cs";

    private static void Init()
    {
        NoCut.Run();
        ModelNoCut.Run();
        CoreNoCut.Run();
        HotfixInit.Run();
    }
    public static void Generate()
    {
        Init();
        var sb = new StringBuilder();
        sb.AppendLine("//-----------自动生成------------");
        sb.AppendLine("namespace ET");
        sb.AppendLine("{");
        sb.AppendLine("    public static partial class GameRegister");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void RegisterEntitySystem()");
        sb.AppendLine("        {");

        // 遍历 DLL
        var assemblis = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblis)
        {
            if (!assembly.FullName.Contains("ET"))
            {
                continue;
            }
                
            foreach (var type in assembly.GetTypes())
            {
                // 排除 abstract 类、接口
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                // 找到标记 [EntitySystem] 的类
                var hasAttr = type.GetCustomAttributes(false).Any(a => a.GetType().Name == "EntitySystemAttribute");
                if (!hasAttr)
                {
                    continue;
                }

                // 修正内部类名称，用 . 替代 +
                var typeName = type.FullName.Replace('+', '.');
                sb.AppendLine($"            EntitySystemSingleton.RegisterEntitySystem<{typeName}>();");
            }
        }
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        // 保存到文件
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

    }
}
