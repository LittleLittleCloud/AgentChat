using Azure.AI.OpenAI;
using System.Text.Json.Serialization;

namespace AgentChat.OpenAI
{
    public class OpenAISubmitToolOutputObject
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "submit_tool_outputs";

        [JsonPropertyName("submit_tool_outputs")]
        public SubmitToolOutputsObject? SubmitToolOutputs { get; set; }

        public class SubmitToolOutputsObject
        {
            [JsonPropertyName("tool_calls")]
            public ToolCallObject[]? ToolCalls { get; set; }

            public class ToolCallObject
            {
                [JsonPropertyName("id")]
                public string? Id { get; set; }

                [JsonPropertyName("type")]
                public string Type { get; set; } = "function";

                [JsonPropertyName("function")]
                public FunctionCallObject? Function { get; set; }
            }

            public class FunctionCallObject
            {
                [JsonPropertyName("name")]
                public string? Name { get; set; }

                [JsonPropertyName("arguments")]
                public string? Arguments { get; set; }
            }
        }
    }
}