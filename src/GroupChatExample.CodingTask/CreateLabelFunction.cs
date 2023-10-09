using GroupChatExample.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GroupChatExample.CodingTask
{
    public partial class CreateLabel
    {
        private readonly Logger? _logger;
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public CreateLabel(Logger? logger = null)
        {
            this._logger = logger;
        }

        /// <summary>
        /// save task to label.json
        /// </summary>
        /// <param name="taskName">the name of task.</param>
        /// <param name="taskDescription">the description of task. Be brief.</param>
        /// <param name="taskOutputFileName">output result file from task.</param>
        /// <param name="outputFolder">the output folder to save label.</param>
        [FunctionAttribution]
        public async Task<string> SaveTask(string taskName, string taskDescription, string taskOutputFileName, string outputFolder)
        {
            var label = new Label
            {
                IsSucceed = false,
                TaskName = taskName,
                TaskDescription = taskDescription,
                Output = taskOutputFileName,
            };

            var labelFilePath = Path.Combine(outputFolder, $"label.json");
            var labelJson = JsonSerializer.Serialize(label, jsonSerializerOptions);

            // save label to file, overwrite if file exist
            await File.WriteAllTextAsync(labelFilePath, labelJson);

            return @$"create task successfully, the task name is {taskName}, the task description is {taskDescription}";
        }
    }
}
