using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    public static class AgentExtension
    {
        public static async Task<IEnumerable<IChatMessage>> SendMessageAsync(
            this IAgent agent,
            IChatMessage msg,
            GroupChat groupChat,
            int maxRound = 10,
            bool throwWhenMaxRoundReached = true,
            CancellationToken ct = default)
        {
            if (msg.From != agent.Name)
            {
                throw new ArgumentException("The message is not from the agent", nameof(msg));
            }

            return await groupChat.CallAsync(new[] { msg }, maxRound, throwWhenMaxRoundReached);
        }

        public static async Task<IChatMessage> SendMessageAsync(
            this IAgent agent,
            IChatMessage? msg = null,
            CancellationToken ct = default)
        {
            if (msg == null)
            {
                return await agent.CallAsync(Enumerable.Empty<IChatMessage>(), ct);
            }

            return await agent.CallAsync(new[] { msg }, ct);
        }

        public static async Task<IEnumerable<IChatMessage>> SendMessageAsync(
            this IAgent agent,
            IAgent receiver,
            IEnumerable<IChatMessage>? chatHistory = null,
            int maxRound = 10,
            bool throwWhenMaxRoundReached = false,
            CancellationToken ct = default)
        {
            var groupChat = new SequentialGroupChat(new[] { agent, receiver });

            return await groupChat.CallAsync(chatHistory, maxRound, throwWhenMaxRoundReached);
        }
    }
}
