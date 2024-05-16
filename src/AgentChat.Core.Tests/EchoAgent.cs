namespace AgentChat.Core.Tests;

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
        var reply = new Message(Role.Assistant, lastMessage?.Content, Name);

        return Task.FromResult<IChatMessage>(reply);
    }
}