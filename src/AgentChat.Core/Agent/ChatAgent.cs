using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    /// <summary>
    /// A simple chat agent that uses <see cref="IChatLLM"/> to generate reply message.
    /// </summary>
    public class ChatAgent : IAgent
    {
        public ChatAgent(
            IChatLLM chatLLM,
            string name)
        {
            ChatLLM = chatLLM;
            Name = name;
        }

        public string Name { get; }

        public IChatLLM ChatLLM { get; }

        public async Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default)
        {
            var reply = await ChatLLM.GetChatCompletionsAsync(conversation, ct: ct);

            if (reply.Message is IChatMessage msg)
            {
                msg.From = Name;

                return msg;
            }

            throw new Exception("ChatLLM did not return a valid message.");
        }
    }
}
