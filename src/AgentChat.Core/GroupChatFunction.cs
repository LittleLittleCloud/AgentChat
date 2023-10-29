using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AgentChat
{
    public partial class GroupChatFunction
    {
        public const string TERMINATE = "[GROUPCHAT_TERMINATE]";
        public const string CLEAR_MESSAGES = "// ignore this line [GROUPCHAT_CLEAR_MESSAGES]";

        /// <summary>
        /// terminate the group chat.
        /// </summary>
        /// <param name="message">terminate message.</param>
        [FunctionAttribution]
        public Task<string> TerminateGroupChat(string message)
        {
            return Task.FromResult($"{GroupChatFunction.TERMINATE}: {message}");
        }

        /// <summary>
        /// Summarize the current conversation.
        /// </summary>
        /// <param name="context">conversation context.</param>
        [FunctionAttribution]
        public Task<string> ClearGroupChat(string context)
        {

            var msg = @$"{context}
<eof_msg>
{GroupChatFunction.CLEAR_MESSAGES}
";
            return Task.FromResult(msg);
        }
    }
}
