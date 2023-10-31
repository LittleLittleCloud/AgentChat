using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat.OpenAI
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
                    // add as asssistant message
                    var content = message.Content ?? string.Empty;
                    content = @$"{content}
<eof_msg>
round # {i++}";
                    yield return new ChatMessage(ChatRole.Assistant, content);
                }
            }
        }

        public static IAgent CreateAgent(
            this GPTInstruct gptInstruct,
            string agentName,
            string roleInformation,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null)
        {
            var anotherGPT = new GPTInstruct(
                gptInstruct,
                temperature: temperature,
                maxToken: maxToken,
                stopWords: stopWords);

            IAgent agent = new ChatAgent(anotherGPT, agentName);

            // add role information
            agent = agent.CreatePreprocessAgent(agentName, (msgs, ct) =>
            {
                var systemMessage = new Message(Role.System, roleInformation, from: agentName);

                return Task.FromResult(new[] { systemMessage }.Concat(msgs));
            });

            return agent;
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
    }
}
