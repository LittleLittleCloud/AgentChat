using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat;

public interface IAgent
{
    public string Name { get; }

    public Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default);
}