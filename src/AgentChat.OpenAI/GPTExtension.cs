using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentChat.OpenAI
{
    public static class GPTExtension
    {
        internal static string ToChatML(this IChatMessage message)
        {
            return @$"<|im_start|>{message.Role.ToChatRole()}
{ message.Content}
<|im_end|>";
        }

        public static Role ToRole(this ChatRole role)
        {
            if (role == ChatRole.User)
            {
                return Role.User;
            }
            else if (role == ChatRole.System)
            {
                return Role.System;
            }

            return Role.Assistant;
        }

        public static ChatRole ToChatRole(this Role role)
        {
            if (role == Role.User)
            {
                return ChatRole.User;
            }
            else if (role == Role.System)
            {
                return ChatRole.System;
            }

            return ChatRole.Assistant;
        }
    }
}
