using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    /// <summary>
    /// Interface for chat model. This interface provides a unified way to interact with different llms.
    /// </summary>
    public interface IChatLLM
    {
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
