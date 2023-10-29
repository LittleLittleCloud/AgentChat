using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    public class GroupChat : IGroupChat
    {
        private IChatLLM chatLLM;
        private IAgent admin;
        private List<IAgent> agents = new List<IAgent>();
        private IEnumerable<IChatMessage> initializeMessages = new List<IChatMessage>();

        public GroupChat(
            IChatLLM chatLLM,
            IAgent admin,
            IEnumerable<IAgent> agents,
            IEnumerable<IChatMessage>? initializeMessages = null)
        {
            this.chatLLM = chatLLM;
            this.admin = admin;
            this.agents = agents.ToList();
            this.agents.Add(admin);
            this.initializeMessages = initializeMessages ?? new List<IChatMessage>();
        }

        public async Task<IAgent?> SelectNextSpeakerAsync(IEnumerable<IChatMessage> conversationHistory)
        {
            var agent_names = this.agents.Select(x => x.Name).ToList();
            var systemMessage = new Message(ChatRole.System,
                content: $@"You are in a role play game. Carefully read the conversation history and carry on the conversation.
The available roles are:
{string.Join(",", agent_names)}

Each message will start with 'From name:', e.g:
From admin:
//your message//.");

            var conv = this.ProcessConversationsForRolePlay(this.initializeMessages, conversationHistory);

            var messages = new IChatMessage[] { systemMessage }.Concat(conv);
            var response = await this.chatLLM.GetChatCompletionsWithRetryAsync(messages, temperature: 0, stopWords: new[] { ":" });

            var name = response.Message?.Content;

            try
            {
                // remove From
                name = name!.Substring(5);
                var agent = this.agents.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());

                return agent;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void AddInitializeMessage(IChatMessage message)
        {
            this.initializeMessages = this.initializeMessages.Append(message);
        }

        public async Task<IEnumerable<IChatMessage>> CallAsync(
            IEnumerable<IChatMessage>? conversationWithName = null,
            int maxRound = 10,
            bool throwExceptionWhenMaxRoundReached = false,
            CancellationToken? ct = null)
        {
            if (maxRound == 0)
            {
                if (throwExceptionWhenMaxRoundReached)
                {
                    throw new Exception("Max round reached.");
                }
                else
                {
                    return conversationWithName ?? Enumerable.Empty<IChatMessage>();
                }
            }

            // sleep 10 seconds
            await Task.Delay(1000);

            if (conversationWithName == null)
            {
                conversationWithName = Enumerable.Empty<IChatMessage>();
            }

            var agent = await this.SelectNextSpeakerAsync(conversationWithName) ?? this.admin;
            IChatMessage? result = null;
            var processedConversation = this.ProcessConversationForAgent(this.initializeMessages, conversationWithName);
            result = await agent.CallAsync(processedConversation) ?? throw new Exception("No result is returned.");
            result.PrettyPrintMessage();
            var updatedConversation = conversationWithName.Append(result);

            // if message is terminate message, then terminate the conversation
            if (result?.IsGroupChatTerminateMessage() ?? false)
            {
                return updatedConversation;
            }

            return await this.CallAsync(updatedConversation, maxRound - 1, throwExceptionWhenMaxRoundReached);
        }
    }
}
