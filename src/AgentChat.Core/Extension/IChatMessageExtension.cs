﻿using System;

namespace AgentChat;

public static class IChatMessageExtension
{
    public const string CLEAR_MESSAGES = "[GROUPCHAT_CLEAR_MESSAGES]";

    public const string TERMINATE = "[GROUPCHAT_TERMINATE]";

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

    public static bool IsGroupChatClearMessage(this IChatMessage message) => message.Content?.Contains(CLEAR_MESSAGES) ?? false;

    /// <summary>
    ///     Return true if <see cref="IChatMessage.Content" /> contains <see cref="TERMINATE" />, otherwise false.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static bool IsGroupChatTerminateMessage(this IChatMessage message) => message.Content?.Contains(TERMINATE) ?? false;

    public static string PrettyPrintMessage(this IChatMessage message)
    {
        var result = message.FormatMessage();
        Console.WriteLine(result);
        return result;
    }
}