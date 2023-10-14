using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GroupChatExample.Helper
{
    public partial class GroupChat
    {
        private IAgent admin;
        private List<IAgent> agents = new List<IAgent>();
        private OpenAIClient _client;
        private readonly string _model;
        private IEnumerable<(ChatMessage, string)> initializeMessages = new List<(ChatMessage, string)>();
        private IEnumerable<string> pleaseSpeakMessageCandidates = new[]
        {
            "It's your turn to speak.",
            "Please speak.",
            "proceed the conversation please",
        };
        public const string TERMINATE = "[GROUPCHAT_TERMINATE]";
        public const string CLEAR_MESSAGES = "// ignore this line [GROUPCHAT_CLEAR_MESSAGES]";

        /// <summary>
        /// terminate the group chat.
        /// </summary>
        /// <param name="message">terminate message.</param>
        [FunctionAttribution]
        public async Task<string> TerminateGroupChat(string message)
        {
            return $"{TERMINATE}: {message}";
        }

        /// <summary>
        /// Summarize the current conversation.
        /// </summary>
        /// <param name="context">conversation context.</param>
        [FunctionAttribution]
        public async Task<string> ClearGroupChat(string context)
        {
            return @$"{context}
<eof_msg>
{CLEAR_MESSAGES}
";
        }

        public GroupChat(OpenAIClient client,
            string model,
            IAgent admin,
            IEnumerable<IAgent> agents,
            IEnumerable<(ChatMessage, string)>? initializeMessages = null)
        {
            this._client = client;
            this._model = model;
            this.admin = admin;
            this.agents = agents.ToList();
            this.agents.Add(admin);
            this.initializeMessages = initializeMessages ?? new List<(ChatMessage, string)>();
        }

        public async Task<IAgent?> SelectNextSpeakerAsync(IEnumerable<(ChatMessage, string)> conversationWithName)
        {
            var agent_names = this.agents.Select(x => x.Name).ToList();
            var systemMessage = new ChatMessage(
                ChatRole.System,
                $@"You are in a role play game. Carefully read the conversation history and carry on the conversation.
The available roles are:
{string.Join(",", agent_names)}

Each message will start with 'From name:', e.g:
From admin:
//your message//."
                );

            var conv = this.ProcessConversationsForRolePlay(this.initializeMessages, conversationWithName);

            var messages = new[] { systemMessage }.Concat(conv);

            var option = new ChatCompletionsOptions
            {
                Temperature = 0,
            };

            option.StopSequences.Add(":");

            foreach (var message in messages)
            {
                option.Messages.Add(message);
            }

            var response = await this._client.GetChatCompletionsWithRetryAsync(this._model, option);

            var name = response.Value.Choices.First().Message.Content;
            var stopReason = response.Value.Choices.First().FinishReason;

            if (stopReason == CompletionsFinishReason.Stopped)
            {
                // remove From
                name = name.Substring(5);
                var agent = this.agents.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());

                return agent;
            }

            return null;
        }

        public void AddInitializeMessage(string message, string name)
        {
            var chatMessage = new ChatMessage
            {
                Content = message,
                Role = ChatRole.User,
            };
            this.initializeMessages = this.initializeMessages.Append((chatMessage, name));
        }

        public async Task<IEnumerable<(ChatMessage, string)>> CallAsync(IEnumerable<(ChatMessage, string)>? conversationWithName = null, int maxRound = 10, bool throwExceptionWhenMaxRoundReached = true)
        {
            if (maxRound == 0)
            {
                if (throwExceptionWhenMaxRoundReached)
                {
                    throw new Exception("Max round reached.");
                }
                else
                {
                    return conversationWithName ?? Enumerable.Empty<(ChatMessage, string)>();
                }
            }

            // sleep 10 seconds
            await Task.Delay(1000);

            if (conversationWithName == null)
            {
                conversationWithName = Enumerable.Empty<(ChatMessage, string)>();
            }


            var agent = await this.SelectNextSpeakerAsync(conversationWithName) ?? this.admin;
            ChatMessage? result = null;
            var processedConversation = this.ProcessConversationForAgent(agent.Name, this.initializeMessages, conversationWithName);
            result = await agent.CallAsync(processedConversation) ?? throw new Exception("No result is returned.");
            this.PrettyPrintMessage(result, agent.Name);
            var updatedConversation = conversationWithName.Append((result, agent.Name));

            // if message is terminate message, then terminate the conversation
            if (result?.IsGroupChatTerminateMessage() ?? false)
            {
                return updatedConversation;
            }

            return await this.CallAsync(updatedConversation, maxRound - 1, throwExceptionWhenMaxRoundReached);
        }

        public void PrettyPrintMessage(ChatMessage message, string name)
        {
            var result = this.FormatMessage(message, name);
            Console.WriteLine(result);
            
        }

        public string FormatMessage(ChatMessage message, string name)
        {
            // write result
            var result = $"Message from {name}\n";
            // write a seperator
            result += new string('-', 20) + "\n";
            result += message.Content + "\n";
            result += new string('-', 20) + "\n";

            return result;
        }
    }
}
