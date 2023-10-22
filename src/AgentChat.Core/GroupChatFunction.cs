using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AgentChat
{
    public partial class GroupChatFunction
    {
        /// <summary>
        /// terminate the group chat.
        /// </summary>
        /// <param name="message">terminate message.</param>
        [FunctionAttribution]
        public async Task<string> TerminateGroupChat(string message)
        {
            return $"{GroupChat.TERMINATE}: {message}";
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
{GroupChat.CLEAR_MESSAGES}
";
        }
    }
}
