using System.CommandLine;

internal static partial class Program
{
    private class Option
    {
        public DirectoryInfo? TaskFolder { get; set; }

        public bool SkipIfSucceed { get; set; }

        public string OutputLog { get; set; }

        public int MaxRoundForCodingGroup { get; set; } = 20;

        public int MaxRoundForNotebookWritingGroup { get; set; } = 50;

        public DirectoryInfo? MLNetSampleFolder { get; set; }
    }

    private static readonly Option<DirectoryInfo> _task_folder = new Option<DirectoryInfo>("--task-folder", "The folder containing the mlnet 101 tasks.");

    private static readonly Option<bool> _skip_if_succeed = new Option<bool>("--skip-if-succeed", () => true, "Skip the task if it has already been completed. Default is true");

    private static readonly Option<string> _output_log = new Option<string>("--output-log", () => "output.log", "The output log file. Default is output.log");

    private static readonly Option<int> _max_round_for_coding_group = new Option<int>("--max-round-for-coding-group", () => 30, "The max round for coding group. Default is 30");

    private static readonly Option<int> _max_round_for_notebook_writing_group = new Option<int>("--max-round-for-notebook-writing-group", () => 50, "The max round for notebook writing group. Default is 20");
    
    private static readonly Option<DirectoryInfo> _mlnet_sample_folder = new Option<DirectoryInfo>("--mlnet-sample-folder", "The folder containing the mlnet sample project.");
    private static readonly Command _create_notebook_command = new Command("create-notebook", "create notebook from mlnet 101 tasks")
        {
            _task_folder,
            _skip_if_succeed,
            _output_log,
            _max_round_for_coding_group,
            _max_round_for_notebook_writing_group,
        };

    private static readonly Command _create_notebook_and_label_command = new Command("create-notebook-and-label", "create notebook and label from mlnet example project")
    {
            _mlnet_sample_folder,
            _task_folder,
            _skip_if_succeed,
            _output_log,
            _max_round_for_notebook_writing_group,
        };

    private static readonly RootCommand _root_command = new RootCommand("Copilot builder coding task")
    {
            _create_notebook_command,
            _create_notebook_and_label_command,
        };

    //private static Option ParseOption(string[] args)
    //{
    //    var option = new Option();
    //    var parse_result = _create_notebook_command.Parse(args);
    //    option.TaskFolder = parse_result.GetValueForOption(_task_folder) ?? throw new ArgumentException();
    //    option.SkipIfSucceed = parse_result.GetValueForOption(_skip_if_succeed);
    //    option.OutputLog = parse_result.GetValueForOption(_output_log) ?? "output.txt";
    //    option.MaxRoundForCodingGroup = parse_result.GetValueForOption(_max_round_for_coding_group);
    //    option.MaxRoundForNotebookWritingGroup = parse_result.GetValueForOption(_max_round_for_notebook_writing_group);

    //    return option;
    //}
}
