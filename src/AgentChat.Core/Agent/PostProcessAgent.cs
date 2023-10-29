using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    public class PostProcessAgent : IAgent
    {
        public PostProcessAgent(
            IAgent innerAgent,
            string name,
            Func<IEnumerable<IChatMessage>, IChatMessage, CancellationToken?, Task<IChatMessage>> postprocessFunc)
        {
            InnerAgent = innerAgent;
            Name = name;
            PostprocessFunc = postprocessFunc;
        }

        public IAgent InnerAgent { get; }

        public string Name { get; }

        public Func<IEnumerable<IChatMessage>, IChatMessage, CancellationToken?, Task<IChatMessage>> PostprocessFunc { get; }

        public async Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default)
        {
            var reply = await InnerAgent.CallAsync(conversation, ct);
            return await PostprocessFunc(conversation, reply, ct);
        }
    }
}
