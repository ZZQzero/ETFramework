using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ET
{
    [Generator(LanguageNames.CSharp)]
    public class ETMessageOpcodeGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor DuplicateOpcodeRule = new DiagnosticDescriptor(
            id: "ETOP001",
            title: "Duplicate opcode",
            messageFormat: "Duplicate opcode {0} used by types '{1}' and '{2}'",
            category: "ET.Generators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor MissingResponseTypeRule = new DiagnosticDescriptor(
            id: "ETOP002",
            title: "Missing ResponseType",
            messageFormat: "Request type '{0}' implements IRequest but does not have a ResponseTypeAttribute (and is not a ILocationMessage)",
            category: "ET.Generators",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(GeneratorInitializationContext context)
        {
            // 注册语法接收器：收集带 attribute 的类型申明
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver) return;
            
            var compilation = context.Compilation;
            var hasHint = compilation.SyntaxTrees
                .Select(st => st.GetRoot())
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax>()
                .SelectMany(root => root.Members)
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax>()
                .SelectMany(ns => ns.Members)
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                .Any(c => c.Identifier.Text == "OpcodeGeneratorHint");
            if (!hasHint)
            {
                // 如果没有找到 Hint 类，就直接返回，不生成
                return;
            }
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "GEN001", 
                    "Debug", 
                    $"OpcodeGenerator running in {compilation.AssemblyName}, Hint={hasHint}", 
                    "Generator", 
                    DiagnosticSeverity.Info, 
                    true),
                Location.None));
            var messages = new List<MessageInfo>();

            // 遍历候选类型
            foreach (var typeDecl in receiver.Candidates)
            {
                var model = compilation.GetSemanticModel(typeDecl.SyntaxTree);
                var typeSymbol = model.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
                if (typeSymbol == null) continue;

                // 读取所有属性数据
                var attrs = typeSymbol.GetAttributes();

                // 找到 MessageAttribute
                AttributeData messageAttr = attrs.FirstOrDefault(a => IsAttributeByName(a, "MessageAttribute"));
                if (messageAttr == null) continue;

                // 尝试读取 opcode（支持构造参数或命名参数 Opcode = ...）
                ushort opcode = 0;
                if (messageAttr.ConstructorArguments.Length > 0)
                {
                    var arg = messageAttr.ConstructorArguments[0];
                    opcode = ExtractUShortFromTypedConstant(arg);
                }

                if (messageAttr.NamedArguments.Length > 0)
                {
                    foreach (var kv in messageAttr.NamedArguments)
                    {
                        if (string.Equals(kv.Key, "Opcode", StringComparison.OrdinalIgnoreCase))
                        {
                            opcode = ExtractUShortFromTypedConstant(kv.Value);
                        }
                    }
                }

                // 如果 opcode == 0，跳过
                // 仍然收集，只是不在字典中生成 opcode==0 的项
                // 检查是否实现 IRequest
                bool isRequest = ImplementsInterfaceByName(typeSymbol, "IRequest");
                bool isLocationMessage = ImplementsInterfaceByName(typeSymbol, "ILocationMessage");

                // 读取 ResponseTypeAttribute（可能的写法： [ResponseType("G2C_Login")] 或 [ResponseType(typeof(G2C_Login))]）
                AttributeData respAttr = attrs.FirstOrDefault(a => IsAttributeByName(a, "ResponseTypeAttribute"));
                string? responseTypeNameFromAttr = null;
                INamedTypeSymbol? responseTypeSymbolFromAttr = null;

                if (respAttr != null)
                {
                    // constructor args
                    if (respAttr.ConstructorArguments.Length > 0)
                    {
                        var c0 = respAttr.ConstructorArguments[0];
                        if (c0.Kind == TypedConstantKind.Primitive && c0.Value is string s)
                        {
                            responseTypeNameFromAttr = s;
                        }
                        else if (c0.Kind == TypedConstantKind.Type && c0.Value is INamedTypeSymbol tSym)
                        {
                            responseTypeSymbolFromAttr = tSym;
                        }
                    }

                    // named args (in case author used named parameter)
                    foreach (var kv in respAttr.NamedArguments)
                    {
                        if (string.Equals(kv.Key, "Type", StringComparison.OrdinalIgnoreCase))
                        {
                            var v = kv.Value;
                            if (v.Kind == TypedConstantKind.Primitive && v.Value is string s)
                                responseTypeNameFromAttr = s;
                            else if (v.Kind == TypedConstantKind.Type && v.Value is INamedTypeSymbol tSym)
                                responseTypeSymbolFromAttr = tSym;
                        }
                    }
                }

                // 如果实现 IRequest 且非 ILocationMessage 且没有 response info -> 报错（编译时诊断）
                if (isRequest && !isLocationMessage && respAttr == null)
                {
                    var diag = Diagnostic.Create(MissingResponseTypeRule, typeSymbol.Locations.FirstOrDefault() ?? Location.None, typeSymbol.ToDisplayString());
                    context.ReportDiagnostic(diag);
                    // 仍继续收集，以便生成器尽量输出可用部分
                }

                // resolve response type full name if we have a name string
                string? responseTypeFullName = null;
                if (responseTypeSymbolFromAttr != null)
                {
                    responseTypeFullName = CleanFullName(responseTypeSymbolFromAttr.ToDisplayString());
                }
                else if (!string.IsNullOrEmpty(responseTypeNameFromAttr))
                {
                    // try to resolve via compilation.GetTypeByMetadataName
                    // allow both "ET.G2C_Login" or "G2C_Login" -> try ET.<name>
                    string candidate = responseTypeNameFromAttr;
                    INamedTypeSymbol? rt = compilation.GetTypeByMetadataName(candidate);
                    if (rt == null)
                    {
                        // try adding ET. prefix
                        rt = compilation.GetTypeByMetadataName("ET." + candidate);
                    }
                    if (rt != null)
                    {
                        responseTypeFullName = CleanFullName(rt.ToDisplayString());
                    }
                    else
                    {
                        // fallback: keep the name as ET.<name>
                        if (!candidate.Contains('.'))
                            responseTypeFullName = "ET." + candidate;
                        else
                            responseTypeFullName = candidate;
                    }
                }

                messages.Add(new MessageInfo
                {
                    TypeSymbol = typeSymbol,
                    TypeFullName = CleanFullName(typeSymbol.ToDisplayString()),
                    Opcode = opcode,
                    IsRequest = isRequest,
                    IsLocationMessage = isLocationMessage,
                    ResponseTypeFullName = responseTypeFullName
                });
            }

            // 检测重复 opcode（忽略 opcode == 0）
            var grouped = messages.Where(m => m.Opcode != 0).GroupBy(m => m.Opcode);
            foreach (var g in grouped)
            {
                var list = g.ToList();
                if (list.Count > 1)
                {
                    // 报告错误：第一次和第二次位置作为示例
                    var first = list[0];
                    var second = list[1];
                    var loc1 = first.TypeSymbol.Locations.FirstOrDefault() ?? Location.None;
                    var diag = Diagnostic.Create(DuplicateOpcodeRule, loc1, first.Opcode, first.TypeFullName, second.TypeFullName);
                    context.ReportDiagnostic(diag);
                }
            }

            // 生成代码
            string generated = GenerateCode(messages);
            context.AddSource("MessageOpcodeMap.g.cs", generated);
        }

        private static string CleanFullName(string name)
        {
            // remove "global::" if present
            return name.StartsWith("global::") ? name.Substring("global::".Length) : name;
        }

        private static bool IsAttributeByName(AttributeData a, string attributeShortName)
        {
            if (a.AttributeClass == null) return false;
            var n = a.AttributeClass.Name; // e.g. "MessageAttribute"
            if (string.Equals(n, attributeShortName, StringComparison.Ordinal))
                return true;
            return n.EndsWith(attributeShortName, StringComparison.Ordinal);
        }

        private static ushort ExtractUShortFromTypedConstant(TypedConstant tc)
        {
            try
            {
                if (tc.Kind == TypedConstantKind.Primitive && tc.Value != null)
                {
                    // usually int
                    if (tc.Value is int iv) return (ushort)iv;
                    if (tc.Value is short sv) return (ushort)sv;
                    if (tc.Value is long lv) return (ushort)lv;
                    if (tc.Value is ushort uv) return uv;
                }
            }
            catch
            {
                // ignore
            }
            return 0;
        }

        private static bool ImplementsInterfaceByName(INamedTypeSymbol typeSymbol, string interfaceName)
        {
            if (typeSymbol.AllInterfaces.Any(i => i.Name == interfaceName)) return true;
            // also check base types (redundant because AllInterfaces covers inherited)
            return false;
        }

        private static string GenerateCode(List<MessageInfo> messages)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Nino.Core;");
            sb.AppendLine();
            sb.AppendLine("namespace ET");
            sb.AppendLine("{");
            sb.AppendLine("    public static class MessageOpcodeTypeMap");
            sb.AppendLine("    {");

            // OpcodeToType (Dictionary<ushort, Type>)
            sb.AppendLine("        public static readonly Dictionary<ushort, Type> OpcodeToType = new(256)");
            sb.AppendLine("        {");

            foreach (var m in messages.Where(x => x.Opcode != 0).OrderBy(x => x.Opcode))
            {
                sb.AppendLine($"            {{ {m.Opcode}, typeof({m.TypeFullName}) }},");
            }

            sb.AppendLine("        };");
            sb.AppendLine();
            
            // OpcodeToMessage Dictionary<ushort, Func<MemoryBuffer, MessageObject>>
            sb.AppendLine("        public static readonly Dictionary<ushort, Func<MemoryBuffer, MessageObject>> OpcodeToMessage = new(256)");
            sb.AppendLine("        {");

            foreach (var m in messages.Where(x => x.Opcode != 0).OrderBy(x => x.Opcode))
            {
                sb.AppendLine($"            {{ {m.Opcode}, static (MemoryBuffer stream) => NinoDeserializer.Deserialize<{m.TypeFullName}>(stream.GetSpan()) }},");
            }

            sb.AppendLine("        };");
            sb.AppendLine();

            // TypeToOpcode (Dictionary<Type, ushort>)
            sb.AppendLine("        public static readonly Dictionary<Type, ushort> TypeToOpcode = new(256)");
            sb.AppendLine("        {");
            foreach (var m in messages.Where(x => x.Opcode != 0).OrderBy(x => x.TypeFullName))
            {
                sb.AppendLine($"            {{ typeof({m.TypeFullName}), {m.Opcode} }},");
            }
            sb.AppendLine("        };");
            sb.AppendLine();

            // RequestResponseType (Dictionary<Type, Type>)
            sb.AppendLine("        public static readonly Dictionary<Type, Type> RequestResponseType = new(256)");
            sb.AppendLine("        {");
            foreach (var m in messages)
            {
                if (m.IsRequest && !string.IsNullOrEmpty(m.ResponseTypeFullName))
                {
                    sb.AppendLine($"            {{ typeof({m.TypeFullName}), typeof({m.ResponseTypeFullName}) }},");
                }
                else if (m.IsRequest && string.IsNullOrEmpty(m.ResponseTypeFullName))
                {
                    sb.AppendLine($"            {{ typeof({m.TypeFullName}), typeof(ET.MessageResponse) }},");
                }
            }
            sb.AppendLine("        };");
            sb.AppendLine();
            
            // RequestResponse
            sb.AppendLine("        public static readonly Dictionary<Type, Func<bool,IResponse>> RequestResponse = new(256)");
            sb.AppendLine("        {");
            foreach (var m in messages)
            {
                if (m.IsRequest && !string.IsNullOrEmpty(m.ResponseTypeFullName))
                {
                    sb.AppendLine($"            {{ typeof({m.TypeFullName}), (isFromPool) => ObjectPool.Fetch<{m.ResponseTypeFullName}>(isFromPool) }},");
                }
                else if (m.IsRequest && string.IsNullOrEmpty(m.ResponseTypeFullName))
                {
                    sb.AppendLine($"            {{ typeof({m.TypeFullName}), (isFromPool) => ObjectPool.Fetch<ET.MessageResponse>(isFromPool) }},");
                }
            }
            sb.AppendLine("        };");

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<TypeDeclarationSyntax> Candidates { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // collect types that have attributes
                if (syntaxNode is TypeDeclarationSyntax tds && tds.AttributeLists.Count > 0)
                {
                    Candidates.Add(tds);
                }
            }
        }

        private class MessageInfo
        {
            public INamedTypeSymbol TypeSymbol = null!;
            public string TypeFullName = "";
            public ushort Opcode;
            public bool IsRequest;
            public bool IsLocationMessage;
            public string? ResponseTypeFullName;
        }
    }
}