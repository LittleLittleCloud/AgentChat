using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentChat.OpenAI
{
    public class OpenAIThreadRunStatusConverter : JsonConverter<OpenAIAssistantRunStatus>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            // can convert any type that inherits from OpenAIThreadMessageContentObject
            return typeof(OpenAIAssistantRunStatus).IsAssignableFrom(typeToConvert);
        }

        public override OpenAIAssistantRunStatus? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonDoc = JsonDocument.ParseValue(ref reader);
            var status = jsonDoc.RootElement.GetString() ?? throw new JsonException("Expected string value");
            return new OpenAIAssistantRunStatus(status);
        }

        public override void Write(Utf8JsonWriter writer, OpenAIAssistantRunStatus value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Status);
        }
    }
}