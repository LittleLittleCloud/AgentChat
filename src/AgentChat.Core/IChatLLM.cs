using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    public interface IChatLLM
    {
        IChatMessage CreateChatMessage(
            ChatRole role,
            string? content = null,
            string? name = null,
            string? from = null);

        // TODO
        // support streaming chat
        Task<IChatCompletion> GetChatCompletionsAsync(
            IEnumerable<IChatMessage> messages,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null,
            CancellationToken? ct = default);
    }
}
