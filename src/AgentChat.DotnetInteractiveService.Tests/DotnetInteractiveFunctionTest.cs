using AgentChat.Core.Tests;
using AgentChat.Example.Share;
using FluentAssertions;
using AgentChat.OpenAI;
using Xunit.Abstractions;

namespace AgentChat.DotnetInteractiveService.Tests
{
    public class DotnetInteractiveFunctionTest : IDisposable
    {
        private ITestOutputHelper _output;
        private InteractiveService _interactiveService;
        private string _workingDir;
        private DotnetInteractiveFunction _function;

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

        public void Dispose()
        {
            _interactiveService.Dispose();
            _function.Dispose();
        }

        [ApiKeyFact("AZURE_OPENAI_API_KEY")]
        public async Task DotnetInteractiveFunction_RunCSharpCode_TestAsync()
        {
            var agent = Constant.GPT35.CreateAgent(
                name: "tester",
                roleInformation: "tester",
                functionMap: new Dictionary<Azure.AI.OpenAI.FunctionDefinition, Func<string, Task<string>>>
                {
                    { _function.RunCodeFunction, _function.RunCodeWrapper },
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

        [ApiKeyFact("AZURE_OPENAI_API_KEY")]
        public async Task DotnetInteractiveFunction_InstallNugetPackage_TestAsync()
        {
            var agent = new GPTAgent(
                Constant.GPT35,
                "tester",
                "tester",
                new Dictionary<Azure.AI.OpenAI.FunctionDefinition, Func<string, Task<string>>>
                {
                    { _function.InstallNugetPackagesFunction, _function.InstallNugetPackagesWrapper },
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
    }
}
