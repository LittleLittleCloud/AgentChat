using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentChat.Core.Tests
{
    internal class EchoAgent : IAgent
    {
        public EchoAgent(string name)
        {
            Name = name;
        }
        public string Name { get; }

        public Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default)
        {
            // return the most recent message
            var lastMessage = conversation.LastOrDefault();
            var reply = new Message(ChatRole.Assistant, lastMessage?.Content, Name);

            return Task.FromResult<IChatMessage>(reply);
        }
    }
}
