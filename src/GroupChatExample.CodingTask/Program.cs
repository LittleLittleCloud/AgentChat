// See https://aka.ms/new-console-template for more information
using GroupChatExample.CodingTask;
using GroupChatExample.Helper;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.Json;

_create_notebook_command.Handler = CommandHandler.Create<Option>(async (option) =>
{
    var taskFolder = option.TaskFolder.FullName;
    var outputFolder = Path.Combine(taskFolder, "output");
    var globalLogger = new Logger(outputFolder);
    globalLogger.Log("Start coding task.");
    globalLogger.Log($"Task folder: {taskFolder}");
    globalLogger.Log($"Output folder: {outputFolder}");
    globalLogger.Log($"skip if succeed: {option.SkipIfSucceed}");

    var jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
    };

    // go through each task under task folder and load the label.json
    // the folder structure should be:
    // .
    // ├── task1
    // │   ├── label.json
    // │   ├── other files
    // ├── task2
    // │   ├── label.json
    // │   ├── other files
    // ├── task3
    // │   ├── label.json...

    // recursive to false to avoid going into sub folder
    var folders = Directory.GetDirectories(taskFolder, "*", SearchOption.TopDirectoryOnly);
    foreach (var folder in folders)
    {
        var labelJsonPath = Path.Combine(folder, "label.json");
        if (!File.Exists(labelJsonPath))
        {
            globalLogger.Log($"label.json not found in {folder}. Skip it.");
            continue;
        }

        var labelJson = await File.ReadAllTextAsync(labelJsonPath);
        var label = JsonSerializer.Deserialize<Label>(labelJson);
        if (label is null)
        {
            globalLogger.Log($"label.json is invalid in {folder}. Skip it.");
            continue;
        }

        if (label.IsSucceed && option.SkipIfSucceed)
        {
            globalLogger.Log($"Task {label.TaskName} already succeed. Skip it.");
            continue;
        }

        // run task
        globalLogger.Log($"Start run task {label.TaskName}.");
        globalLogger.Log($"Task description: {label.TaskDescription}");
        globalLogger.Log($"Task output file name: {label.Output}");

        var task = new DataPoint
        {
            Name = label.TaskName,
            Description = label.TaskDescription,
            OutputFileName = label.Output,
        };

        try
        {
            var isSucceed = await RunCodingTask(task, folder);

            if (!isSucceed)
            {
                globalLogger.Log($"Task {task.Name} failed. Skip it.");
                continue;
            }

            isSucceed = await WriteNotebookTask(task, folder);
            label.IsSucceed = isSucceed;
            labelJson = JsonSerializer.Serialize(label, jsonSerializerOptions);
            await File.WriteAllTextAsync(labelJsonPath, labelJson);

            globalLogger.Log($"Task {task.Name} completed.");
            globalLogger.Log($"result: {labelJson}");
        }
        catch (Exception ex)
        {
            globalLogger.Log($"Task {task.Name} failed. Error: {ex.Message}");
            globalLogger.Log(ex.Message);
            continue;
        }
    }
});

_create_notebook_and_label_command.Handler = CommandHandler.Create<Option>(async (option) =>
{
    var mlnetSampleFolder = option.MLNetSampleFolder?.FullName ?? throw new ArgumentException("mlnet sample folder is required.");
    var taskFolder = option.TaskFolder?.FullName ?? throw new ArgumentException("task folder is required.");
    var outputFolder = taskFolder;
    var globalLogger = new Logger(outputFolder);
    globalLogger.Log("Start coding task.");
    globalLogger.Log($"MLNet sample folder: {mlnetSampleFolder}");
    globalLogger.Log($"MLNet 101 Task folder: {taskFolder}");
    globalLogger.Log($"skip if succeed: {option.SkipIfSucceed}");

    var jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
    };

    // step 1: get all csharp files under mlnet sample folder
    var csharpFiles = Directory.GetFiles(mlnetSampleFolder, "*.cs", SearchOption.AllDirectories);

    foreach (var csharpFile in csharpFiles)
    {
        var workDir = Path.Join(outputFolder, Path.GetFileNameWithoutExtension(csharpFile));
        if (!Directory.Exists(workDir))
        {
            Directory.CreateDirectory(workDir);
        }

        // skip if already succeed and is_success is true
        var labelJsonPath = Path.Combine(workDir, "label.json");
        if (File.Exists(labelJsonPath))
        {
            var labelJson = await File.ReadAllTextAsync(labelJsonPath);
            var label = JsonSerializer.Deserialize<Label>(labelJson);
            if (label is null)
            {
                globalLogger.Log($"label.json is invalid in {workDir}. Skip it.");
                continue;
            }

            if (label.IsSucceed && option.SkipIfSucceed)
            {
                globalLogger.Log($"Task {label.TaskName} already succeed. Skip it.");
                continue;
            }
        }

        try
        {
            var isSucceed = await ConvertCSharpToNotebookTaskAsync(csharpFile, workDir, option.MaxRoundForNotebookWritingGroup, logger: globalLogger);
            if (!isSucceed)
            {
                globalLogger.Log($"Task {csharpFile} failed. Skip it.");
                continue;
            }
            else
            {
                globalLogger.Log($"Task {csharpFile} completed.");
            }
        }
        catch (Exception ex)
        {
            globalLogger.Log($"Task {csharpFile} failed. Error: {ex.Message}");
            globalLogger.Log(ex.Message);
            continue;
        }
    }
});


_root_command.Invoke(args);
