using System.Text.Json;

namespace AgentChat.SourceGenerator.Tests;

/// <summary>
/// 
/// </summary>
public partial class FunctionExamples
{
    /// <summary>
    ///     Add function
    /// </summary>
    /// <param name="a">a</param>
    /// <param name="b">b</param>
    [FunctionAttribution]
    public int Add(int a, int b) => a + b;

    /// <summary>
    ///     DictionaryToString function
    /// </summary>
    /// <param name="xargs">an object of key-value pairs. key is string, value is string</param>
    [FunctionAttribution]
    public Task<string> DictionaryToStringAsync(Dictionary<string, string> xargs)
    {
        var res = JsonSerializer.Serialize(xargs, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return Task.FromResult(res);
    }

    /// <summary>
    ///     query function
    /// </summary>
    /// <param name="query">query, required</param>
    /// <param name="k">top k, optional, default value is 3</param>
    /// <param name="thresold">thresold, optional, default value is 0.5</param>
    [FunctionAttribution]
    public string[] Query(string query, int k = 3, float thresold = 0.5f) => Enumerable.Repeat(query, k).ToArray();

    /// <summary>
    ///     Sum function
    /// </summary>
    /// <param name="args">an array of double values</param>
    [FunctionAttribution]
    public double Sum(double[] args) => args.Sum();
}