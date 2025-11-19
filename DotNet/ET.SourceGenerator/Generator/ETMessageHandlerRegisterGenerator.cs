#define DOTNET

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ET;

[Generator(LanguageNames.CSharp)]
public class ETMessageHandlerRegisterGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => ETMessageHandlerRegisterSyntaxContextReceiver.Create());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ETMessageHandlerRegisterSyntaxContextReceiver receiver || receiver.handlers.Count == 0)
        {
            return;
        }

        // 获取当前程序集名称，用于生成独立的方法名
        string assemblyName = context.Compilation.AssemblyName ?? "Unknown";
        string assemblySuffix = GetAssemblySuffix(assemblyName);

        // 根据特性标签自动生成注册代码，按程序集分组
        // 生成RegisterMessage方法（Actor消息）
        string registerMessageCode = GenerateRegisterMessageCode(receiver.handlers, assemblySuffix);
        if (!string.IsNullOrEmpty(registerMessageCode))
        {
            context.AddSource("GameRegisterMessage.g.cs", registerMessageCode);
        }

        // 生成RegisterMessageSession方法（Session消息）
        string registerMessageSessionCode = GenerateRegisterMessageSessionCode(receiver.handlers, assemblySuffix);
        if (!string.IsNullOrEmpty(registerMessageSessionCode))
        {
            context.AddSource("GameRegisterMessageSession.g.cs", registerMessageSessionCode);
        }

        // 生成RegisterEvent方法
        string registerEventCode = GenerateRegisterEventCode(receiver.handlers, assemblySuffix);
        if (!string.IsNullOrEmpty(registerEventCode))
        {
            context.AddSource("GameRegisterEvent.g.cs", registerEventCode);
        }

        // 生成RegisterInvoke方法
        string registerInvokeCode = GenerateRegisterInvokeCode(receiver.handlers, assemblySuffix);
        if (!string.IsNullOrEmpty(registerInvokeCode))
        {
            context.AddSource("GameRegisterInvoke.g.cs", registerInvokeCode);
        }

        // 生成RegisterHttp方法
        string registerHttpCode = GenerateRegisterHttpCode(receiver.handlers, assemblySuffix);
        if (!string.IsNullOrEmpty(registerHttpCode))
        {
            context.AddSource("GameRegisterHttp.g.cs", registerHttpCode);
        }

        // 生成RegisterAI方法
        string registerAICode = GenerateRegisterAICode(receiver.handlers, assemblySuffix);
        if (!string.IsNullOrEmpty(registerAICode))
        {
            context.AddSource("GameRegisterAI.g.cs", registerAICode);
        }

        // 生成RegisterConsole方法
        string registerConsoleCode = GenerateRegisterConsoleCode(receiver.handlers, assemblySuffix);
        if (!string.IsNullOrEmpty(registerConsoleCode))
        {
            context.AddSource("GameRegisterConsole.g.cs", registerConsoleCode);
        }
    }

    private string GetAssemblySuffix(string assemblyName)
    {
        // 提取程序集名称的关键部分作为后缀
        // 例如：ET.Hotfix -> Hotfix, ET.Model -> Model
        if (assemblyName.Contains("."))
        {
            string[] parts = assemblyName.Split('.');
            return parts[parts.Length - 1];
        }
        return assemblyName;
    }

    private string GenerateRegisterMessageCode(List<HandlerInfo> handlers, string assemblySuffix)
    {
        var messageHandlers = handlers.Where(h => h.HandlerType == HandlerType.MessageHandler).ToList();

        if (messageHandlers.Count == 0)
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace ET");
        sb.AppendLine("{");
        sb.AppendLine($"    public static partial class GameRegister{assemblySuffix}");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void RegisterMessageAuto()");
        sb.AppendLine("        {");

        foreach (var handler in messageHandlers.OrderBy(h => h.SceneType).ThenBy(h => h.HandlerName))
        {
            sb.AppendLine($"            MessageDispatcher.Instance.RegisterMessage<{handler.HandlerName}>(SceneType.{handler.SceneTypeName});");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateRegisterMessageSessionCode(List<HandlerInfo> handlers, string assemblySuffix)
    {
        var sessionHandlers = handlers.Where(h => h.HandlerType == HandlerType.MessageSessionHandler).ToList();

        if (sessionHandlers.Count == 0)
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace ET");
        sb.AppendLine("{");
        sb.AppendLine($"    public static partial class GameRegister{assemblySuffix}");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void RegisterMessageSessionAuto()");
        sb.AppendLine("        {");

        foreach (var handler in sessionHandlers.OrderBy(h => h.SceneType).ThenBy(h => h.HandlerName))
        {
            sb.AppendLine($"            MessageSessionDispatcher.Instance.RegisterMessageSession<{handler.HandlerName}>(SceneType.{handler.SceneTypeName});");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateRegisterEventCode(List<HandlerInfo> handlers, string assemblySuffix)
    {
        var eventHandlers = handlers.Where(h => h.HandlerType == HandlerType.Event).ToList();

        if (eventHandlers.Count == 0)
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace ET");
        sb.AppendLine("{");
        sb.AppendLine($"    public static partial class GameRegister{assemblySuffix}");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void RegisterEventAuto()");
        sb.AppendLine("        {");

        foreach (var handler in eventHandlers.OrderBy(h => h.SceneType).ThenBy(h => h.HandlerName))
        {
            sb.AppendLine($"            EventSystem.Instance.RegisterEvent<{handler.HandlerName}>(SceneType.{handler.SceneTypeName});");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateRegisterInvokeCode(List<HandlerInfo> handlers, string assemblySuffix)
    {
        var invokeHandlers = handlers.Where(h => h.HandlerType == HandlerType.Invoke).ToList();

        if (invokeHandlers.Count == 0)
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace ET");
        sb.AppendLine("{");
        sb.AppendLine($"    public static partial class GameRegister{assemblySuffix}");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void RegisterInvokeAuto()");
        sb.AppendLine("        {");

        foreach (var handler in invokeHandlers.OrderBy(h => h.InvokeType).ThenBy(h => h.HandlerName))
        {
            if (handler.InvokeType > 0)
            {
                sb.AppendLine($"            EventSystem.Instance.RegisterInvoke<{handler.HandlerName}>({handler.InvokeTypeName});");
            }
            else
            {
                sb.AppendLine($"            EventSystem.Instance.RegisterInvoke<{handler.HandlerName}>();");
            }
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateRegisterHttpCode(List<HandlerInfo> handlers, string assemblySuffix)
    {
        var httpHandlers = handlers.Where(h => h.HandlerType == HandlerType.HttpHandler).ToList();

        if (httpHandlers.Count == 0)
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace ET");
        sb.AppendLine("{");
        sb.AppendLine($"    public static partial class GameRegister{assemblySuffix}");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void RegisterHttpAuto()");
        sb.AppendLine("        {");

        foreach (var handler in httpHandlers.OrderBy(h => h.SceneType).ThenBy(h => h.HandlerName))
        {
            sb.AppendLine($"            HttpDispatcher.Instance.HttpRegister<{handler.HandlerName}>(SceneType.{handler.SceneTypeName}, \"{handler.Path}\");");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateRegisterAICode(List<HandlerInfo> handlers, string assemblySuffix)
    {
        var aiHandlers = handlers.Where(h => h.HandlerType == HandlerType.AIHandler).ToList();

        if (aiHandlers.Count == 0)
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace ET");
        sb.AppendLine("{");
        sb.AppendLine($"    public static partial class GameRegister{assemblySuffix}");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void RegisterAIAuto()");
        sb.AppendLine("        {");

        foreach (var handler in aiHandlers.OrderBy(h => h.HandlerName))
        {
            sb.AppendLine($"            AIDispatcherSingle.Instance.RegisterAI<{handler.HandlerName}>();");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateRegisterConsoleCode(List<HandlerInfo> handlers, string assemblySuffix)
    {
        var consoleHandlers = handlers.Where(h => h.HandlerType == HandlerType.ConsoleHandler).ToList();

        if (consoleHandlers.Count == 0)
        {
            return "";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine("namespace ET");
        sb.AppendLine("{");
        sb.AppendLine($"    public static partial class GameRegister{assemblySuffix}");
        sb.AppendLine("    {");
        sb.AppendLine("        public static void RegisterConsoleAuto()");
        sb.AppendLine("        {");

        foreach (var handler in consoleHandlers.OrderBy(h => h.Mode).ThenBy(h => h.HandlerName))
        {
            sb.AppendLine($"            ConsoleDispatcher.Instance.RegisterConsole<{handler.HandlerName}>({handler.Mode});");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }


    class ETMessageHandlerRegisterSyntaxContextReceiver : ISyntaxContextReceiver
    {
        internal static ISyntaxContextReceiver Create()
        {
            return new ETMessageHandlerRegisterSyntaxContextReceiver();
        }

        public List<HandlerInfo> handlers = new List<HandlerInfo>();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                return;
            }

            var classTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
            if (classTypeSymbol == null)
            {
                return;
            }

            // 检查各种特性
            HandlerInfo? handlerInfo = null;

            // 检查各种特性
            foreach (var attr in classTypeSymbol.GetAttributes())
            {
                string attrName = attr.AttributeClass?.ToString() ?? "";
                
                // 检查MessageSessionHandler特性（优先检查，因为更明确）
                if (attrName == "ET.MessageSessionHandlerAttribute")
                {
                    var attrSyntax = classDeclarationSyntax.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => a.Name.ToString() == "MessageSessionHandler" || a.Name.ToString().EndsWith("MessageSessionHandler"));
                    
                    if (attrSyntax != null)
                    {
                        string sceneTypeName = ExtractSceneTypeName(attrSyntax, attr);
                        int sceneType = 0;
                        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int intValue)
                        {
                            sceneType = intValue;
                        }

                        handlerInfo = new HandlerInfo
                        {
                            HandlerName = classTypeSymbol.ToString(),
                            HandlerType = HandlerType.MessageSessionHandler,
                            SceneType = sceneType,
                            SceneTypeName = sceneTypeName,
                            FilePath = classDeclarationSyntax.SyntaxTree.FilePath,
                            AssemblyName = context.SemanticModel.Compilation.AssemblyName ?? ""
                        };
                        break;
                    }
                }
                // 检查MessageHandler特性
                else if (attrName == "ET.MessageHandlerAttribute")
                {
                    var attrSyntax = classDeclarationSyntax.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => a.Name.ToString() == "MessageHandler" || a.Name.ToString().EndsWith("MessageHandler"));
                    
                    if (attrSyntax != null)
                    {
                        string sceneTypeName = ExtractSceneTypeName(attrSyntax, attr);
                        int sceneType = 0;
                        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int intValue)
                        {
                            sceneType = intValue;
                        }

                        // 判断是MessageHandler还是MessageSessionHandler（通过基类判断）
                        HandlerType handlerType = DetermineHandlerType(classTypeSymbol);
                        
                        handlerInfo = new HandlerInfo
                        {
                            HandlerName = classTypeSymbol.ToString(),
                            HandlerType = handlerType,
                            SceneType = sceneType,
                            SceneTypeName = sceneTypeName,
                            FilePath = classDeclarationSyntax.SyntaxTree.FilePath,
                            AssemblyName = context.SemanticModel.Compilation.AssemblyName ?? ""
                        };
                        break;
                    }
                }
                else if (attrName == "ET.EventAttribute")
                {
                    var attrSyntax = classDeclarationSyntax.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => a.Name.ToString() == "Event" || a.Name.ToString().EndsWith("Event"));
                    
                    if (attrSyntax != null)
                    {
                        string sceneTypeName = ExtractSceneTypeName(attrSyntax, attr);
                        int sceneType = 0;
                        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int intValue)
                        {
                            sceneType = intValue;
                        }

                        handlerInfo = new HandlerInfo
                        {
                            HandlerName = classTypeSymbol.ToString(),
                            HandlerType = HandlerType.Event,
                            SceneType = sceneType,
                            SceneTypeName = sceneTypeName,
                            FilePath = classDeclarationSyntax.SyntaxTree.FilePath,
                            AssemblyName = context.SemanticModel.Compilation.AssemblyName ?? ""
                        };
                        break;
                    }
                }
                else if (attrName == "ET.InvokeAttribute")
                {
                    var attrSyntax = classDeclarationSyntax.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => a.Name.ToString() == "Invoke" || a.Name.ToString().EndsWith("Invoke"));
                    
                    if (attrSyntax != null)
                    {
                        string invokeTypeName = ExtractInvokeTypeName(attrSyntax, attr);
                        long invokeType = 0;
                        if (attr.ConstructorArguments.Length > 0)
                        {
                            var arg = attr.ConstructorArguments[0];
                            if (arg.Value is long longValue)
                            {
                                invokeType = longValue;
                            }
                            else if (arg.Value is int intValue)
                            {
                                invokeType = intValue;
                            }
                        }

                        handlerInfo = new HandlerInfo
                        {
                            HandlerName = classTypeSymbol.ToString(),
                            HandlerType = HandlerType.Invoke,
                            InvokeType = invokeType,
                            InvokeTypeName = invokeTypeName,
                            FilePath = classDeclarationSyntax.SyntaxTree.FilePath,
                            AssemblyName = context.SemanticModel.Compilation.AssemblyName ?? ""
                        };
                        break;
                    }
                }
                else if (attrName == "ET.HttpHandlerAttribute")
                {
                    var attrSyntax = classDeclarationSyntax.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => a.Name.ToString() == "HttpHandler" || a.Name.ToString().EndsWith("HttpHandler"));
                    
                    if (attrSyntax != null)
                    {
                        string sceneTypeName = ExtractSceneTypeName(attrSyntax, attr);
                        int sceneType = 0;
                        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int intValue)
                        {
                            sceneType = intValue;
                        }

                        // 提取 path 参数（第二个参数）
                        string path = ExtractHttpPath(attrSyntax, attr);

                        handlerInfo = new HandlerInfo
                        {
                            HandlerName = classTypeSymbol.ToString(),
                            HandlerType = HandlerType.HttpHandler,
                            SceneType = sceneType,
                            SceneTypeName = sceneTypeName,
                            Path = path,
                            FilePath = classDeclarationSyntax.SyntaxTree.FilePath,
                            AssemblyName = context.SemanticModel.Compilation.AssemblyName ?? ""
                        };
                        break;
                    }
                }
                else if (attrName == "ET.AIHandlerAttribute")
                {
                    var attrSyntax = classDeclarationSyntax.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => a.Name.ToString() == "AIHandler" || a.Name.ToString().EndsWith("AIHandler"));
                    
                    if (attrSyntax != null)
                    {
                        handlerInfo = new HandlerInfo
                        {
                            HandlerName = classTypeSymbol.ToString(),
                            HandlerType = HandlerType.AIHandler,
                            FilePath = classDeclarationSyntax.SyntaxTree.FilePath,
                            AssemblyName = context.SemanticModel.Compilation.AssemblyName ?? ""
                        };
                        break;
                    }
                }
                else if (attrName == "ET.ConsoleHandlerAttribute")
                {
                    var attrSyntax = classDeclarationSyntax.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => a.Name.ToString() == "ConsoleHandler" || a.Name.ToString().EndsWith("ConsoleHandler"));
                    
                    if (attrSyntax != null)
                    {
                        string mode = ExtractConsoleMode(attrSyntax, attr);

                        handlerInfo = new HandlerInfo
                        {
                            HandlerName = classTypeSymbol.ToString(),
                            HandlerType = HandlerType.ConsoleHandler,
                            Mode = mode,
                            FilePath = classDeclarationSyntax.SyntaxTree.FilePath,
                            AssemblyName = context.SemanticModel.Compilation.AssemblyName ?? ""
                        };
                        break;
                    }
                }
            }

            if (handlerInfo != null)
            {
                handlers.Add(handlerInfo);
            }
        }

        private HandlerType DetermineHandlerType(INamedTypeSymbol classTypeSymbol)
        {
            var baseType = classTypeSymbol.BaseType;
            while (baseType != null)
            {
                string baseTypeName = baseType.Name;
                string baseTypeFullName = baseType.ToString();

                // 检查是否是MessageSessionHandler基类
                if (baseTypeName == "MessageSessionHandler" || baseTypeFullName.StartsWith("ET.MessageSessionHandler"))
                {
                    return HandlerType.MessageSessionHandler;
                }

                // 检查是否是MessageHandler基类
                if (baseTypeName == "MessageHandler" || baseTypeFullName.StartsWith("ET.MessageHandler"))
                {
                    return HandlerType.MessageHandler;
                }

                baseType = baseType.BaseType;
            }

            return HandlerType.MessageHandler; // 默认
        }

        private string ExtractSceneTypeName(AttributeSyntax attr, AttributeData attrData)
        {
            // 首先尝试从语法节点中提取名称（例如 SceneType.Gate）
            if (attr.ArgumentList != null && attr.ArgumentList.Arguments.Count > 0)
            {
                var argExpr = attr.ArgumentList.Arguments[0].Expression;
                // 尝试提取 MemberAccessExpression (例如 SceneType.Gate)
                if (argExpr is MemberAccessExpressionSyntax memberAccess)
                {
                    return memberAccess.Name.Identifier.ValueText;
                }
                // 尝试提取 IdentifierName (例如 Gate，如果已经using了SceneType)
                else if (argExpr is IdentifierNameSyntax identifierName)
                {
                    return identifierName.Identifier.ValueText;
                }
            }

            // 如果从语法节点获取失败，尝试从语义信息获取
            if (attrData.ConstructorArguments.Length > 0)
            {
                var arg = attrData.ConstructorArguments[0];
                if (arg.Kind == TypedConstantKind.Primitive && arg.Value is int intValue)
                {
                    return $"Unknown_{intValue}";
                }
                else if (arg.Kind == TypedConstantKind.Enum)
                {
                    var enumType = arg.Type as INamedTypeSymbol;
                    if (enumType != null && enumType.TypeKind == TypeKind.Enum)
                    {
                        int sceneType = (int)arg.Value!;
                        var member = enumType.GetMembers().FirstOrDefault(m => 
                            m is IFieldSymbol field && 
                            field.IsStatic && 
                            field.ConstantValue != null && 
                            (int)field.ConstantValue == sceneType);
                        if (member != null)
                        {
                            return member.Name;
                        }
                    }
                }
            }

            return "Unknown";
        }

        private string ExtractInvokeTypeName(AttributeSyntax attr, AttributeData attrData)
        {
            // 首先尝试从语法节点中提取名称（例如 SceneType.Gate 或 MailBoxType.OrderedMessage）
            if (attr.ArgumentList != null && attr.ArgumentList.Arguments.Count > 0)
            {
                var argExpr = attr.ArgumentList.Arguments[0].Expression;
                // 尝试提取 MemberAccessExpression (例如 SceneType.Gate)
                if (argExpr is MemberAccessExpressionSyntax memberAccess)
                {
                    return $"{memberAccess.Expression.ToString()}.{memberAccess.Name.Identifier.ValueText}";
                }
                // 尝试提取 IdentifierName
                else if (argExpr is IdentifierNameSyntax identifierName)
                {
                    return identifierName.Identifier.ValueText;
                }
                // 尝试提取 LiteralExpression (数字)
                else if (argExpr is LiteralExpressionSyntax literal)
                {
                    return literal.Token.ValueText;
                }
            }

            // 如果从语法节点获取失败，尝试从语义信息获取
            if (attrData.ConstructorArguments.Length > 0)
            {
                var arg = attrData.ConstructorArguments[0];
                if (arg.Kind == TypedConstantKind.Primitive)
                {
                    return arg.Value?.ToString() ?? "0";
                }
                else if (arg.Kind == TypedConstantKind.Enum)
                {
                    var enumType = arg.Type as INamedTypeSymbol;
                    if (enumType != null && enumType.TypeKind == TypeKind.Enum)
                    {
                        long invokeType = (long)arg.Value!;
                        var member = enumType.GetMembers().FirstOrDefault(m => 
                            m is IFieldSymbol field && 
                            field.IsStatic && 
                            field.ConstantValue != null && 
                            (long)field.ConstantValue == invokeType);
                        if (member != null)
                        {
                            return $"{enumType.Name}.{member.Name}";
                        }
                    }
                }
            }

            return "0";
        }

        private string ExtractHttpPath(AttributeSyntax attr, AttributeData attrData)
        {
            // 首先尝试从语法节点中提取路径（第二个参数）
            if (attr.ArgumentList != null && attr.ArgumentList.Arguments.Count > 1)
            {
                var argExpr = attr.ArgumentList.Arguments[1].Expression;
                // 尝试提取字符串字面量
                if (argExpr is LiteralExpressionSyntax literal && literal.Token.Value is string strValue)
                {
                    return strValue;
                }
            }

            // 如果从语法节点获取失败，尝试从语义信息获取
            if (attrData.ConstructorArguments.Length > 1)
            {
                var arg = attrData.ConstructorArguments[1];
                if (arg.Kind == TypedConstantKind.Primitive && arg.Value is string str)
                {
                    return str;
                }
            }

            return "";
        }

        private string ExtractConsoleMode(AttributeSyntax attr, AttributeData attrData)
        {
            // 首先尝试从语法节点中提取 mode（第一个参数）
            if (attr.ArgumentList != null && attr.ArgumentList.Arguments.Count > 0)
            {
                var argExpr = attr.ArgumentList.Arguments[0].Expression;
                // 尝试提取 MemberAccessExpression (例如 ConsoleMode.ReloadConfig)
                if (argExpr is MemberAccessExpressionSyntax memberAccess)
                {
                    return $"{memberAccess.Expression.ToString()}.{memberAccess.Name.Identifier.ValueText}";
                }
                // 尝试提取 IdentifierName (例如 ReloadConfig，如果已经using了ConsoleMode)
                else if (argExpr is IdentifierNameSyntax identifierName)
                {
                    return identifierName.Identifier.ValueText;
                }
                // 尝试提取字符串字面量
                else if (argExpr is LiteralExpressionSyntax literal && literal.Token.Value is string strValue)
                {
                    return $"\"{strValue}\"";
                }
            }

            // 如果从语法节点获取失败，尝试从语义信息获取
            if (attrData.ConstructorArguments.Length > 0)
            {
                var arg = attrData.ConstructorArguments[0];
                if (arg.Kind == TypedConstantKind.Primitive && arg.Value is string str)
                {
                    return $"\"{str}\"";
                }
                else if (arg.Kind == TypedConstantKind.Enum)
                {
                    var enumType = arg.Type as INamedTypeSymbol;
                    if (enumType != null && enumType.TypeKind == TypeKind.Enum)
                    {
                        string enumValue = arg.Value?.ToString() ?? "0";
                        var member = enumType.GetMembers().FirstOrDefault(m => 
                            m is IFieldSymbol field && 
                            field.IsStatic && 
                            field.ConstantValue != null && 
                            field.ConstantValue.ToString() == enumValue);
                        if (member != null)
                        {
                            return $"{enumType.Name}.{member.Name}";
                        }
                        return $"{enumType.Name}.{enumValue}";
                    }
                }
            }

            return "\"\"";
        }
    }

    enum HandlerType
    {
        MessageHandler,
        MessageSessionHandler,
        Event,
        Invoke,
        HttpHandler,
        AIHandler,
        ConsoleHandler
    }

    class HandlerInfo
    {
        public string HandlerName { get; set; } = "";
        public HandlerType HandlerType { get; set; }
        public int SceneType { get; set; }
        public string SceneTypeName { get; set; } = "";
        public long InvokeType { get; set; }
        public string InvokeTypeName { get; set; } = "";
        public string Path { get; set; } = "";
        public string Mode { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string AssemblyName { get; set; } = "";
    }
}