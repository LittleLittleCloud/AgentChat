// See https://aka.ms/new-console-template for more information
using Azure.AI.OpenAI;
using GroupChatExample.DotnetInteractiveService;
using GroupChatExample.Helper;

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
using var dotnetInteractiveFunction = new DotnetInteractiveFunction(service, logger);
var OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set");
var model = "gpt-3.5-turbo-0613";
var openAIClient = new OpenAIClient(OPENAI_API_KEY);
var fixInvalidJsonFunction = new FixInvalidJsonFunctionWrapper(openAIClient, model);

var coder = new ChatAgent(
        openAIClient,
        model,
        "Coder",
        @"You are dotnet coder, you write dotnet script to resolve tasks.
First, create a step-by-step plan for the task. 
Then write code to resolve each step.
Complete one step at a time, once you complete a step, you ask runner to run that step for you.
e.g.
- step1: download file
```
...
```

runner, run step 1 for me.

Once you complete all steps, rely [COMPLETE].
Here're some rules to follow when you write dotnet code:
- Write code in dotnet interactive. You can use `#r` to reference nuget package.
- Don't write function, just write code, like what python does.
- Don't use `using` statement. Runner can't handle it.
- Try to use `var` instead of explicit type.
- Try avoid using external library.
- Always print out the result to console. Don't write code that doesn't print out anything.
");

var runner = new ChatAgent(
        openAIClient,
        model,
        "Runner",
        @"You are a helpful code runner. When a latest message come, do the following check step by step
- If the latest message contains no code block or nuget packages to install, say 'No code available'.
- If the latest message contains csharp block, call run code function to run the code. Pass the code as is.
- If the latest message contains nuget packages to install, call install nuget package function to install packages.

Here're some examples
- Example 1 -
No code available

- Example 2 -
RunCode( //arguments)

- Example 3 -
InstallNugetPackages( //arguments)
",
        new Dictionary<FunctionDefinition, Func<string, Task<string>>>
        {
            { dotnetInteractiveFunction.RunCodeFunction, fixInvalidJsonFunction.FixInvalidJsonWrapper(dotnetInteractiveFunction.RunCodeWrapper) },
            { dotnetInteractiveFunction.InstallNugetPackagesFunction, dotnetInteractiveFunction.InstallNugetPackagesWrapper },
        });

var admin = new ChatAgent(
    openAIClient,
    model,
    "Admin",
    @"You are admin, you provide task to coder and runner. You terminate group chat when task resolved successfully. Otherwise you ask coder to resolve your task.");

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

groupChat.AddMessage("Welcome to the group chat! Work together to resolve my task.", admin.Name);
groupChat.AddMessage("I'll write dotnet code to resolve Admin's task. I'll fix any bugs from Runner", coder.Name);
groupChat.AddMessage("I'll review and run dotnet code from Coder and return result.", runner.Name);
groupChat.AddMessage($"The task is: retrieve the latest PR from mlnet repo, print the result and save the result to pr.txt.", admin.Name);
groupChat.AddMessage($"The link to mlnet repo is: https://github.com/dotnet/machinelearning. You don't need to pass a token as this api is public available. Make sure to include a User-Agent header, otherwise github will reject it.", admin.Name);

var conversation = await groupChat.CallAsync(maxRound: 20);
