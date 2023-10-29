using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    public interface IGroupChat
    {
        void AddInitializeMessage(IChatMessage message);

        Task<IEnumerable<IChatMessage>> CallAsync(IEnumerable<IChatMessage>? conversationWithName = null, int maxRound = 10, bool throwExceptionWhenMaxRoundReached = true, CancellationToken? ct = null);
    }
}