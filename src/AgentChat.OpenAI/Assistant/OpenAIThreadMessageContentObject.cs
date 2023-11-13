using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AgentChat.OpenAI
{
    [JsonConverter(typeof(OpenAIThreadMessageContentObjectConverter))]
    public abstract class OpenAIThreadMessageContentObject
    {
        [JsonPropertyName("type")]
        public abstract string Type { get; set; }
    }

    public class OpenAIThreadImageMessageContentObject : OpenAIThreadMessageContentObject
    {
        [JsonPropertyName("type")]
        public override string Type { get; set; } = "image_file";

        [JsonPropertyName("image_file")]
        public ImageFileObject? ImageFile { get; set; }

        public class ImageFileObject
        {
            [JsonPropertyName("file_id")]
            public string? FileId { get; set; }
        }
    }

    public class OpenAIThreadTextMessageContentObject : OpenAIThreadMessageContentObject
    {
        [JsonPropertyName("type")]
        public override string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public TextObject? Text { get; set; }

        public class TextObject
        {
            [JsonPropertyName("value")]
            public string? Value { get; set; }
        }
    }
}
