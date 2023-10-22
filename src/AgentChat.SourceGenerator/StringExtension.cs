using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace AgentChat.SourceGenerator
{
    internal static class StringExtension
    {
        public static string FormatCode(this string code)
        {
            var _workSpace = new AdhocWorkspace();
            _workSpace.AddSolution(
                      SolutionInfo.Create(SolutionId.CreateNewId("formatter"),
                      VersionStamp.Default)
            );

            var root = CSharpSyntaxTree.ParseText(code).GetRoot();
            var formatter = Formatter.Format(root, _workSpace);
            return formatter.ToFullString();
        }
    }
}
