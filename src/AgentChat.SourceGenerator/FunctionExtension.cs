using AgentChat.SourceGenerator;

internal static class FunctionExtension
{
    public static string GetFunctionDefinitionName(this FunctionContract function) => $"{function.GetFunctionName()}Function";

    public static string GetFunctionName(this FunctionContract function) => function.Name ?? string.Empty;

    public static string GetFunctionSchemaClassName(this FunctionContract function) => $"{function.GetFunctionName()}Schema";

    public static string GetFunctionWrapperName(this FunctionContract function) => $"{function.GetFunctionName()}Wrapper";
}