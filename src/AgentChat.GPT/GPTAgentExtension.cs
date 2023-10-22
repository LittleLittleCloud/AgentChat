using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    public static class GPTAgentExtension
    {
        internal static IEnumerable<ChatMessage> ProcessChatMessages(this GPTAgent agent, IEnumerable<IChatMessage> messages)
        {
            var i = 0;
            foreach (var message in messages)
            {
                if (message.From != agent.Name)
                {
                    // add as user message
                    var content = message.Content ?? string.Empty;
                    content = @$"{content}
<eof_msg>
From {message.From}
round # {i++}";
                    yield return new ChatMessage(ChatRole.User, content);
                }
                else if (message is GPTChatMessage gptMessage && gptMessage.ChatMessage is ChatMessage chatMessage)
                {
                    if (chatMessage.FunctionCall != null)
                    {
                        var functonCallMessage = new ChatMessage(ChatRole.Assistant, null)
                        {
                            FunctionCall = chatMessage.FunctionCall,
                        };

                        i++;

                        yield return functonCallMessage;

                        var functionResultMessage = new ChatMessage(ChatRole.Function, chatMessage.Content)
                        {
                            Name = chatMessage.Name,
                        };

                        yield return functionResultMessage;

                        i++;
                    }
                    else
                    {
                        // add suffix
                        var content = chatMessage.Content ?? string.Empty;
                        content = @$"{content}
<eof_msg>
round # {i++}";
                        
                        var assistantMessage = new ChatMessage(ChatRole.Assistant, content);

                        yield return assistantMessage;
                    }
                }
                else
                {
                    throw new ArgumentException("chat message is null");
                }
            }
        }

        public static async Task<IEnumerable<IChatMessage>> SendMessageAsync(
            this GPTAgent agent,
            string msg,
            GroupChat groupChat,
            int maxRound = 10,
            bool throwWhenMaxRoundReached = true,
            CancellationToken ct = default)
        {
            var chatMessage = new ChatMessage(ChatRole.User, msg);
            var gptMessage = new GPTChatMessage(chatMessage)
            {
                From = agent.Name,
            };

            return await groupChat.CallAsync(new[] { gptMessage }, maxRound, throwWhenMaxRoundReached);
        }

        public static async Task<IEnumerable<IChatMessage>> SendMessageAsync(
            this GPTAgent agent,
            IAgent receiver,
            string msg,
            IEnumerable<IChatMessage>? chatHistory = null,
            int maxRound = 10,
            bool throwWhenMaxRoundReached = false,
            CancellationToken ct = default)
        {
            var chatMessage = new ChatMessage(ChatRole.Assistant, msg);
            var gptMessage = new GPTChatMessage(chatMessage)
            {
                From = agent.Name,
            };

            chatHistory = chatHistory?.Append(gptMessage) ?? new[] { gptMessage };

            return await agent.SendMessageAsync(receiver, chatHistory, maxRound, throwWhenMaxRoundReached, ct);
        }

        public static async Task<IChatMessage> SendMessageAsync(
            this GPTAgent agent,
            string msg,
            CancellationToken ct = default)
        {
            var chatMessage = new ChatMessage(ChatRole.User, msg);
            var gptMessage = new GPTChatMessage(chatMessage);

            return await agent.CallAsync(new[] { gptMessage }, ct);
        }

        public static GPTAgent CreateAgent(
            this GPT gpt,
            string name,
            string roleInformation,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null,
            Dictionary<FunctionDefinition, Func<string, Task<string>>>? functionMap = null)
        {
            var anotherGPT = new GPT(
                gpt,
                temperature: temperature,
                maxToken: maxToken,
                stopWords: stopWords,
                functionDefinitions: functionMap?.Keys.ToArray());

            return new GPTAgent(
                anotherGPT,
                name,
                roleInformation,
                functionMap);
        }

        public static void AddInitializeMessage(this GPTAgent agent, string message, GroupChat groupChat)
        {
            var messageToAdd = new ChatMessage(ChatRole.User, message);
            var gptMessage = new GPTChatMessage(messageToAdd)
            {
                From = agent.Name,
            };

            groupChat.AddInitializeMessage(gptMessage);
        }
    }
}
