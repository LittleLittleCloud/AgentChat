using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat;

public class SequentialGroupChat : IGroupChat
{
    private readonly List<IAgent> agents = new();

    private readonly List<IChatMessage> initializeMessages = new();

    public SequentialGroupChat(
        IEnumerable<IAgent> agents,
        List<IChatMessage>? initializeMessages = null)
    {
        this.agents.AddRange(agents);
        this.initializeMessages = initializeMessages ?? new List<IChatMessage>();
    }

    public void AddInitializeMessage(IChatMessage message)
    {
        initializeMessages.Add(message);
    }

    public async Task<IEnumerable<IChatMessage>> CallAsync(
        IEnumerable<IChatMessage>? conversationWithName = null,
        int maxRound = 10,
        bool throwExceptionWhenMaxRoundReached = false,
        CancellationToken? ct = null)
    {
        var conversationHistory = new List<IChatMessage>();

        if (conversationWithName != null)
        {
            conversationHistory.AddRange(conversationWithName);
        }

        var lastSpeaker = conversationHistory.LastOrDefault()?.From switch
        {
            null => agents.First(),
            _ => agents.FirstOrDefault(x => x.Name == conversationHistory.Last().From) ??
                 throw new Exception("The agent is not in the group chat")
        };
        var round = 0;

        while (round < maxRound)
        {
            var currentSpeaker = SelectNextSpeaker(lastSpeaker);
            var processedConversation = this.ProcessConversationForAgent(initializeMessages, conversationHistory);
            var result = await currentSpeaker.CallAsync(processedConversation) ?? throw new Exception("No result is returned.");
            result.PrettyPrintMessage();
            conversationHistory.Add(result);

            // if message is terminate message, then terminate the conversation
            if (result?.IsGroupChatTerminateMessage() ?? false)
            {
                break;
            }

            lastSpeaker = currentSpeaker;
            round++;
        }

        if (round == maxRound && throwExceptionWhenMaxRoundReached)
        {
            throw new Exception("Max round reached");
        }

        return conversationHistory;
    }

    private IAgent SelectNextSpeaker(IAgent currentSpeaker)
    {
        var index = agents.IndexOf(currentSpeaker);

        if (index == -1)
        {
            throw new ArgumentException("The agent is not in the group chat", nameof(currentSpeaker));
        }

        var nextIndex = (index + 1) % agents.Count;
        return agents[nextIndex];
    }
}