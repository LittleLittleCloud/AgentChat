using Azure.AI.OpenAI;

namespace AgentChat.OpenAI;

public static class GPTExtension
{
    internal static string ToChatML(this IChatMessage message) =>
        @$"<|im_start|>{message.Role.ToChatRole()}
{message.Content}
<|im_end|>";

    public static ChatRole ToChatRole(this Role role)
    {
        if (role == Role.User)
        {
            return ChatRole.User;
        }

        if (role == Role.System)
        {
            return ChatRole.System;
        }

        return ChatRole.Assistant;
    }

    public static Role ToRole(this ChatRole role)
    {
        if (role == ChatRole.User)
        {
            return Role.User;
        }

        if (role == ChatRole.System)
        {
            return Role.System;
        }

        return Role.Assistant;
    }
}