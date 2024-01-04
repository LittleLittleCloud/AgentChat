using AgentChat;

/// <summary>
/// 
/// </summary>
public partial class TopLevelStatementFunctionExample
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [FunctionAttribution]
    public Task<string> Add(int a, int b) => Task.FromResult($"{a + b}");
}