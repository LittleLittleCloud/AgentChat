using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.DotNet.Interactive;

namespace AgentChat.Example.Share
{
    public class NotebookUserAgent: IAgent
    {
        public NotebookUserAgent(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public async Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> messages, CancellationToken ct)
        {
            var input = await Kernel.GetInputAsync();

            return new Message(ChatRole.Assistant, input, null, this.Name);
        }
    }
}
