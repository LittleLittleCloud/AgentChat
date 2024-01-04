using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using AgentChat.SourceGenerator.Template;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;

namespace AgentChat.SourceGenerator;

public static class GeneratorHelper
{
    public static string LogName(this SyntaxNode node) => node.ToString().Substring(0, Math.Min(50, node.ToString().Length));

    public static string? GetNamespace(this ClassDeclarationSyntax classDeclaration)
    {
        // Starting with the parent of the class declaration,
        // walk up the syntax tree until finding a namespace declaration or the root.
        var current = classDeclaration.Parent;
        while (current != null)
        {
            // Check if the current syntax node is a NamespaceDeclarationSyntax
            if (current is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                // Namespace found, return its fully qualified name
                return namespaceDeclaration.Name.ToString();
            }
            // Move to the next parent
            current = current.Parent;
        }
        // If no namespace declaration is found, the class is in the global namespace
        return null; // Or "global" or whatever represents the global namespace in your context
    }
}

[Generator]
public class FunctionCallGenerator : IIncrementalGenerator
{
    private const string FUNCTION_CALL_ATTRIBUTION = "AgentChat.FunctionAttribution";

    private const string FUNCTION_CALL_ATTRIBUTION_NAME = "FunctionAttribution";

    private static readonly DiagnosticDescriptor DiagnosticMessage = new(
        "AGCGEN001",
        "Output",
        "{0}",
        "AgentChatSourceGenerator",
        DiagnosticSeverity.Info,
        true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if LAUNCH_DEBUGGER
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
#endif
        var optionProvider = context.AnalyzerConfigOptionsProvider.Select((provider, ct) =>
        {
            var generateFunctionDefinitionContract =
                provider.GlobalOptions.TryGetValue("build_property.EnableContract", out var value) &&
                value?.ToLowerInvariant() == "true";

            return generateFunctionDefinitionContract;
        });

        

        bool IsNodeGood(SyntaxNode node, CancellationToken ct)
        {
            if (node is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                //Trace.WriteLine($"Node {node.LogName()} is not a ClassDeclarationSyntax");
                return false;
            }

            if (!classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                //Trace.WriteLine($"Node {node.LogName()} doesn't is not a 'partial' class");
                return false;
            }

            if (!classDeclarationSyntax.Members.Any(HasFunctionAttributionAttribute))
            {
                //Trace.WriteLine($"Node {node.LogName()} doesn't have any members with a FunctionAttribution attribute");
                return false;
            }

            return true;
        }

        // step 1
        // filter syntax tree and search syntax node that satisfied the following conditions
        // - is partial class
        var partialClassSyntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(IsNodeGood,
                (ctx, ct) =>
                {
                    // first check if any method of the class has FunctionAttribution attribute
                    // if not, then return null
                    //var filePath = ctx.Node.SyntaxTree.FilePath;
                    //var fileName = Path.GetFileNameWithoutExtension(filePath);

                    if (ctx.Node is not ClassDeclarationSyntax classDeclarationSyntax)
                    {
                        throw new Exception("Node is not ClassDeclarationSyntax");
                    }


                    var classNamespace = classDeclarationSyntax.GetNamespace();
                    var fullClassName = string.IsNullOrEmpty(classNamespace) 
                        ? $"{classDeclarationSyntax.Identifier}"
                        : $"{classNamespace}.{classDeclarationSyntax.Identifier}";

                    Trace.WriteLine($"🤖 Namespace:{classNamespace} Class:{fullClassName}");


                    // collect methods that has FunctionAttribution attribute
                    var methodDeclarationSyntaxes = classDeclarationSyntax.Members.Where(HasFunctionAttributionAttribute)
                        .Select(member => member as MethodDeclarationSyntax)
                        .Where(method => method != null);

                    var functionContracts = methodDeclarationSyntaxes.Select(method => CreateFunctionContract(method!)).ToArray();

                    /*Trace.WriteLine($"Class: {fullClassName}");

                    foreach (var funcc in functionContracts)
                    {
                        Trace.WriteLine($"\tFunction: {funcc.Name}");
                    }*/

                    return (fullClassName, classDeclarationSyntax, functionContracts);
                })

            //.Where(node => node.functionContracts != null)
            .Collect();

        var aggregateProvider = optionProvider.Combine(partialClassSyntaxProvider);

        // step 2
        context.RegisterSourceOutput(aggregateProvider,
            (ctx, source) =>
            {
                var groups = source.Right.GroupBy(item => item.fullClassName);

                foreach (var group in groups)
                {
                    var functionContracts = group.SelectMany(item => item.functionContracts!).ToArray();
                    var classDeclarationSyntax = group.First().classDeclarationSyntax;
                    var className = classDeclarationSyntax!.Identifier.ToString();
                    var classNamespace = classDeclarationSyntax.GetNamespace();
                 

                    foreach (var funcc in functionContracts)
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticMessage, Location.None,
                            $"Generating Function: {funcc.Name}"));
                    }

                    var functionTT = new FunctionCallTemplate
                    {
                        NameSpace = classNamespace ?? string.Empty,
                        ClassName = className,
                        FunctionContracts = functionContracts.ToArray()
                    };

                    var functionSource = functionTT.TransformText();
                    var fileName = $"{className}.generated.cs";

                    ctx.AddSource(fileName, SourceText.From(functionSource, Encoding.UTF8));
                    File.WriteAllText(Path.Combine(Path.GetTempPath(), fileName), functionSource);
                }

                if (source.Left)
                {
                    var overallFunctionDefinition =
                        source.Right.SelectMany(x => x.functionContracts.Select(y => new { x.fullClassName, y }));

                    var overallFunctionDefinitionObject = overallFunctionDefinition
                        .Where(x => x.y.Parameters != null)
                        .Select(
                            x => new
                            {
                                x.fullClassName,
                                functionDefinition = new
                                {
                                    x.y.Name,
                                    x.y.Description,
                                    x.y.ReturnType,
                                    Parameters = x.y.Parameters!.Select(y => new
                                    {
                                        y.Name,
                                        y.Description,
                                        y.JsonType,
                                        y.JsonItemType,
                                        y.Type,
                                        y.IsOptional,
                                        y.DefaultValue
                                    })
                                }
                            });

                    var json = JsonConvert.SerializeObject(overallFunctionDefinitionObject, Formatting.Indented);

                    // wrap json inside csharp block, as SG doesn't support generating non-source file
                    json = $@"/* <auto-generated> wrap json inside csharp block, as SG doesn't support generating non-source file
{json}
</auto-generated>*/";
                    ctx.AddSource("FunctionDefinition.json", SourceText.From(json, Encoding.UTF8));
                }
            });
    }

    private FunctionContract CreateFunctionContract(MethodDeclarationSyntax method)
    {
        // get function_call attribute
        var functionCallAttribute = method.AttributeLists.SelectMany(attributeList => attributeList.Attributes)
            .FirstOrDefault(attribute => attribute.Name.ToString() == FUNCTION_CALL_ATTRIBUTION);

        // get document string if exist
        var documentationCommentTrivia = method.GetDocumentationCommentTriviaSyntax();

        var functionName = method.Identifier.ToString();

        var functionDescription =
            functionCallAttribute?.ArgumentList?.Arguments
                .FirstOrDefault(argument => argument.NameEquals?.Name.ToString() == "Description")?.Expression.ToString() ??
            string.Empty;

        if (string.IsNullOrEmpty(functionDescription))
        {
            // if functionDescription is empty, then try to get it from documentationCommentTrivia
            // firstly, try getting from <summary> tag
            var summary = documentationCommentTrivia?.Content.GetFirstXmlElement("summary");

            if (summary is not null && XElement.Parse(summary.ToString()) is XElement element)
            {
                functionDescription = element.Nodes().OfType<XText>().FirstOrDefault()?.Value;

                if (functionDescription is null)
                {
                    throw new Exception("functionDescription is null");
                }

                // remove [space...][//|///][space...] from functionDescription
                // replace [^\S\r\n]+[\/]+\s* with empty string
                functionDescription = Regex.Replace(functionDescription, @"[^\S\r\n]+\/[\/]+\s*", string.Empty);
            }
            else
            {
                // if <summary> tag is not exist, then simply use the entire leading trivia as functionDescription
                functionDescription = method.GetLeadingTrivia().ToString();

                // remove [space...][//|///][space...] from functionDescription
                // replace [^\S\r\n]+[\/]+\s* with empty string
                functionDescription = Regex.Replace(functionDescription, @"[^\S\r\n]+\/[\/]+\s*", string.Empty);
            }
        }

        // get parameters
        var parameters = method.ParameterList.Parameters.Select(parameter =>
        {
            var description = $"{parameter.Identifier}. type is {parameter.Type}";

            // try to get parameter description from documentationCommentTrivia
            var parameterDocumentationComment =
                documentationCommentTrivia?.GetParameterDescriptionFromDocumentationCommentTriviaSyntax(parameter.Identifier
                    .ToString());

            if (parameterDocumentationComment is not null)
            {
                description = parameterDocumentationComment;

                // remove [space...][//|///][space...] from functionDescription
                // replace [^\S\r\n]+[\/]+\s* with empty string
                description = Regex.Replace(description, @"[^\S\r\n]+\/[\/]+\s*", string.Empty);
            }

            var jsonItemType = parameter.Type!.ToString().EndsWith("[]") ?
                parameter.Type!.ToString().Substring(0, parameter.Type!.ToString().Length - 2) : null;

            return new ParameterContract
            {
                Name = parameter.Identifier.ToString(),
                JsonType = parameter.Type!.ToString() switch
                {
                    "string" => "string",
                    "string[]" => "array",
                    "System.Int32" or "int" => "number",
                    "System.Int64" or "long" => "number",
                    "System.Single" or "float" => "number",
                    "System.Double" or "double" => "number",
                    "System.Boolean" or "bool" => "boolean",
                    "System.DateTime" => "string",
                    "System.Guid" => "string",
                    "System.Object" => "object",
                    _ => "object"
                },
                JsonItemType = jsonItemType,
                Type = parameter.Type!.ToString(),
                Description = description,
                IsOptional = parameter.Default != null,

                // if Default is null or "null", then DefaultValue is null
                DefaultValue = parameter.Default?.ToString() == "null" ? null : parameter.Default?.Value.ToString()
            };
        });

        return new FunctionContract
        {
            Name = functionName,
            Description = functionDescription?.Trim() ?? functionName,
            Parameters = parameters.ToArray(),
            ReturnType = method.ReturnType.ToString()
        };
    }

    /// <summary>
    /// Returns true if the member has a FunctionAttribution attribute
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    protected bool HasFunctionAttributionAttribute(MemberDeclarationSyntax member)
    {
        //Trace.WriteLine($"Member: {member.ToString()}");

        foreach (var attribute in member.AttributeLists.SelectMany(list => list.Attributes))
        {
            if (attribute.Name is IdentifierNameSyntax { Identifier.Text: FUNCTION_CALL_ATTRIBUTION_NAME } nameSyntax)
            {
                //Trace.WriteLine($"FUNCTION: {member.ToString()} {nameSyntax}");
                return true;
            }
        }

        return false;
    }
}