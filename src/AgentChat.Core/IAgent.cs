using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    public interface IAgent
    {
        public string Name { get; }

        public Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default);
    }
}
