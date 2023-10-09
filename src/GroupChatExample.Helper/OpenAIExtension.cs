using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GroupChatExample.Helper
{
    internal static class OpenAIExtension
    {
        public static async Task<Azure.Response<ChatCompletions>> GetChatCompletionsWithRetryAsync(this OpenAIClient client, string modelName, ChatCompletionsOptions option, CancellationToken ct = default, int maxRetry = 5)
        {
            try
            {
                return await client.GetChatCompletionsAsync(modelName, option, ct);
            }
            catch (Azure.RequestFailedException ex)
            {
                if (ex.Status == 429 && maxRetry > 0)
                {
                    await Task.Delay(10000);
                    return await client.GetChatCompletionsWithRetryAsync(modelName, option, ct, maxRetry - 1);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
