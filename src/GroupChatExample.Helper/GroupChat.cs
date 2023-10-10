using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GroupChatExample.Helper
{
    public partial class GroupChat
    {
        private ChatAgent admin;
        private List<ChatAgent> agents = new List<ChatAgent>();
        private OpenAIClient _client;
        private readonly string _model;
        private IEnumerable<(ChatMessage, string)> initializeMessages = new List<(ChatMessage, string)>();
        private IEnumerable<string> pleaseSpeakMessageCandidates = new[]
        {
            "It's your turn to speak.",
            "Please speak.",
            "proceed the conversation please",
        };
        private const string TERMINATE = "[GROUPCHAT_TERMINATE]";

        /// <summary>
        /// terminate the group chat.
        /// </summary>
        /// <param name="message">terminate message.</param>
        [FunctionAttribution]
        public async Task<string> TerminateGroupChat(string message)
        {
            return $"{TERMINATE}: {message}";
        }

        public GroupChat(OpenAIClient client,
            string model,
            ChatAgent admin,
            IEnumerable<ChatAgent> agents,
            IEnumerable<(ChatMessage, string)>? initializeMessages = null)
        {
            this._client = client;
            this._model = model;
            this.admin = admin;
            this.agents = agents.ToList();
            this.agents.Add(admin);
            this.initializeMessages = initializeMessages ?? new List<(ChatMessage, string)>();
        }

        public async Task<ChatAgent?> SelectNextSpeakerAsync(IEnumerable<(ChatMessage, string)> conversationWithName)
        {
            var systemMessage = new ChatMessage(
                ChatRole.System,
                @"You are in a role play game. Carefully read the conversation history and carry on the conversation.
Each message will start with 'From name:', e.g:
From admin:
//your message//."
                );

            var conv = conversationWithName.Select(x =>
            {
                var msg = @$"From {x.Item2}:
{x.Item1.Content}";
                return new ChatMessage(ChatRole.User, msg);
            });

            var initializeConv = this.initializeMessages.Select(x =>
            {
                var msg = @$"From {x.Item2}:
{x.Item1.Content}";
                return new ChatMessage(ChatRole.User, msg);
            });

            var messages = new[] { systemMessage }.Concat(initializeConv).Concat(conv);

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

        public void AddMessage(string message, string name)
        {
            var chatMessage = new ChatMessage
            {
                Content = message,
                Role = ChatRole.User,
            };
            this.initializeMessages = this.initializeMessages.Append((chatMessage, name));
        }

        public async Task<IEnumerable<(ChatMessage, string)>?> CallAsync(IEnumerable<(ChatMessage, string)>? conversationWithName = null, int maxRound = 10, bool throwExceptionWhenMaxRoundReached = true)
        {
            if (maxRound == 0)
            {
                if (throwExceptionWhenMaxRoundReached)
                {
                    throw new Exception("Max round reached.");
                }
                else
                {
                    return conversationWithName;
                }
            }

            // sleep 10 seconds
            await Task.Delay(10000);

            if (conversationWithName == null)
            {
                conversationWithName = Enumerable.Empty<(ChatMessage, string)>();
            }

            var agent = await this.SelectNextSpeakerAsync(conversationWithName) ?? this.admin;
            ChatMessage? result = null;
            while (true)
            {
                var processedConversation = await this.ProcessConversations(agent, this.initializeMessages.Concat(conversationWithName));
                result = await agent.CallAsync(processedConversation) ?? throw new Exception("No result is returned.");

                // check if result is end with <eof_name>:
                if (result.Content.EndsWith("<eof_name>:"))
                {
                    // content is From name<eof_name>:
                    // retrieve name
                    // sleep 10 seconds
                    await Task.Delay(1000);
                    var name = result.Content.Substring(5, result.Content.Length - 16);
                    if (agent.Name == name)
                    {
                        agent = this.admin;
                    }
                    else
                    {
                        // check if name is valid
                        if (!this.agents.Any(x => x.Name.ToLower() == name.ToLower()))
                        {
                            throw new Exception("Invalid name.");
                        }

                        // ask agent to speak
                        // step 1: randomly pick a message from candidates
                        var random = new Random();
                        var index = random.Next(0, this.pleaseSpeakMessageCandidates.Count());
                        var message = this.pleaseSpeakMessageCandidates.ElementAt(index);
                        result = new ChatMessage(ChatRole.Assistant, $"{name}, {message}");
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            

            this.PrettyPrintMessage(result, agent.Name);
            var updatedConversation = conversationWithName.Append((result, agent.Name));

            // if message is terminate message, then terminate the conversation
            if (this.IsTerminateMessage(result))
            {
                return updatedConversation;
            }

            return await this.CallAsync(updatedConversation, maxRound - 1, throwExceptionWhenMaxRoundReached);
        }

        private bool IsTerminateMessage(ChatMessage message)
        {
            return message.Content.StartsWith(TERMINATE);
        }

        private async Task<IEnumerable<ChatMessage>> ProcessConversations(ChatAgent nextSpeaker, IEnumerable<(ChatMessage, string)> conversationWithName)
        {
            var conversation = conversationWithName.Select(c =>
            {
                if(c.Item2 == nextSpeaker.Name)
                {
                    return c.Item1;
                }
                else
                {
                    var content = c.Item1.Content;
                    // add From name: prefix
                    content = @$"From {c.Item2}<eof_name>:
{content}";
                    var msg = new ChatMessage(ChatRole.User, content);

                    return msg;
                }
            });

            return conversation;
        }

        public void PrettyPrintMessage(ChatMessage message, string name)
        {
            // write result
            Console.WriteLine($"Message from {name}");
            // write a seperator
            Console.WriteLine(new string('-', 20));
            Console.WriteLine(message.Content);
            Console.WriteLine(new string('-', 20));
        }
    }
}
