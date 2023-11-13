using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace AgentChat.OpenAI
{
    [JsonConverter(typeof(OpenAIThreadRunStatusConverter))]
    public class OpenAIAssistantRunStatus : IEquatable<OpenAIAssistantRunStatus>
    {
        private readonly string _status;

        [JsonConstructor]
        public OpenAIAssistantRunStatus(string status)
        {
            _status = status;
        }

        public string Status => _status;

        public static OpenAIAssistantRunStatus Queued { get; } = new OpenAIAssistantRunStatus("queued");
        public static OpenAIAssistantRunStatus InProgress { get; } = new OpenAIAssistantRunStatus("in_progress");
        public static OpenAIAssistantRunStatus RequiresAction { get; } = new OpenAIAssistantRunStatus("requires_action");
        public static OpenAIAssistantRunStatus Cancelling { get; } = new OpenAIAssistantRunStatus("cancelling");
        public static OpenAIAssistantRunStatus Cancelled { get; } = new OpenAIAssistantRunStatus("cancelled");
        public static OpenAIAssistantRunStatus Failed { get; } = new OpenAIAssistantRunStatus("failed");
        public static OpenAIAssistantRunStatus Completed { get; } = new OpenAIAssistantRunStatus("completed");
        public static OpenAIAssistantRunStatus Expired { get; } = new OpenAIAssistantRunStatus("expired");
    
        // override ==, !=, Equals, GetHashCode
        public override bool Equals(object? obj)
        {
            return obj is OpenAIAssistantRunStatus status &&
                   _status == status._status;
        }

        public bool Equals(OpenAIAssistantRunStatus other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return _status.GetHashCode();
        }

        public static bool operator ==(OpenAIAssistantRunStatus? left, OpenAIAssistantRunStatus? right)
        {
            return left?._status == right?._status;
        }

        public static bool operator !=(OpenAIAssistantRunStatus? left, OpenAIAssistantRunStatus? right)
        {
            return !(left == right);
        }

    }

    public class OpenAIThreadRunObject
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; } = "thread.run";

        [JsonPropertyName("created_at")]
        public long? CreatedAt { get; set; }

        [JsonPropertyName("assistant_id")]
        public string? AssistantId { get; set; }

        [JsonPropertyName("thread_id")]
        public string? ThreadId { get; set; }

        [JsonPropertyName("status")]
        public OpenAIAssistantRunStatus Status { get; set; } = OpenAIAssistantRunStatus.Queued;

        [JsonPropertyName("started_at")]
        public long? StartedAt { get; set; }

        [JsonPropertyName("expires_at")]
        public long? ExpiresAt { get; set; }

        [JsonPropertyName("cancelled_at")]
        public long? CancelledAt { get; set; }

        [JsonPropertyName("failed_at")]
        public long? FailedAt { get; set; }

        [JsonPropertyName("completed_at")]
        public long? CompletedAt { get; set; }

        [JsonPropertyName("last_error")]
        public string? LastError { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("instructions")]
        public string? Instructions { get; set; }

        [JsonPropertyName("file_ids")]
        public string[]? FileIds { get; set; }

        // to do
        [JsonPropertyName("tools")]
        public object[]? Tools { get; set; }

        [JsonPropertyName("required_action")]
        public OpenAISubmitToolOutputObject? RequiredAction { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
