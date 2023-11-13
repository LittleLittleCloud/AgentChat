using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AgentChat.OpenAI
{
    public class OpenAIThreadMessageObject
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; } = "thread.message";

        [JsonPropertyName("created_at")]
        public long? CreatedAt { get; set; }

        [JsonPropertyName("thread_id")]
        public string? ThreadId { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public OpenAIThreadMessageContentObject[]? Content { get; set; }

        [JsonPropertyName("file_ids")]
        public string[]? FileIds { get; set; }

        [JsonPropertyName("assistant_id")]
        public string? AssistantId { get; set; }

        [JsonPropertyName("run_id")]
        public string? RunId { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
