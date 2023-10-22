using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AgentChat.Core
{
    public static class GroupChatExtension
    {
        public static bool IsGroupChatTerminateMessage(this ChatMessage message)
        {
            return message.Content.Contains(GroupChat.TERMINATE);
        }

        public static bool IsGroupChatClearMessage(this ChatMessage message)
        {
            return message.Content.Contains(GroupChat.CLEAR_MESSAGES);
        }

        public static IEnumerable<(ChatMessage, string)> MessageToKeep(
            this GroupChat _,
            IEnumerable<(ChatMessage, string)> messages)
        {
            var lastCLRMessageIndex = messages.ToList()
                    .FindLastIndex(x => x.Item1.IsGroupChatClearMessage());

            // if multiple clr messages, e.g [msg, clr, msg, clr, msg, clr, msg]
            // only keep the the messages after the second last clr message.
            if (messages.Count(m => m.Item1.IsGroupChatClearMessage()) > 1)
            {
                lastCLRMessageIndex = messages.ToList()
                    .FindLastIndex(lastCLRMessageIndex - 1, lastCLRMessageIndex - 1, x => x.Item1.IsGroupChatClearMessage());
                messages = messages.Skip(lastCLRMessageIndex);
            }

            lastCLRMessageIndex = messages.ToList()
                .FindLastIndex(x => x.Item1.IsGroupChatClearMessage());

            if (lastCLRMessageIndex != -1 && messages.Count() - lastCLRMessageIndex >= 2)
            {
                messages = messages.Skip(lastCLRMessageIndex);
            }

            Console.WriteLine($"message length {messages.Count()}");


            return messages;
        }

        public static IEnumerable<ChatMessage> ProcessConversationForAgent(
            this GroupChat groupChat,
            string nextSpeaker,
            IEnumerable<(ChatMessage, string)> initialMessages,
            IEnumerable<(ChatMessage, string)> messages)
        {
            messages = groupChat.MessageToKeep(messages);
            var messagesToKeep = initialMessages.Concat(messages);
            var messagesForAgent = new List<ChatMessage>();
            foreach (var message in messagesToKeep)
            {
                var i = messagesForAgent.Count;
                if (message.Item2 != nextSpeaker)
                {
                    // add as user message
                    var content = message.Item1.Content;
                    content = @$"{content}
<eof_msg>
From {message.Item2}
round # {i}";
                    var msg = new ChatMessage(ChatRole.User, content);
                    messagesForAgent.Add(msg);
                }
                else
                {
                    // add as agent message
                    if (message.Item1.FunctionCall != null)
                    {
                        var functionCallMessage = new ChatMessage(ChatRole.Assistant, null)
                        {
                            FunctionCall = message.Item1.FunctionCall,
                        };

                        messagesForAgent.Add(functionCallMessage);

                        var functionResultMessage = new ChatMessage(ChatRole.Function, message.Item1.Content)
                        {
                            Name = message.Item1.Name,
                        };

                        messagesForAgent.Add(functionResultMessage);
                    }
                    else
                    {
                        // add suffix
                        var content = message.Item1.Content;
                        content = @$"{content}
<eof_msg>
round # {i}";

                        var assistantMessage = new ChatMessage(ChatRole.Assistant, content);

                        messagesForAgent.Add(assistantMessage);
                    }
                }
            }

            return messagesForAgent;
        }

        public static IEnumerable<ChatMessage> ProcessConversationsForRolePlay(
                this GroupChat groupChat,
                IEnumerable<(ChatMessage, string)> initialMessages,
                IEnumerable<(ChatMessage, string)> messages)
        {
            messages = groupChat.MessageToKeep(messages);
            var messagesToKeep = initialMessages.Concat(messages);

            return messagesToKeep.Select((x, i) =>
            {
                var msg = @$"From {x.Item2}:
{x.Item1.Content}
<eof_msg>
round # {i}";
                return new ChatMessage(ChatRole.User, msg);
            });
        }
    }
}
