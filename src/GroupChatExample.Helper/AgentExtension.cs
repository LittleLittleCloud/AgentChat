using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GroupChatExample.Helper
{
    public static class AgentExtension
    {
        public static async Task<IEnumerable<(ChatMessage, string)>> SendMessageAsync(
            this IAgent agent,
            string message,
            GroupChat groupChat,
            int maxRound = 10,
            bool throwWhenMaxRoundReached = true,
            CancellationToken ct = default)
        {
            var msg = new ChatMessage(ChatRole.User, message);
            return await groupChat.CallAsync(new[] {(msg, agent.Name)}, maxRound, throwWhenMaxRoundReached);
        }
    }
}
