// See https://aka.ms/new-console-template for more information
using Azure.AI.OpenAI;
using GroupChatExample.CoderRunnerExamplar;
using GroupChatExample.DotnetInteractiveService;
using GroupChatExample.Helper;
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
using var httpClient = new HttpClient();
await service.StartAsync(workDir, default);
var logger = new Logger(workDir);
var notebookPath = Path.Combine(workDir, "notebook.ipynb");
using var dotnetInteractiveFunction = new DotnetInteractiveFunction(service, notebookPath, logger: logger);
var OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set");
var model = Constant.AZURE_GPT_4_MODEL_ID;
var openAIClient = new OpenAIClient(OPENAI_API_KEY);
openAIClient = Constant.AzureGPT4;
var fixInvalidJsonFunction = new FixInvalidJsonFunctionWrapper(openAIClient, model);
var exampleFunction = new MLNetExamplarFunction(httpClient, openAIClient, model);
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
...
```
end

Here're some rules to follow when you write dotnet code:
- Don't write main function, just write code, like what python does.
- Don't use `using` statement. Runner can't handle it.
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

var examplar = new ChatAgent(
    openAIClient,
    model,
    "Examplar",
    @"You are mlnet Examplar. You provide mlnet api examples.
- if runner ask for mlnet api example, call search mlnet api example function to search mlnet api example.
- if question is not related to mlnet, say 'I don't know'.

Here're some examples
- Example 1 -
SearchMLNetApiExample( //arguments)

- Example 3 -
I don't know, question is not related to mlnet.
",
    new Dictionary<FunctionDefinition, Func<string, Task<string>>>
    {
        { exampleFunction.SearchMLNetApiExampleFunction, exampleFunction.SearchMLNetApiExampleWrapper },
    });

var admin = new ChatAgent(
    openAIClient,
    model,
    "Admin",
    @"You are admin, you provide task to coder and runner.
For each step, you ask Examplar to provide examples. then ask Coder to implement the step, then ask Runner to run the code.
If the code is not valid, ask Coder to fix the code.
e.g.
Examplar, provide mlnet example for xxx
Coder, implement download file step
Runner, run code.
Coder, fix the code.

You terminate group chat when task resolved successfully. Otherwise you ask coder to resolve your task.");

var groupChat = new GroupChat(
    openAIClient,
    model,
    admin,
    new[]
    {
        coder,
        runner,
        examplar,
    });

admin.FunctionMaps.Add(groupChat.TerminateGroupChatFunction, groupChat.TerminateGroupChatWrapper);

groupChat.AddMessage("Welcome to the group chat! Work together to resolve my task. I'll terminate the group chat when task get resolved", admin.Name);
//groupChat.AddMessage("I'll write csharp code to resolve given step. I'll first write code, then ask Runner to run the code.", coder.Name);
//groupChat.AddMessage("I'll run code after Coder provide code", runner.Name);
//groupChat.AddMessage("I'll provide mlnet example for each step and fix mlnet-related error", examplar.Name);
groupChat.AddMessage(@$"The task is: Train a lightGBM binary classification model using mlnet.
- first, install necessary nuget packages and include namespaces.
- then create a dummy dataset with at least 100 rows and four features.
- then create a binary classification pipeline using lightGBM.
- Then train the pipeline using dummy data and print accuracy
- finally, save the model to lgbm.mlnet", admin.Name);
groupChat.AddMessage("Examplar, provide some examples on installing nuget packages and include namespaces", admin.Name);
var conversation = await groupChat.CallAsync(maxRound: 60);

// log conversation to chat_history.txt
if (conversation is not null)
{
    var sb = new StringBuilder();
    foreach (var (message, name) in conversation)
    {
        var fmtMsg = groupChat.FormatMessage(message, name);
        sb.AppendLine(fmtMsg);
    }

    var chatHistoryPath = Path.Combine(workDir, "chat_history.txt");
    await logger.LogToFile("chat_history.txt", sb.ToString());
    logger.Log($"Chat history is logged to {chatHistoryPath}");
}
