using Azure.AI.OpenAI;
using GroupChatExample.DotnetInteractiveService;
using GroupChatExample.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupChatExample.CodingTask
{
    public partial class EngineerFunction
    {
        private readonly DotnetInteractiveFunction dotnetInteractiveFunction;
        private readonly MLNetExample101Function mltask101Function;
        private readonly Logger? logger;

        public EngineerFunction(DotnetInteractiveFunction interactiveFunction, MLNetExample101Function mlnetFunction, Logger? logger = null)
        {
            this.dotnetInteractiveFunction = interactiveFunction;
            this.mltask101Function = mlnetFunction;
            this.logger = logger;
        }


        /// <summary>
        /// Complete architect step.
        /// </summary>
        /// <param name="task">the overall task.</param>
        /// <param name="taskDescirption">task description.</param>
        /// <param name="step">the step to complete in this round.</param>
        /// <param name="existingSolution">existing solution.</param>
        [FunctionAttribution]
        public async Task<string> CompleteArchitectStep(string task, string taskDescirption, string step, string existingSolution)
        {
            using var engineer = new ChatAgent(
            Constant.GPT35,
            Constant.GPT_35_MODEL_ID,
            name: "Engineer",
            roleInformation: @$"Resolve the step using csharp and ask Executor to verify your code.
Once Executor verify your code, reply [STEP_COMPLETE]. If your code has error, you can ask Examplar to help you fix it.
## Context ##
Task: {task}
Task Description: {taskDescirption}

Current Step To Resolve:
{step}

Existing Code From Previous Steps:
{existingSolution}
## End ##

Here're a few response example you can refer to:
## Example 1
```csharp
...
```

Executor, run the code above and verify it.

## Example 2
```nuget
...
```
Executor, install the nuget package above.

## Example 3
Examplar, can you provide some example for this step?

## Example 4
Examplar, can you help me fix the error in the code above?
...");

            using var examplar = new ChatAgent(
                Constant.GPT35,
                Constant.GPT_35_MODEL_ID,
                name: "Examplar",
                roleInformation: @"You either fix error from engineer's code, or find mlnet api example if Engineer using the mlnet api in a wrong way.",
                functionMaps: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
                {
                { mltask101Function.SearchMLNetApiExampleFunction, mltask101Function.SearchMLNetApiExampleWrapper },
                { mltask101Function.FixErrorFunction, mltask101Function.FixErrorWrapper },
                });

            using var executor = new ChatAgent(
                Constant.GPT35,
                Constant.GPT_35_MODEL_ID,
                name: "Executor",
                roleInformation: @"You run code from Engineer. If Engineer provide nuget install code, install them using InstallNugetPackages.
If Engineer provide csharp code, running them using RunCode and return result.",
                functionMaps: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
                {
                { dotnetInteractiveFunction.InstallNugetPackagesFunction, dotnetInteractiveFunction.InstallNugetPackagesWrapper },
                { dotnetInteractiveFunction.RunCodeFunction, dotnetInteractiveFunction.RunCodeWrapper},
                });

            var terminateGroupFunction = new AdminFunction();
            using var admin = new ChatAgent(
                Constant.GPT35,
                Constant.GPT_35_MODEL_ID,
                name: "Admin",
                roleInformation: "You terminate group chat when Engineer resolve given step successfully. If code has bug. you ask Engineer to resolve the bug.",
                functionMaps: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
                {
                    { terminateGroupFunction.TaskCompletedSuccessfullyFunction, terminateGroupFunction.TaskCompletedSuccessfullyWrapper },
                    { terminateGroupFunction.AskEngineerToFixBugFunction, terminateGroupFunction.AskEngineerToFixBugWrapper },
                });

            var initialChatMessages = new (ChatMessage, string)[]
            {
                (
                    new ChatMessage
                    {
                        Role = ChatRole.User,
                        Content = "Welcome to the chat group, work together to resolve the given step for a task. I'll terminate the group chat once the give step is resolved."
                    },
                    admin.Name
                ),
                (
                    new ChatMessage
                    {
                        Role = ChatRole.User,
                        Content = "I'll write dotnet code to resolve the given step. I'll say [STEP_COMPLETE] once the step resolved"
                    },
                    engineer.Name
                ),
                (
                    new ChatMessage()
                    {
                        Role = ChatRole.User,
                        Content = "I run dotnet code from Engineer and return result. I'll also report any bug during executing code.",
                    },
                    executor.Name
                ),
                (
                    new ChatMessage()
                    {
                        Role = ChatRole.User,
                        Content = "I'll provide mlnet example"
                    },
                    examplar.Name
                ),
                (
                    new ChatMessage
                    {
                        Role = ChatRole.User,
                        Content = $"engineer, first ask examplar to provide some examples for current step, then resolve this given step: {step}. If code has error, you can ask examplar help you fix it."
                    },
                    admin.Name
                ),
            };

            var group = new GroupChat(
                Constant.GPT35,
                Constant.GPT_35_MODEL_ID,
                admin,
                new[]
                {
                    engineer,
                    examplar,
                    executor,
                },
                initialChatMessages);

            // pretty print messages
            foreach (var message in initialChatMessages)
            {
                group.PrettyPrintMessage(message.Item1, message.Item2);
            }

            try
            {
                var chatHistory = await group.CallAsync(Enumerable.Empty<(ChatMessage, string)>(), 20);

                if (chatHistory is null)
                {
                    throw new Exception("chat history is null");
                }

                var summarizeCodeMessage = new ChatMessage
                {
                    Role = ChatRole.User,
                    Content = "engineer, please summarize your solution to the given step and return to me. Put your solution between ```csharp and ```"
                };

                group.PrettyPrintMessage(summarizeCodeMessage, admin.Name);
                chatHistory = await group.CallAsync(chatHistory.Append((summarizeCodeMessage, admin.Name)), 1, throwExceptionWhenMaxRoundReached: false);
                var lastMessage = chatHistory?.Last();
                if (lastMessage?.Item1 is ChatMessage msg && lastMessage?.Item2 is string name && name == engineer.Name)
                {
                    return msg.Content;
                }

                return $"fail to resole the given task";
            }
            catch (Exception ex)
            {
                return $"fail to resole the given task: error: {ex.Message}";
            }
        }

        /// <summary>
        /// Create and save end to end solution.
        /// </summary>
        /// <param name="codeToSave">code to save.</param>
        [FunctionAttribution]
        public async Task<string> CreateAndSaveEndToEndSolution(string codeToSave)
        {
            this.logger?.LogToFile("End2EndSolution.cs", codeToSave);

            return $"Save code to End2EndSolution.cs successfully, The complete code is {codeToSave}";
        }
    }
}
