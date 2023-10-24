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
        /// <summary>
        /// Create <see cref="IChatMessage"/> that accomodates the underlying chat model.
        /// </summary>
        /// <param name="role">mapping to <see cref="ChatMessage.Role"/></param>
        /// <param name="content">mapping to <see cref="ChatMessage.Content"/>.</param>
        /// <param name="name">mapping to <see cref="ChatMessage.Name"/>.
        /// Note that <see cref="IGroupChat"/> doesn't use this field to identify the sender.
        /// The sender is indicated via <see cref="IChatMessage.From"/> field.</param>
        /// <param name="from">if set, indicates which agent/entity creates this message.</param>
        /// <returns></returns>
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
