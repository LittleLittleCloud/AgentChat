// See https://aka.ms/new-console-template for more information
using Azure.AI.OpenAI;
using AgentChat.CoderRunnerExamplar;
using AgentChat.DotnetInteractiveService;
using System.Text;
using AgentChat.Example.Share;
using AgentChat;

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
using var dotnetInteractiveFunction = new DotnetInteractiveFunction(service, notebookPath, true);

var fixInvalidJsonFunction = new FixInvalidJsonFunctionWrapper(Constant.GPT35);
var exampleFunction = new MLNetExamplarFunction(httpClient, Constant.GPT35);
var coder = Constant.GPT4.CreateAgent(
        name: "Coder",
        roleInformation: @"You act as dotnet coder, you write dotnet script to resolve tasks.
Here's the workflow you follow:
-workflow-
if no_current_step
    ask_for_current_step
else if no_example_code
    ask_examplar_to_provide_mlnet_example
else
    write_code_to_resolve_current_step
wait_for_code_to_be_run
if code_has_error
    fix_code_error and wait_for_code_to_be_run
-end-

Here're some rules to follow on write_code_to_resolve_current_step:
- put code between ```csharp and ```
- Use top-level statements, remove main function, just write code, like what python does.
- Remove all `using` statement.
- Try to use `var` instead of explicit type.
- Try avoid using external library.
- Don't use external data source, like file, database, etc. Create a dummy dataset if you need.
- Always print out the result to console. Don't write code that doesn't print out anything.

Here are some examples for ask_for_current_step:
- No current step is provided. Please provide current step.

Here are some examples for write_code_to_resolve_current_step:
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
```

Here are some examples for wait_for_code_to_be_run:
- Runner, please run the code.
");


var runner = Constant.GPT35.CreateAgent(
        name: "Runner",
        roleInformation: @"You act as dotnet runner, you run dotnet script and install nuget packages. Here's the workflow you follow:
-workflow-
if code_is_available_from_latest_message
    if nuget_packages_is_available_from_latest_message, call install_nuget_packages
    call run_code
else
    ask_for_code_to_run
-end-

Here are some examples for ask_for_code_to_run:
- No code is provided. Please provide code to run.

Here are some examples for install_nuget_packages:
- install_nuget_packages // nuget packages to install
",
        functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
        {
            { dotnetInteractiveFunction.RunCodeFunction, fixInvalidJsonFunction.FixInvalidJsonWrapper(dotnetInteractiveFunction.RunCodeWrapper) },
            { dotnetInteractiveFunction.InstallNugetPackagesFunction, dotnetInteractiveFunction.InstallNugetPackagesWrapper },
        });

var examplar = Constant.GPT35.CreateAgent(
    name: "Examplar",
    roleInformation: @"You act as mlnet expert that provide mlnet example and fix mlnet error. Here's the workflow you follow:
-workflow-
if step_is_given and no_example_code
    call search_mlnet_api_example
else if mlnet_error_is_given
    call fix_mlnet_error
else
    no_op
-end-

Here are some examples for no_op:
- example code is provided, and no error is given. No op.
",
    functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
    {
        { exampleFunction.SearchMLNetApiExampleFunction, exampleFunction.SearchMLNetApiExampleWrapper },
        { exampleFunction.FixMLNetErrorFunction, exampleFunction.FixMLNetErrorWrapper },
    });

var contextPath = Path.Combine(workDir, "context.txt");
var contextManagementFunction = new ContextManagementFunction(contextPath);
var groupChatFunction = new GroupChatFunction();

var admin = Constant.GPT4.CreateAgent(
    name: "Admin",
    roleInformation: @"You act as group admin that lead other agents to resolve task together. Here's the workflow you follow:
-workflow-
if current_step is resolved
    save_context
    provide_next_step

if all_steps_are_resolved
    terminate_chat
-end-

The task is Train a lightGBM binary classification model using mlnet.

The steps are:
- install necessary nuget packages (Microsoft.ML and Microsoft.ML.LightGBM) and include namespaces.
- create a DummyData class with four numeric features and one bool label.
- generate 1000 rows of DummyData, and split the data into train and test set.
- Create a LightGBM binary classification pipeline. The pipeline should first concatenate all features into a single column, followed by a LightGbm binary classification trainer.
- train the model from pipeline and train set and evaluate the model with test data.
- save the model to lgbm.mlnet

Here are some examples for provide_next_step:
- The next step to resolve is xxx. Please resolve it.
",
    functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
    {
        { groupChatFunction.TerminateGroupChatFunction, groupChatFunction.TerminateGroupChatWrapper },
        { contextManagementFunction.SaveContextFunction, async (args) =>
{
    var summarizeConversationWrapper = fixInvalidJsonFunction.FixInvalidJsonWrapper(contextManagementFunction.SaveContextWrapper);
    var context = await summarizeConversationWrapper(args);

    context = await groupChatFunction.ClearGroupChat(context);

    return context;
}},
        { contextManagementFunction.LoadContextFunction, async (args) =>
{
    var loadContextWrapper = fixInvalidJsonFunction.FixInvalidJsonWrapper(contextManagementFunction.LoadContextWrapper);
    var context = await loadContextWrapper(args);
    context = await groupChatFunction.ClearGroupChat(context);
    return context;
}},
    });

var groupChat = new GroupChat(
    Constant.GPT35,
    admin,
    new[]
    {
        coder,
        runner,
        examplar,
    });

admin.AddInitializeMessage("Welcome to the group chat! I'm Admin. Work together to resolve my task.", groupChat);
coder.AddInitializeMessage("Hey I'm Coder", groupChat);
runner.AddInitializeMessage("Hey I'm Runner", groupChat);
examplar.AddInitializeMessage("Hey I'm Examplar", groupChat);
admin.AddInitializeMessage(@$"Here's the workflow for this group chat
-group_chat_workflow-
admin_load_previous_context and provide_next_step
admin_save_context_for_every_10_new_messages
if all_steps_are_resolved
    terminate_chat
else
    admin_provide_next_step
    examplar_provide_mlnet_example
    coder_write_code_to_resolve_step
    runner_run_code

if no_error_from_runner
    admin_save_context
    admin_provide_next_step
else
    examplar_fix_mlnet_error or coder_fix_code_error
-end-
", groupChat);

var conversation = await groupChat.CallAsync(null, maxRound: 200, true);

// log conversation to chat_history.txt
if (conversation is not null)
{
    var sb = new StringBuilder();
    foreach (var message in conversation)
    {
        var fmtMsg = groupChat.FormatMessage(message);
        sb.AppendLine(fmtMsg);
    }

    var chatHistoryPath = Path.Combine(workDir, "chat_history.txt");
    await logger.LogToFile("chat_history.txt", sb.ToString());
    logger.Log($"Chat history is logged to {chatHistoryPath}");
}
