using AgentChat.DotnetInteractiveService;
using AgentChat.Example.Share;
using AgentChat.OpenAI;
using Azure.AI.OpenAI;
using FluentAssertions;
using Xunit.Abstractions;

namespace AgentChat.Core.Tests
{
    public partial class TwoAgentTest
    {
        private readonly ITestOutputHelper _output;

        public TwoAgentTest(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// say name
        /// </summary>
        /// <param name="name">name.</param>
        [FunctionAttribution]
        public async Task<string> SayName(string name)
        {
            return $"SayName: {name}";
        }

        [FunctionAttribution]
        public async Task<string> Greeting(string msg)
        {
            return "No code available";
        }

        [FunctionAttribution]
        public async Task<string> TaskComplete(string msg)
        {
            return "[COMPLETE]";
        }

        [ApiKeyFact("AZURE_OPENAI_API_KEY")]
        public async Task TwoAgentChatTest()
        {
            var alice = Constant.GPT35.CreateAgent(
                name: "Alice",
                roleInformation: $@"You are a helpful AI assistant");

            var bob = Constant.GPT35.CreateAgent(
                name: "Bob",
                roleInformation: $@"You call SayName function",
                functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
                {
                    { this.SayNameFunction, this.SayNameWrapper },
                });

            var msgs = await alice.SendMessageToAgentAsync(bob, "hey what's your name", maxRound: 1);

            msgs.Should().HaveCount(2);
            msgs.First().Content.Should().Be("hey what's your name");
            msgs.First().From.Should().Be(alice.Name);
            msgs.Last().Content.Should().Be("SayName: Bob");
            msgs.Last().From.Should().Be(bob.Name);
        }

        [ApiKeyFact("AZURE_OPENAI_API_KEY_1")]
        public async Task TwoAgentCodingTest()
        {
            var coder = Constant.GPT35.CreateAgent(
                name: "Coder",
                roleInformation: @"You act as dotnet coder, you write dotnet script to resolve task.
-workflow-
write code

if code_has_error
    fix_code_error

if task_complete, call TaskComplete

-end-

Here're some rules to follow on write_code_to_resolve_current_step:
- put code between ```csharp and ```
- Use top-level statements, remove main function, just write code, like what python does.
- Remove all `using` statement. Runner can't handle it.
- Try to use `var` instead of explicit type.
- Try avoid using external library.
- Don't use external data source, like file, database, etc. Create a dummy dataset if you need.
- Always print out the result to console. Don't write code that doesn't print out anything.

Here are some examples for write code:
```nuget
xxx
```
```csharp
xxx
```

Here are some examples for fix_code_error:
The error is caused by xxx. Here's the fix code
```csharp
xxx
```",
                temperature: 0,
                functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
                {
                    { TaskCompleteFunction, TaskCompleteWrapper },
                });

            var workDir = Path.Combine(Path.GetTempPath(), "InteractiveService");
            if (!Directory.Exists(workDir))
                Directory.CreateDirectory(workDir);
            var service = new InteractiveService(workDir);
            using var dotnetInteractiveFunctions = new DotnetInteractiveFunction(service);

            // this function is used to fix invalid json returned by GPT-3
            var fixInvalidJsonFunction = new FixInvalidJsonFunctionWrapper(Constant.GPT35);

            var runner = Constant.GPT35.CreateAgent(
                name: "Runner",
                roleInformation: @"you act as dotnet runner, you run dotnet script and install nuget packages. Here's the workflow you follow:
-workflow-
if code_is_available
    call run_code

if nuget_packages_is_available
    call install_nuget_packages

for any other case
    call greeting
-end-",
                temperature: 0,
                functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>> {
                    { dotnetInteractiveFunctions.RunCodeFunction, fixInvalidJsonFunction.FixInvalidJsonWrapper(dotnetInteractiveFunctions.RunCodeWrapper) },
                    { dotnetInteractiveFunctions.InstallNugetPackagesFunction, dotnetInteractiveFunctions.InstallNugetPackagesWrapper },
                    { this.GreetingFunction, this.GreetingWrapper },
                });

            // start kenel
            await service.StartAsync(workDir, default);

            // test runner
            var msg = await runner.SendMessageAsync("hey");
            msg.Content.Should().Be("No code available");

            msg = await runner.SendMessageAsync("```csharp\nConsole.WriteLine(\"hello world\");\n```");
            msg.Content.Should().StartWith("hello world");

            msg = await runner.SendMessageAsync("```nuget\nMicrosoft.ML\n```");
            msg.Content.Should().StartWith("Installed nuget packages:");

            // use runner agent to auto-reply message from coder
            var userAgent = runner.CreateAutoReplyAgent("User", async (msgs, ct) =>
            {
                // if last message contains "COMPLETE", stop sending messages to runner agent and fall back to user agent
                if (msgs.Last().Content?.Contains("COMPLETE") is true)
                    return new Message(Role.User, IChatMessageExtension.TERMINATE, from: "User");

                // otherwise, send message to runner agent to either run code or install nuget packages and get the reply
                return await runner.SendMessageAsync(msgs.Last());
            });

            var chatHistory = await userAgent.SendMessageToAgentAsync(
                coder,
                "what's the 10th of fibonacci? Print the question and result in the end.",
                maxRound: 10);

            // print chat history
            foreach (var message in chatHistory)
            {
                _output.WriteLine(message.FormatMessage());
            }

            // last message should be terminate message
            chatHistory.Last().IsGroupChatTerminateMessage().Should().BeTrue();
        }
    }
}
