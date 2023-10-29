using System.Collections.Generic;
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
        Task<ChatCompletion> GetChatCompletionsAsync(
            IEnumerable<IChatMessage> messages,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null,
            CancellationToken? ct = default);

        public class ChatCompletion
        {
            public IChatMessage? Message { get; set; }

            public int? PromptTokens { get; set; }

            public int? TotalTokens { get; set; }

            public int? CompletionTokens { get; set; }
        }
    }
}
