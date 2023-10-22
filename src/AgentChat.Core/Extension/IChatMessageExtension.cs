using System;
using System.Collections.Generic;
using System.Text;

namespace AgentChat
{
    public static class IChatMessageExtension
    {
        public static string FormatMessage(this IChatMessage message)
        {
            // write result
            var result = $"Message from {message.From}\n";
            // write a seperator
            result += new string('-', 20) + "\n";
            result += message.Content + "\n";
            result += new string('-', 20) + "\n";

            return result;
        }

        public static string PrettyPrintMessage(this IChatMessage message)
        {
            var result = message.FormatMessage();
            Console.WriteLine(result);
            return result;
        }
        public static bool IsGroupChatTerminateMessage(this IChatMessage message)
        {
            return message.Content?.Contains(GroupChatFunction.TERMINATE) ?? false;
        }

        public static bool IsGroupChatClearMessage(this IChatMessage message)
        {
            return message.Content?.Contains(GroupChatFunction.CLEAR_MESSAGES) ?? false;
        }

    }
}
