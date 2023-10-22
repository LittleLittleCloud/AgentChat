using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    public static class ChatLLMExtension
    {
        public static async Task<IChatCompletion> GetChatCompletionsWithRetryAsync(
            this IChatLLM chatLLM,
            IEnumerable<IChatMessage> messages,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null,
            CancellationToken ct = default, int maxRetry = 5)
        {
            try
            {
                return await chatLLM.GetChatCompletionsAsync(messages, temperature, maxToken, stopWords, ct);
            }
            catch (Exception)
            {
                await Task.Delay(10000);
                return await chatLLM.GetChatCompletionsWithRetryAsync(messages, temperature, maxToken, stopWords, ct, maxRetry - 1);
            }
        }
    }
}
