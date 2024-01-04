using System.Threading.Tasks;

namespace AgentChat.Example.Share;

public partial class GroupChatFunction
{
    /// <summary>
    ///     Summarize the current conversation.
    /// </summary>
    /// <param name="context">conversation context.</param>
    [FunctionAttribution]
    public Task<string> ClearGroupChat(string context)
    {
        var msg = @$"{context}
<eof_msg>
{IChatMessageExtension.CLEAR_MESSAGES}
";
        return Task.FromResult(msg);
    }

    /// <summary>
    ///     terminate the group chat.
    /// </summary>
    /// <param name="message">terminate message.</param>
    [FunctionAttribution]
    public Task<string> TerminateGroupChat(string message) => Task.FromResult($"{IChatMessageExtension.TERMINATE}: {message}");
}