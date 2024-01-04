using System.Runtime.InteropServices;
using AgentChat.Core.Tests;
using AgentChat.Example.Share;
using AgentChat.OpenAI;
using Azure.AI.OpenAI;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AgentChat.DotnetInteractiveService.Tests;

/// <summary>
/// 
/// </summary>
public class DotnetInteractiveFunctionTest : IDisposable
{
    private readonly DotnetInteractiveFunction _function;

    private readonly InteractiveService _interactiveService;

    private readonly ITestOutputHelper _output;

    private readonly string _workingDir;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="output"></param>
    public DotnetInteractiveFunctionTest(ITestOutputHelper output)
    {
        _output = output;
        _workingDir = Path.Combine(Path.GetTempPath(), "test");

        if (!Directory.Exists(_workingDir))
        {
            Directory.CreateDirectory(_workingDir);
        }

        _interactiveService = new InteractiveService(_workingDir);
        _interactiveService.StartAsync(_workingDir, default).Wait();
        _function = new DotnetInteractiveFunction(_interactiveService);
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        _interactiveService.Dispose();
        _function.Dispose();
    }

    /// <summary>
    /// 
    /// </summary>
    [ApiKeyFact("AZURE_OPENAI_API_KEY")]
    public async Task DotnetInteractiveFunction_InstallNugetPackage_TestAsync()
    {
        var agent = new GPTAgent(
            Constant.GPT35,
            "tester",
            "tester",
            new Dictionary<FunctionDefinition, Func<string, Task<string>>>
            {
                { _function.InstallNugetPackagesFunction, _function.InstallNugetPackagesWrapper }
            });

        var msg = @"Install the following nugets:
- Microsoft.ML
- Microsoft.ML.AutoML
";
        var result = await agent.SendMessageAsync(msg);
        result.Should().BeOfType<GPTChatMessage>();
        (result as GPTChatMessage)?.FunctionCall?.Name.Should().Be(_function.InstallNugetPackagesFunction.Name);
        result?.Content.Should().Contain("Microsoft.ML");
        result?.Content.Should().Contain("Microsoft.ML.AutoML");
    }

    /// <summary>
    /// 
    /// </summary>
    [ApiKeyFact("AZURE_OPENAI_API_KEY")]
    public async Task DotnetInteractiveFunction_RunCSharpCode_TestAsync()
    {
        var agent = Constant.GPT35.CreateAgent(
            "tester",
            "tester",
            functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
            {
                { _function.RunCodeFunction, _function.RunCodeWrapper }
            });

        var msg = @"Run the following C# code:
```csharp
Console.WriteLine(""Hello World"");
```
";
        var result = await agent.SendMessageAsync(msg);
        result.Should().BeOfType<GPTChatMessage>();
        (result as GPTChatMessage)?.FunctionCall?.Name.Should().Be(_function.RunCodeFunction.Name);
        result?.Content.Should().StartWith("Hello World");
    }

    /// <summary>
    /// 
    /// </summary>
    [Fact]
    public async Task InteractiveService_InitializeTestAsync()
    {
        var cts = new CancellationTokenSource();
        var isRunning = await _interactiveService.StartAsync(_workingDir, cts.Token);

        isRunning.Should().BeTrue();

        _interactiveService.IsRunning().Should().BeTrue();

        var versionFormatString = string.Empty;

        // test code snippet
        var hello_world = @"
Console.WriteLine(""hello world"");
";

        await TestCodeSnippet(_interactiveService, hello_world, "hello world");

        await TestCodeSnippet(
            _interactiveService,
            @"
Console.WriteLine(""hello world""
",
            "Error: (2,32): error CS1026: ) expected");

        await TestCodeSnippet(
            _interactiveService,
            "throw new Exception();",
            "Error: System.Exception: Exception of type 'System.Exception' was thrown");

        // run the following test only on windows
        // test power shell
        // echo hello world
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var ps = @"echo ""hello world""";
            await TestPowershellCodeSnippet(_interactiveService, ps, "hello world");
        }
    }

    private void Service_Output(object? sender, string e)
    {
        _output.WriteLine(e);
    }

    private async Task TestCodeSnippet(InteractiveService service, string code, string expectedOutput)
    {
        var result = await service.SubmitCSharpCodeAsync(code, CancellationToken.None);
        result.Should().StartWith(expectedOutput);
    }

    private async Task TestPowershellCodeSnippet(InteractiveService service, string code, string expectedOutput)
    {
        var result = await service.SubmitPowershellCodeAsync(code, CancellationToken.None);
        result.Should().StartWith(expectedOutput);
    }
}