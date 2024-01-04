using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentChat.Example.Share;

public class FixInvalidJsonFunctionWrapper
{
    private readonly IChatLLM chatLLM;

    private readonly Logger? logger;

    public FixInvalidJsonFunctionWrapper(IChatLLM chatLLM, Logger? logger = null)
    {
        this.chatLLM = chatLLM;
        this.logger = logger;
    }

    public Func<string, Task<string>> FixInvalidJsonWrapper(Func<string, Task<string>> func)
    {
        return async message =>
        {
            // if message is not a valid json, ask openai to fix it.
            var isValidJson = true;
            var errorMessage = string.Empty;

            try
            {
                JsonSerializer.Deserialize<Dictionary<string, object>>(message);
            }
            catch (JsonException ex)
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
                var systemMessage = new Message(Role.System, prompt);

                var res = await chatLLM.GetChatCompletionsAsync(new[] { systemMessage }, 0, 1024);
                var completion = res.Message ?? throw new Exception("fail to generate json response");

                message = completion.Content ?? throw new Exception("fail to generate json response");
            }

            var result = await func(message);
            return result;
        };
    }
}