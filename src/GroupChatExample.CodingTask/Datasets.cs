using System.Text.Json;
using System.Text.Json.Serialization;

namespace GroupChatExample.CodingTask
{
    public class DataPoint
    {
        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        /// <summary>
        /// The output file name is used to automatically determine if task is completed successfully.
        /// </summary>
        public string OutputFileName { get; set; } = "";
    }

    public class Label
    {
        [JsonPropertyName("task_name")]
        public string TaskName { get; set; } = "";

        [JsonPropertyName("task_description")]
        public string TaskDescription { get; set; } = "";

        /// <summary>
        /// The output file name is used to automatically determine if task is completed successfully.
        /// </summary>
        [JsonPropertyName("output")]
        public string Output { get; set; } = "";

        [JsonPropertyName("is_succeed")]
        public bool IsSucceed { get; set; } = false;
    }

    public static class Dataset
    {
        public static IEnumerable<DataPoint> LoadDataset()
        {
            var datasetPath = "datasets.json";

            if (!File.Exists(datasetPath))
            {
                throw new FileNotFoundException("Dataset file not found.", datasetPath);
            }

            var json = File.ReadAllText(datasetPath);
            var dataset = JsonSerializer.Deserialize<DataPoint[]>(json);

            return dataset ?? new DataPoint[0];
        }
    }
}


