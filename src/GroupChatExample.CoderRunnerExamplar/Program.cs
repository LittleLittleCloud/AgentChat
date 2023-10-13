// See https://aka.ms/new-console-template for more information
using Azure.AI.OpenAI;
using GroupChatExample.CoderRunnerExamplar;
using GroupChatExample.DotnetInteractiveService;
using GroupChatExample.Helper;
using System.Reflection;
using System.Text;

var workDir = Path.Combine(Path.GetTempPath(), "CoderRunnerExamplar");

// create workDir if not exists
if (!Directory.Exists(workDir))
{
    Directory.CreateDirectory(workDir);
}

using var service = new InteractiveService(workDir);
using var httpClient = new HttpClient();
await service.StartAsync(workDir, default);
var logger = new Logger(workDir);
var notebookPath = Path.Combine(workDir, "notebook.ipynb");
using var dotnetInteractiveFunction = new DotnetInteractiveFunction(service, notebookPath, logger: logger, true);
var OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set");
var model = Constant.AZURE_GPT_35_MODEL_ID;
var openAIClient = new OpenAIClient(OPENAI_API_KEY);
openAIClient = Constant.AzureGPT35;
var fixInvalidJsonFunction = new FixInvalidJsonFunctionWrapper(openAIClient, model);
var exampleFunction = new MLNetExamplarFunction(httpClient, openAIClient, model);
var coder = new ChatAgent(
        Constant.AzureGPT4,
        Constant.AZURE_GPT_4_MODEL_ID,
        "Coder",
        @"You write dotnet script to resolve tasks.
You implement given step based on previous context. You don't need to provide complete code, just add the code based on previous context.

e.g.
```nuget
// install xx packages
```

```csharp
...
```
end

Here're some rules to follow when you write dotnet code:
- Remove Main function and use Top-level statement.
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
You can only reply with RunCodeFunction or InstallNugetPackagesFunction or 'No code to run' or 'Goodbye'.
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
    @"You are mlnet Examplar. You provide mlnet api examples and fix mlnet error.
You can only reply with SearchMLNetApiExampleFunction or FixMLNetErrorFunction or 'I don't know'.

Here're some examples
- Example 1 -
SearchMLNetApiExample( //arguments)

- Example 2 -
I don't know, question is not related to mlnet.
",
    new Dictionary<FunctionDefinition, Func<string, Task<string>>>
    {
        { exampleFunction.SearchMLNetApiExampleFunction, exampleFunction.SearchMLNetApiExampleWrapper },
        { exampleFunction.FixMLNetErrorFunction, exampleFunction.FixMLNetErrorWrapper },
    });

var admin = new ChatAgent(
    openAIClient,
    model,
    "Admin",
    @"You lead the group to resolve task.
First load previous context and continue from there. Otherwise, start from the first step.
To resolve each step, you ask Examplar to provide examples. then ask Coder to implement the step, then ask Runner to run the code.
If the code is not valid, ask Coder to fix the code.
If the error is related to mlnet, ask Examplar to fix MLNet error.
You save context every 10 rounds. Or when a step is resolved.
If Coder fails to write correct code for over 3 times, try the current step again by loading previous context.

Here're some response examples.
- Examplar, provide mlnet example for xxx
- Coder, implement download file step
- Runner, run code from Coder.
- Examplar, fix the mlnet error.
- Current step is resolved, save context.
- Three strikes, load previous context and try again.
");

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

var contextPath = Path.Combine(workDir, "context.txt");
var summarizeFunction = new ContextManagementFunction(contextPath);
admin.FunctionMaps.Add(summarizeFunction.LoadContextFunction, summarizeFunction.LoadContextWrapper);
admin.FunctionMaps.Add(groupChat.TerminateGroupChatFunction, groupChat.TerminateGroupChatWrapper);
admin.FunctionMaps.Add(summarizeFunction.SaveContextFunction, async (args) =>
{
    var summarizeConversationWrapper = fixInvalidJsonFunction.FixInvalidJsonWrapper(summarizeFunction.SaveContextWrapper);
    var context = await summarizeConversationWrapper(args);

    context = await groupChat.SummarizeConversation(context);

    return context;
});

groupChat.AddInitializeMessage("Welcome to the group chat! Work together to resolve my task.", admin.Name);
groupChat.AddInitializeMessage("Hey", coder.Name);
groupChat.AddInitializeMessage("Hey", runner.Name);
groupChat.AddInitializeMessage("Hey", examplar.Name);
groupChat.AddInitializeMessage("For each step, I'll first ask Examplar to provide mlnet example", admin.Name);
groupChat.AddInitializeMessage("Then I'll ask Coder to write code", admin.Name);
groupChat.AddInitializeMessage("Then I'll ask Runner to run the code", admin.Name);
groupChat.AddInitializeMessage("Once a step is completed, I'll save the context", admin.Name);
groupChat.AddInitializeMessage(@$"The task is: Train a lightGBM binary classification model using mlnet.
- first, install necessary nuget packages and include namespaces.
- then create a dummy dataset with at least 100 rows and four features.
- then create a binary classification pipeline using lightGBM.
- Then train the pipeline using dummy data and print accuracy
- finally, save the model to lgbm.mlnet", admin.Name);

var conversation = await admin.SendMessageAsync("Let me load previous context first", groupChat, 100);

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
