// See https://aka.ms/new-console-template for more information
using Azure.AI.OpenAI;
using GroupChatExample.DotnetInteractiveService;
using GroupChatExample.Helper;
using System.Reflection;
using System.Text;

var workDir = Path.Combine(Path.GetTempPath(), "InteractiveService");

// remove workDir if exists
if (Directory.Exists(workDir))
{
    Directory.Delete(workDir, recursive: true);
}

// create workDir
Directory.CreateDirectory(workDir);

using var service = new InteractiveService(workDir);
await service.StartAsync(workDir, default);
var logger = new Logger(workDir);
using var dotnetInteractiveFunction = new DotnetInteractiveFunction(service, logger: logger);
var OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set");
var model = "gpt-3.5-turbo-0613";
var openAIClient = new OpenAIClient(OPENAI_API_KEY);
var fixInvalidJsonFunction = new FixInvalidJsonFunctionWrapper(openAIClient, model);

var coder = new ChatAgent(
        openAIClient,
        model,
        "Coder",
        @"You are dotnet coder, you write dotnet script to resolve tasks.
You implement given step based on previous context. You don't need to provide complete code, just provide the code to implement the step.

e.g.
```nuget
// install xx packages
```

```csharp
var a = 1;
var b = 2;
...
```
end

Here're some rules to follow when you write dotnet code:
- Use top-level statements, remove main function, just write code, like what python does.
- Remove all `using` statement. Runner can't handle it.
- Try to use `var` instead of explicit type.
- Try avoid using external library.
- Don't use external data source, like file, database, etc. Create a dummy dataset if you need.
- Always print out the result to console. Don't write code that doesn't print out anything.
");

var runner = new ChatAgent(
        openAIClient,
        model,
        "Runner",
        @"You use dotnet interactive to run existing csharp code from the most recent message. You have access to file system and network.
You can only reply with RunCodeFunction or InstallNugetPackagesFunction or 'No code to run'. All other responses is invalid.
",
        new Dictionary<FunctionDefinition, Func<string, Task<string>>>
        {
            { dotnetInteractiveFunction.RunCodeFunction, fixInvalidJsonFunction.FixInvalidJsonWrapper(dotnetInteractiveFunction.RunCodeWrapper) },
            { dotnetInteractiveFunction.InstallNugetPackagesFunction, dotnetInteractiveFunction.InstallNugetPackagesWrapper },
        });

var admin = new ChatAgent(
    Constant.AzureGPT4,
    Constant.AZURE_GPT_4_MODEL_ID,
    "Admin",
    @"You are admin, you provide task to coder and runner.
For each step, you ask Coder to implement the step, then ask Runner to run the code. If there's error, you ask Coder to fix the error.
If all steps resolved, you terminate group chat. Avoid free chatting.");

var groupChat = new GroupChat(
    openAIClient,
    model,
    admin,
    new[]
    {
        coder,
        runner,
    });

admin.FunctionMaps.Add(groupChat.TerminateGroupChatFunction, groupChat.TerminateGroupChatWrapper);

groupChat.AddInitializeMessage("Welcome to the group chat! Work together to resolve my task.", admin.Name);
groupChat.AddInitializeMessage("Hey", coder.Name);
groupChat.AddInitializeMessage("Hey", runner.Name);
groupChat.AddInitializeMessage($"The task is: retrieve the latest PR from mlnet repo, print the result and save the result to pr.txt.", admin.Name);
groupChat.AddInitializeMessage($"The link to mlnet repo is: https://github.com/dotnet/machinelearning. you don't need a token to use github pr api. Make sure to include a User-Agent header, otherwise github will reject it.", admin.Name);
groupChat.AddInitializeMessage(@$"Here's the step-by-step plan
1. Send a GET request to the GitHub API to retrieve the list of pull requests for the mlnet repo.
2. Parse the response JSON to extract the latest pull request.
3. Print the result to the console and save the result to a file named ""pr.txt"".
", admin.Name);
var conversation = await admin.SendMessageAsync("Coder, write code to resolve step 1", groupChat, 100, false);

// log conversation to chat_history.txt
if(conversation is not null)
{
    var sb = new StringBuilder();
    foreach(var (message, name) in conversation)
    {
        var fmtMsg = groupChat.FormatMessage(message, name);
        sb.AppendLine(fmtMsg);
    }

    var chatHistoryPath = Path.Combine(workDir, "chat_history.txt");
    await logger.LogToFile("chat_history.txt", sb.ToString());
    logger.Log($"Chat history is logged to {chatHistoryPath}");
}
