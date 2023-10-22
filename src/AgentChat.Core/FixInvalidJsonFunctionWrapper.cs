using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentChat.Core
{
    public class FixInvalidJsonFunctionWrapper
    {
        private readonly OpenAIClient _client;
        private readonly string _model;
        private readonly Logger? logger;

        public FixInvalidJsonFunctionWrapper(OpenAIClient client, string model, Logger? logger = null)
        {
            this._client = client;
            this._model = model;
            this.logger = logger;
        }

        public Func<string, Task<string>> FixInvalidJsonWrapper(Func<string, Task<string>> func)
        {
            return async (string message) =>
            {
                // if message is not a valid json, ask openai to fix it.
                var isValidJson = true;
                var errorMessage = string.Empty;
                try
                {
                    JsonSerializer.Deserialize<Dictionary<string, object>>(message);
                }
                catch(JsonException ex)
                {
                    isValidJson = false;
                    errorMessage = ex.Message;
                }

                if (!isValidJson)
                {
                    var prompt = @$"Fix the invalid json. Your response should only contain the fixed Json object
```json
{message}
```

Response example:
{{
// fixed json
}}";
                    var systemMessage = new ChatMessage
                    {
                        Role = ChatRole.System,
                        Content = prompt,
                    };

                    var option = new ChatCompletionsOptions
                    {
                        Temperature = 0,
                        MaxTokens = 1024,
                    };

                    option.Messages.Add(systemMessage);

                    var res = await _client.GetChatCompletionsAsync(_model, option);
                    var completion = res.Value.Choices.First().Message;

                    // print fixed json
                    logger?.Log(completion.Content);
                    message = completion.Content;
                }

                var result = await func(message);
                return result;
            };
        }
    }
}
