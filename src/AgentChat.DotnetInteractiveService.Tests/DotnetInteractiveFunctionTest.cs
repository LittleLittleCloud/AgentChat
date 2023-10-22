using FluentAssertions;
using AgentChat.DotnetInteractiveService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using AgentChat.Core;
using AgentChat.Core.Tests;

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
            _workingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_workingDir);
            _interactiveService = new InteractiveService(_workingDir);
            _interactiveService.StartAsync(_workingDir, default).Wait();
            _function = new DotnetInteractiveFunction(_interactiveService);
        }

        public void Dispose()
        {
            _interactiveService.Dispose();
            _function.Dispose();
        }

        [ApiKeyFact]
        public async Task DotnetInteractiveFunction_RunCSharpCode_TestAsync()
        {
            var agent = new GPTAgent(
                Constant.GPT,
                Constant.GPT_35_MODEL_ID,
                "tester",
                "tester",
                new Dictionary<Azure.AI.OpenAI.FunctionDefinition, Func<string, Task<string>>>
                {
                    { _function.RunCodeFunction, _function.RunCodeWrapper },
                });

            var msg = @"Run the following C# code:
```csharp
Console.WriteLine(""Hello World"");
```
";
            var result = await agent.SendMessageAsync(msg);
            result?.FunctionCall?.Name.Should().Be(_function.RunCodeFunction.Name);
            result?.Content.Should().StartWith("Hello World");
        }

        [ApiKeyFact]
        public async Task DotnetInteractiveFunction_InstallNugetPackage_TestAsync()
        {
            var agent = new GPTAgent(
                Constant.GPT,
                Constant.GPT_35_MODEL_ID,
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
            result?.FunctionCall?.Name.Should().Be(_function.InstallNugetPackagesFunction.Name);
            result?.Content.Should().Contain("Microsoft.ML");
            result?.Content.Should().Contain("Microsoft.ML.AutoML");
        }
    }
}
