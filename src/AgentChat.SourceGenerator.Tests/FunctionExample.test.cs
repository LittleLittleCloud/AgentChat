using System.Text.Json;
using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Azure.AI.OpenAI;
using FluentAssertions;
using Xunit;

namespace AgentChat.SourceGenerator.Tests;

public class FunctionExample
{
    private readonly FunctionExamples functionExamples = new();

    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public void Add_Test()
    {
        var args = new
        {
            a = 1,
            b = 2
        };

        VerifyFunction(functionExamples.AddWrapper, args, 3);
        VerifyFunctionDefinition(functionExamples.AddFunction);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task DictionaryToString_Test()
    {
        var args = new
        {
            xargs = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            }
        };

        await VerifyAsyncFunction(functionExamples.DictionaryToStringAsyncWrapper, args,
            JsonSerializer.Serialize(args.xargs, jsonSerializerOptions));
        VerifyFunctionDefinition(functionExamples.DictionaryToStringAsyncFunction);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public void Query_Test()
    {
        var args = new
        {
            query = "hello",
            k = 3
        };

        VerifyFunction(functionExamples.QueryWrapper, args, new[] { "hello", "hello", "hello" });
        VerifyFunctionDefinition(functionExamples.QueryFunction);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public void Sum_Test()
    {
        var args = new
        {
            args = new double[] { 1, 2, 3 }
        };

        VerifyFunction(functionExamples.SumWrapper, args, 6.0);
        VerifyFunctionDefinition(functionExamples.SumFunction);
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task TopLevelFunctionExampleAddTestAsync()
    {
        var example = new TopLevelStatementFunctionExample();

        var args = new
        {
            a = 1,
            b = 2
        };

        await VerifyAsyncFunction(example.AddWrapper, args, "3");
    }

    private async Task VerifyAsyncFunction<T, U>(Func<string, Task<T>> func, U args, T expected)
    {
        var str = JsonSerializer.Serialize(args, jsonSerializerOptions);
        var res = await func(str);
        res.Should().BeEquivalentTo(expected);
    }

    private void VerifyFunction<T, U>(Func<string, T> func, U args, T expected)
    {
        var str = JsonSerializer.Serialize(args, jsonSerializerOptions);
        var res = func(str);
        res.Should().BeEquivalentTo(expected);
    }

    [UseReporter(typeof(DiffReporter))]
    [UseApprovalSubdirectory("ApprovalTests")]
    private void VerifyFunctionDefinition(FunctionDefinition function)
    {
        var func = new
        {
            name = function.Name,
            description = function.Description.Replace(Environment.NewLine, ","),
            parameters = function.Parameters.ToObjectFromJson<object>(jsonSerializerOptions)
        };

        Approvals.Verify(JsonSerializer.Serialize(func, jsonSerializerOptions));
    }
}