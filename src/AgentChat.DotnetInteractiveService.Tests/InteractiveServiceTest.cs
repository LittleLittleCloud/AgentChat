using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AgentChat.DotnetInteractiveService.Tests
{
    public class InteractiveServiceTest : TestOutputHelper
    {
        private readonly ITestOutputHelper _output;

        public InteractiveServiceTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task InteractiveService_InitializeTestAsync()
        {
            var tmp = Path.GetTempPath();
            var workDir = Path.Combine(tmp, "InteractiveService");
            if (Directory.Exists(workDir))
            {
                Directory.Delete(workDir, true);
            }

            Directory.CreateDirectory(workDir);

            using var service = new InteractiveService(workDir);
            service.Output += Service_Output;
            var cts = new CancellationTokenSource();
            var isRunning = await service.StartAsync(workDir, cts.Token);

            isRunning.Should().BeTrue();

            (await service.IsRunningAsync(cts.Token)).Should().BeTrue();

            var versionFormatString = string.Empty;

            // test code snippet
            var hello_world = @"
Console.WriteLine(""hello world"");
";

            await this.TestCodeSnippet(service, hello_world, "hello world");
            await this.TestCodeSnippet(
                service, 
                code:@"
Console.WriteLine(""hello world""
",
                expectedOutput: "Error: (2,32): error CS1026: ) expected");

            await this.TestCodeSnippet(
                service: service,
                code: "throw new Exception();",
                expectedOutput: "Error: System.Exception: Exception of type 'System.Exception' was thrown");

            // run the following test only on windows
            // test power shell
            // echo hello world
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var ps = @"echo ""hello world""";
                await this.TestPowershellCodeSnippet(service, ps, "hello world");
            }
            
        }

        private async Task TestPowershellCodeSnippet(InteractiveService service, string code, string expectedOutput)
        {
            var result = await service.SubmitPowershellCodeAsync(code, CancellationToken.None);
            result.Should().StartWith(expectedOutput);
        }

        private async Task TestCodeSnippet(InteractiveService service, string code, string expectedOutput)
        {
            var result = await service.SubmitCSharpCodeAsync(code, CancellationToken.None);
            result.Should().StartWith(expectedOutput);
        }

        private void Service_Output(object? sender, string e)
        {
            this._output.WriteLine(e);
        }
    }
}
