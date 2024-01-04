using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat;

/// <summary>
///     The auto-reply agent contains an inner agent and an auto-reply function.
///     When calling into the auto-reply agent, it will first try to reply the message using the auto-reply function.
///     If the auto-reply function returns a null value, the inner agent will be called to generate the reply message.
/// </summary>
public class AutoReplyAgent : IAgent
{
    /// <summary>
    ///     The auto-reply function that will be called before calling <see cref="InnerAgent" />.
    ///     If the function returns a non-null value, the agent will not be called.
    ///     Otherwise, the agent will be called.
    /// </summary>
    public Func<IEnumerable<IChatMessage>, CancellationToken?, Task<IChatMessage?>> AutoReplyFunc { get; }

    /// <summary>
    ///     The inner agent.
    /// </summary>
    public IAgent InnerAgent { get; }

    /// <summary>
    ///     Create an auto-reply agent.
    /// </summary>
    /// <param name="innerAgent">the inner agent.</param>
    /// <param name="name">the name of <see cref="AutoReplyAgent" />.</param>
    /// <param name="autoReplyFunc">auto-reply function.</param>
    public AutoReplyAgent(
        IAgent innerAgent,
        string name,
        Func<IEnumerable<IChatMessage>, CancellationToken?, Task<IChatMessage?>> autoReplyFunc)
    {
        InnerAgent = innerAgent;
        Name = name;
        AutoReplyFunc = autoReplyFunc;
    }

    public string Name { get; }

    /// <summary>
    ///     call the agent to generate reply message.
    ///     It will first try to auto reply the message. If no auto reply is available, the <see cref="InnerAgent" /> will be
    ///     called to generate the reply message.
    /// </summary>
    public async Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default)
    {
        if (await AutoReplyFunc(conversation, ct) is IChatMessage autoReply)
        {
            return autoReply;
        }

        return await InnerAgent.CallAsync(conversation, ct);
    }
}