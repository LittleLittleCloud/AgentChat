using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentChat.OpenAI;

[JsonConverter(typeof(OpenAIThreadRunStatusConverter))]
public class OpenAIAssistantRunStatus : IEquatable<OpenAIAssistantRunStatus>
{
    public static OpenAIAssistantRunStatus Queued { get; } = new("queued");

    public static OpenAIAssistantRunStatus InProgress { get; } = new("in_progress");

    public static OpenAIAssistantRunStatus RequiresAction { get; } = new("requires_action");

    public static OpenAIAssistantRunStatus Cancelling { get; } = new("cancelling");

    public static OpenAIAssistantRunStatus Cancelled { get; } = new("cancelled");

    public static OpenAIAssistantRunStatus Failed { get; } = new("failed");

    public static OpenAIAssistantRunStatus Completed { get; } = new("completed");

    public static OpenAIAssistantRunStatus Expired { get; } = new("expired");

    public string Status { get; }

    [JsonConstructor]
    public OpenAIAssistantRunStatus(string status)
    {
        Status = status;
    }

    public bool Equals(OpenAIAssistantRunStatus? other) => this == other;

    // override ==, !=, Equals, GetHashCode
    public override bool Equals(object? obj) =>
        obj is OpenAIAssistantRunStatus status &&
        Status == status.Status;

    public override int GetHashCode() => Status.GetHashCode();

    public static bool operator ==(OpenAIAssistantRunStatus? left, OpenAIAssistantRunStatus? right) =>
        left?.Status == right?.Status;

    public static bool operator !=(OpenAIAssistantRunStatus? left, OpenAIAssistantRunStatus? right) => !(left == right);
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