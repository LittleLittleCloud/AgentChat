using Azure.AI.OpenAI;
using GroupChatExample.CodingTask;
using GroupChatExample.DotnetInteractiveService;
using GroupChatExample.Helper;
using FluentAssertions.Equivalency;
using System.Text.Json;

internal static partial class Program
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
    };
    async static Task<bool> RunCodingTask(DataPoint task, string workingDir, int maxRound = 30)
    {
        var logger = new Logger(workingDir);
        // check if task is resolved by examining if output file exist
        var outputFilePath = Path.Combine(workingDir, task.OutputFileName);
        if (File.Exists(outputFilePath))
        {
            File.Delete(outputFilePath);
        }

        using var interactiveService = new InteractiveService(workingDir);
        await interactiveService.StartAsync(workingDir, CancellationToken.None);

        using var dotnetInteractiveFunction = new DotnetInteractiveFunction(interactiveService, logger: logger);
        var mltask101Function = new MLNetExample101Function(new HttpClient());

        var architect = new ChatAgent(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            "Architect",
            @$"You break down the task into steps and write general instruction for each step. You don't write code!
Don't be too verbose. Just briefly describe what need to be done in each step. The final step should always be providing end-to-end code solution.
For example
- step1: install nuget package
- step2: import all namespaces
- step3: create dummy dataset

Once you create a step-by-step plan, asking engineer to implement your plan step by step. For example
###
- step1: install nuget package

engineer, implement step 1.

Once engineer complete one step, provide them with the next step.");
        var engineerFunction = new EngineerFunction(dotnetInteractiveFunction, mltask101Function, logger);
        using var engineer = new ChatAgent(
            Constant.AzureGPT4,
            Constant.GPT_4_MODEL_ID,
            name: "Engineer",
            roleInformation: @"You are a function caller. You always call CompleteArchitechStep to complete a step.
Once you complete all steps, create and save the end to end solution. Then reply [COMPLETE]",
            functionMaps: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
            {
                { engineerFunction.CompleteArchitectStepFunction, engineerFunction.CompleteArchitectStepWrapper },
                { engineerFunction.CreateAndSaveEndToEndSolutionFunction, engineerFunction.CreateAndSaveEndToEndSolutionWrapper },
            });

        using var examplar = new ChatAgent(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            name: "Examplar",
            roleInformation: @"You help Engineer fix code errors by providing MLNet examples.
Either find similar code for Engineer to reference, or find mlnet api example if Engineer using the mlnet api in a wrong way.",
            functionMaps: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
            {
                { mltask101Function.SearchMLNetApiExampleFunction, mltask101Function.SearchMLNetApiExampleWrapper },
                { mltask101Function.FixErrorFunction, mltask101Function.FixErrorWrapper },
            });

        using var executor = new ChatAgent(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            name: "Executor",
            roleInformation: @"You run code from Engineer. If Engineer provide nuget install code, install them using InstallNugetPackages.
If Engineer provide csharp code, running them using RunCode and return result.",
            functionMaps: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
            {
                { dotnetInteractiveFunction.InstallNugetPackagesFunction, dotnetInteractiveFunction.InstallNugetPackagesWrapper },
                { dotnetInteractiveFunction.RunCodeFunction, dotnetInteractiveFunction.RunCodeWrapper},
            });

        var groupChatFunction = new AdminFunction();
        using var admin = new ChatAgent(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            name: "Admin",
            roleInformation: "You say [TERMINATE] when task get resolved successfully. Otherwise you ask Engineer to resolve your task.",
            functionMaps: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
            {
                { groupChatFunction.TaskCompletedSuccessfullyFunction, groupChatFunction.TaskCompletedSuccessfullyWrapper },
            });

        var initialChatMessages = new (ChatMessage, string)[]
        {
    (
    new ChatMessage()
    {
        Role = ChatRole.User,
        Content = "Welcome to the group chat! Work together to resolve my task. I'll say [TERMINATE] if task get resolved. ",
    },
    admin.Name
    ),
    (
    new ChatMessage()
    {
        Role = ChatRole.User,
        Content = "I'll write dotnet code step by step to resolve Admin's task. I'll also search api document if code fail to compile",
    },
    engineer.Name
    ),
    //(
    //new ChatMessage()
    //{
    //    Role = ChatRole.User,
    //    Content = "I run dotnet code from Engineer and return result. I'll also report any bug during executing code.",
    //},
    //executor.Name
    //),
    (
    new ChatMessage()
    {
        Role = ChatRole.User,
        Content = "I provide step-by-step plan for Engineer to resolve the task.",
    },
    architect.Name
    ),
    //(
    //new ChatMessage()
    //{
    //    Role = ChatRole.User,
    //    Content = "for each step from Architech, I'll provide MLNet example when necessary",
    //},
    //examplar.Name
    //),
        };

        var groupChat = new GroupChat(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            admin,
            new[] { engineer, architect, executor },
            initialChatMessages);

        // clear output
        var msg = (
            new ChatMessage()
            {
                Role = ChatRole.User,
                Content = $@"Task Name: {task.Name}
Description: {task.Description}.

Architect, create a step-by-step plan to resolve my task and ask engineer to implement each steps."
            },
                   admin.Name
                      );

        // pretty print messages
        foreach (var message in initialChatMessages.Concat(new[] { msg }))
        {
            groupChat.PrettyPrintMessage(message.Item1, message.Item2);
        }

        var completeChatHistory = await groupChat.CallAsync(new[] { msg }, maxRound);

        //var summarizeChatHistoryMessage = new ChatMessage
        //{
        //    Role = ChatRole.User,
        //    Content = "engineer, run this step: aggregate the code from each step altogether into a complete code solution and put the code between ```csharp and ```.",
        //};
        //completeChatHistory = completeChatHistory!.Append((summarizeChatHistoryMessage, architect.Name));
        //completeChatHistory = await groupChat.CallAsync(completeChatHistory, 1);

        // log chat history
        var json = JsonSerializer.Serialize(completeChatHistory?.Select(c =>
        {
            return new
            {
                content = c.Item1.Content,
                role = c.Item1.Role.ToString(),
                name = c.Item2,
            };
        }), jsonSerializerOptions);

        await logger.LogToFile("chat_history.json", json);


        var isTaskResolved = File.Exists(outputFilePath);

        return isTaskResolved;
    }
}
