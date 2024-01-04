using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentChat.OpenAI;

public class OpenAIThreadMessageContentObjectConverter : JsonConverter<OpenAIThreadMessageContentObject>
{
    public override bool CanConvert(Type typeToConvert) =>

        // can convert any type that inherits from OpenAIThreadMessageContentObject
        typeof(OpenAIThreadMessageContentObject).IsAssignableFrom(typeToConvert);

    public override OpenAIThreadMessageContentObject? Read(ref Utf8JsonReader reader, Type typeToConvert,
                                                           JsonSerializerOptions options)
    {
        var jsonDoc = JsonDocument.ParseValue(ref reader);
        var type = jsonDoc.RootElement.GetProperty("type").GetString();

        if (type == "text")
        {
            return JsonSerializer.Deserialize<OpenAIThreadTextMessageContentObject>(jsonDoc.RootElement.GetRawText(), options);
        }

        if (type == "image_file")
        {
            return JsonSerializer.Deserialize<OpenAIThreadImageMessageContentObject>(jsonDoc.RootElement.GetRawText(), options);
        }

        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, OpenAIThreadMessageContentObject value, JsonSerializerOptions options)
    {
        if (value is OpenAIThreadTextMessageContentObject text)
        {
            JsonSerializer.Serialize(writer, text, options);
        }
        else if (value is OpenAIThreadImageMessageContentObject image)
        {
            JsonSerializer.Serialize(writer, image, options);
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}