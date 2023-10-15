using Azure.AI.OpenAI;
using GroupChatExample.CodingTask;
using GroupChatExample.Helper;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
internal static partial class Program
{
    async static Task<bool> WriteNotebookTask(DataPoint task, string workingDir, int maxRound = 20, Logger? logger = null)
    {
        // create notebook
        // step 1: load csharp code from the most recent .cs file
        var notebookPath = Path.Combine(workingDir, $"{task.Name}.ipynb");
        if (File.Exists(notebookPath))
        {
            File.Delete(notebookPath);
        }

        var files = Directory.GetFiles(workingDir, "*.cs");
        var csharpCode = files.OrderByDescending(f => f).FirstOrDefault();
        if (csharpCode is null)
        {
            Console.WriteLine($"No csharp code found in {workingDir}. Skip it.");
            return false;
        }

        var code = await File.ReadAllTextAsync(csharpCode);

        // step 2: create group chat
        var engineer = new GPTAgent(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            "engineer",
            $@"You are engineer. Follow steps from architect and resolve task, once you resolve the task, reply [COMPLETE].
For each step, you can provide a csharp code, a markdown or a nuget installation script and send it to notebook_writer.
If there's any error from your code, notebook_writer will let you know. Fix the error and reimpement the step.
Implement one step at a time. Don't skip any step. Don't implement multiple steps at the same time.
here're some examples you can send to notebook_writer:
-- example 1 --
step1: add task name and description
```markdown
...
```
notebook_writer, save this step as cell #1. The cell type is markdown cell. The cell index is 1.

## Reference ##
The following code might be helpful for you to implement each step.
```csharp
{code}
```");

        using var notebookWriterFunction = new NotebookWriterFunction(notebookPath, logger);
        var notebookWriter = new GPTAgent(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            "notebook_writer",
            $@"Follow engineer's instruction to create notebook. The full path of notebook should be {notebookPath}.
if engineer provdes a csharp code, add it as csharp cell.
if engineer provdes a markdown, add it as markdown cell.
if engineer provdes a nuget installation script, add it as nuget installation cell, don't include package version in nuget package.
",
            new Dictionary<FunctionDefinition, Func<string, Task<string>>>()
            {
                { notebookWriterFunction.AddCSharpCodeCellFunction, notebookWriterFunction.AddCSharpCodeCellWrapper },
                { notebookWriterFunction.AddMarkdownCellFunction, notebookWriterFunction.AddMarkdownCellWrapper },
                { notebookWriterFunction.AddNugetInstallationCellFunction, notebookWriterFunction.AddNugetInstallationCellWrapper },
            });

        var admin = new GPTAgent(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            "admin",
            "You are admin. You say [TERMINATE] when notebook get created");

        var initializeChatMessages = new (ChatMessage, string)[]
        {
        (
            new ChatMessage
            {
                Role = ChatRole.User,
                Content = "Welcome to the group chat! Work together to create notebook for an mlnet beginner task. I'll say [TERMINATE] if notebook get created. ",
            },
            admin.Name
        ),
        (
            new ChatMessage
            {
                Role = ChatRole.User,
                Content = "I'll save engineer's notebook to disk.",
            },
            notebookWriter.Name
        ),
        (
            new ChatMessage
            {
                Role = ChatRole.User,
                Content = "I'll write notebook solution",
            },
            engineer.Name
        ),
        (
            new ChatMessage
            {
                Role = ChatRole.User,
                Content = $@"task name: {task.Name}",
            },
            admin.Name
        ),
        (
            new ChatMessage
            {
                Role = ChatRole.User,
                Content = $@"task description: {task.Description}",
            },
            admin.Name
        ),
        (
        new ChatMessage()
        {
            Role = ChatRole.User,
            Content = $"engineer, create notebook for resolving the task. notebook_writer, save notebook to disk and add cells. The full path of notebook should be {notebookPath}."
        },
        admin.Name)
        };

        var groupChat = new GroupChat(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            admin,
            new[] {
                engineer,
                notebookWriter },
            initializeChatMessages);

        // pretty print messages
        foreach (var message in initializeChatMessages)
        {
            groupChat.PrettyPrintMessage(message.Item1, message.Item2);
        }

        var msg = (
            new ChatMessage()
            {
                Role = ChatRole.User,
                Content = $"please create a notebook and add content. The full path of notebook should be {notebookPath}"
            },
            admin.Name);

        var completeChatHistory = await groupChat.CallAsync(new (ChatMessage, string)[0] { }, maxRound);
        // log chat history
        var json = JsonSerializer.Serialize(initializeChatMessages.Concat(completeChatHistory!).Select(c =>
        {
            return new
            {
                content = c.Item1.Content,
                role = c.Item1.Role.ToString(),
                name = c.Item2,
            };
        }), jsonSerializerOptions);

        await File.WriteAllTextAsync(Path.Combine(workingDir, "create_notebook_history.json"), json);

        // check if task is resolved by examining if output file exist
        var isTaskResolved = File.Exists(notebookPath);

        return isTaskResolved;
    }

    async static Task<bool> ConvertCSharpToNotebookTaskAsync(string csharpCodePath, string workDir, int maxRound = 20, Logger? logger = null)
    {
        // output directory
        // workDir/name of csharp code
        var outputDir = workDir;
        if (Directory.Exists(outputDir))
        {
            logger?.Log($"Delete directory {outputDir}");
            Directory.Delete(outputDir, true);
        }

        logger?.Log($"Create directory {outputDir}");
        Directory.CreateDirectory(outputDir);

        var outputNotebookPath = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(csharpCodePath)}.ipynb");
        var outputLabelPath = Path.Combine(outputDir, $"label.json");
        var code = await File.ReadAllTextAsync(csharpCodePath);
        var createLabelFunction = new CreateLabel(logger);

        // create agents
        var labeller = new GPTAgent(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            "labeller",
            @$"You are labeller,

## provided csharp code
```csharp
{code}
```
## end of provided csharp code
First You create task that can be resolve by provided csharp code. The task should save result to an output file if there's any.
Then you create label.json for csharp code and save the label.json to {workDir}",
            new Dictionary<FunctionDefinition, Func<string, Task<string>>>
            {
                { createLabelFunction.SaveTaskFunction, createLabelFunction.SaveTaskWrapper }
            });

        var architect = new GPTAgent(
            Constant.GPT,
            Constant.GPT_4_MODEL_ID,
            "architect",
            @$"You are architect. you briefly explain what to do in each step.
your plan should start with adding task name and description.
Don't be too verbose.
For example
- step1: (required) add task name and description
    '''markdown
    task name: xxx
    task description: xxx
    '''
- step2: (required) install nuget package
    '''nuget
    dotnet nuget install xxx
    '''
- step3: (required) import all namespaces
    ```csharp
    using xxx
    ```
- step4: create MLContext
    ```csharp
    var mlContext = new MLContext();
    ```
- step5: create dummy dataset
    ```csharp
    var data = mlContext.Data.LoadFromEnumerable(new List<Data>() {{ new Data() {{ Label = 1, Features = 1 }} }});
    ```
- step6: train a model
    ```csharp
    var pipeline = ...
    ```
- step7: print/save result...
    ```csharp
    var result = ...
    ```

## provided csharp code
```csharp
{code}
```");

        var engineer = new GPTAgent(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            "engineer",
            $@"You are engineer. Follow steps from architect and resolve task, once you resolve the task, reply [COMPLETE].
For each step, you can provide a csharp code, a markdown or a nuget installation script and send it to notebook_writer.
If there's any error from your code, notebook_writer will let you know. Fix the error and reimpement the step.
Implement one step at a time. Don't skip any step. Don't implement multiple steps at the same time.
here're some examples you can send to notebook_writer:
-- example 1 --
step1: add task name and description
```markdown
...
```
notebook_writer, save this step as cell #1. The cell type is markdown cell. The cell index is 1.

## Reference ##
The following code might be helpful for you to implement each step.
```csharp
{code}
```");

        using var notebookWriterFunction = new NotebookWriterFunction(outputNotebookPath, logger);
        var notebookWriter = new GPTAgent(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            "notebook_writer",
            $@"Follow engineer's instruction to create notebook. The full path of notebook should be {outputNotebookPath}.
if engineer provdes a csharp code, add it as csharp cell.
if engineer provdes a markdown, add it as markdown cell.
if engineer provdes a nuget installation script, add it as nuget installation cell, don't include package version in nuget package.
",
            new Dictionary<FunctionDefinition, Func<string, Task<string>>>()
            {
                { notebookWriterFunction.AddCSharpCodeCellFunction, notebookWriterFunction.AddCSharpCodeCellWrapper },
                { notebookWriterFunction.AddMarkdownCellFunction, notebookWriterFunction.AddMarkdownCellWrapper },
                //{ notebookWriterFunction.CreateNotebookFunction, notebookWriterFunction.CreateNotebookWrapper },
                { notebookWriterFunction.AddNugetInstallationCellFunction, notebookWriterFunction.AddNugetInstallationCellWrapper },
            });

        var admin = new GPTAgent(
           Constant.GPT,
           Constant.GPT_4_MODEL_ID,
           "admin",
           @"You are admin. 
You say [TERMINATE] after engineer says [COMPLETE], otherwise, ask engineer to continue his work");

        var initializeChatMessages = new (ChatMessage, string)[]
        {
            (
            new ChatMessage
            {
                Role = ChatRole.User,
                Content = "Welcome to the group chat! Work together to resolve task from labeller by crafting a dotnet interactive notebook for provided csharp code. I'll say [TERMINATE] if notebook get created and all cells are added. ",
            },
            admin.Name
            ),
            (
            new ChatMessage
            {
                Role = ChatRole.User,
                Content = "I'll create a task and save task to label.json",
            },
            labeller.Name
        ),
            (
            new ChatMessage
            {
                Role = ChatRole.User,
                Content = "I'll provide step-by-step plan to resolve task.",
            },
            architect.Name
        ),
            (
            new ChatMessage
            {
                Role = ChatRole.User,
                Content = "I'll write dotnet interactive notebook to implement each step after label.json is created",
            },
            engineer.Name
        ),
            (
            new ChatMessage
            {
                Role = ChatRole.User,
                Content = $"I'll save engineer's notebook right next to label.json. The name of notebook will be {Path.GetFileName(outputNotebookPath)}",
            },
            notebookWriter.Name
        ),
            (
            new ChatMessage
            {
                Role = ChatRole.User,
                Content = $"Great, let's start. labeller, please create task. architech, please create step-by-step plan and engineer, please resolve task based on that plan.",
            },
            admin.Name
        ),
        };

        var groupChat = new GroupChat(
            Constant.AzureOpenAI,
            Constant.GPT_35_MODEL_ID,
            admin,
            new[]
            {
                labeller,
                engineer,
                notebookWriter,
                architect,
            },
            initializeChatMessages);

        // pretty print messages
        foreach (var message in initializeChatMessages)
        {
            groupChat.PrettyPrintMessage(message.Item1, message.Item2);
        }

        var completeChatHistory = await groupChat.CallAsync(Enumerable.Empty<(ChatMessage, string)>(), maxRound);

        // log chat history
        var json = JsonSerializer.Serialize(initializeChatMessages.Concat(completeChatHistory!).Select(c =>
        {
            return new
            {
                content = c.Item1.Content,
                role = c.Item1.Role.ToString(),
                name = c.Item2,
            };
        }), jsonSerializerOptions);

        await File.WriteAllTextAsync(Path.Combine(workDir, $"{nameof(ConvertCSharpToNotebookTaskAsync)}.json"), json);
        logger?.Log($"Write chat history to {nameof(ConvertCSharpToNotebookTaskAsync)}.json");

        // check if task is resolved by examining if output file exist
        var isTaskResolved = File.Exists(outputNotebookPath) && File.Exists(outputLabelPath);

        // if isTaskResolved is true, then run generated notebook path to verify it can be executed successfully
        if (isTaskResolved)
        {
            logger?.Log($"Verify notebook {outputNotebookPath} can be executed successfully via dotnet repl");
            var cmd = $"dotnet repl --run {outputNotebookPath} --exit-after-run";
            var process = Process.Start(new ProcessStartInfo("dotnet", $"repl --run \"{outputNotebookPath}\" --exit-after-run")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDir,
            });

            // check if exit code is 0
            await process!.WaitForExitAsync();
            isTaskResolved = process.ExitCode == 0;

            if (!isTaskResolved)
            {
                logger?.Log($"Notebook {outputNotebookPath} can not be executed successfully via dotnet repl");
                // log error message
                var error = await process.StandardError.ReadToEndAsync();
                var output = await process.StandardOutput.ReadToEndAsync();
                logger?.Log(output);
                logger?.Log(error);
                return false;
            }
            else
            {
                logger?.Log($"Notebook {outputNotebookPath} can be executed successfully via dotnet repl");
            }

            var label = JsonSerializer.Deserialize<Label>(await File.ReadAllTextAsync(outputLabelPath));
            if (label is null)
            {
                logger?.Log($"label.json is invalid in {outputDir}. Skip it.");
                isTaskResolved = false;
            }
            else
            {
                label.IsSucceed = isTaskResolved;
                var labelJson = JsonSerializer.Serialize(label, jsonSerializerOptions);
                await File.WriteAllTextAsync(outputLabelPath, labelJson);
            }
        }
        return isTaskResolved;
    }
}
