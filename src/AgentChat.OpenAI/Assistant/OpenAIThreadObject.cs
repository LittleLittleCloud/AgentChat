using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentChat.OpenAI;

public class OpenAIThreadObject
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("object")]
    public string Object { get; set; } = "thread";

    [JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}