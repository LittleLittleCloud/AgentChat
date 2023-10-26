using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AgentChat
{
    public static class GroupChatExtension
    {
        public static void AddInitializeMessage(this IAgent agent, string message, GroupChat groupChat)
        {
            var msg = new Message(ChatRole.User, message, from: agent.Name);

            groupChat.AddInitializeMessage(msg);
        }

        public static IEnumerable<IChatMessage> MessageToKeep(
            this IGroupChat _,
            IEnumerable<IChatMessage> messages)
        {
            var lastCLRMessageIndex = messages.ToList()
                    .FindLastIndex(x => x.IsGroupChatClearMessage());

            // if multiple clr messages, e.g [msg, clr, msg, clr, msg, clr, msg]
            // only keep the the messages after the second last clr message.
            if (messages.Count(m => m.IsGroupChatClearMessage()) > 1)
            {
                lastCLRMessageIndex = messages.ToList()
                    .FindLastIndex(lastCLRMessageIndex - 1, lastCLRMessageIndex - 1, x => x.IsGroupChatClearMessage());
                messages = messages.Skip(lastCLRMessageIndex);
            }

            lastCLRMessageIndex = messages.ToList()
                .FindLastIndex(x => x.IsGroupChatClearMessage());

            if (lastCLRMessageIndex != -1 && messages.Count() - lastCLRMessageIndex >= 2)
            {
                messages = messages.Skip(lastCLRMessageIndex);
            }

            return messages;
        }

        public static IEnumerable<IChatMessage> ProcessConversationForAgent(
            this IGroupChat groupChat,
            IEnumerable<IChatMessage> initialMessages,
            IEnumerable<IChatMessage> messages)
        {
            messages = groupChat.MessageToKeep(messages);
            return initialMessages.Concat(messages);
        }

        public static IEnumerable<IChatMessage> ProcessConversationsForRolePlay(
                this IGroupChat groupChat,
                IChatLLM chatLLM,
                IEnumerable<IChatMessage> initialMessages,
                IEnumerable<IChatMessage> messages)
        {
            messages = groupChat.MessageToKeep(messages);
            var messagesToKeep = initialMessages.Concat(messages);

            return messagesToKeep.Select((x, i) =>
            {
                var msg = @$"From {x.From}:
{x.Content}
<eof_msg>
round # {i}";
                return chatLLM.CreateChatMessage(ChatRole.User, content: msg);
            });
        }
    }
}
