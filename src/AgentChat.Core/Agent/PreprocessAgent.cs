using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat;

/// <summary>
///     The preprocess agent preprocesses the conversation before passing it to the inner agent by calling the preprocess
///     function.
/// </summary>
public class PreprocessAgent : IAgent
{
    public IAgent InnerAgent { get; }

    public Func<IEnumerable<IChatMessage>, CancellationToken?, Task<IEnumerable<IChatMessage>>> PreprocessFunc { get; }

    /// <summary>
    ///     Create a wrapper agent that preprocesses the conversation before passing it to the inner agent.
    /// </summary>
    /// <param name="innerAgent">inner agent.</param>
    /// <param name="name">name</param>
    /// <param name="preprocessFunc">preprocess function</param>
    public PreprocessAgent(
        IAgent innerAgent,
        string name,
        Func<IEnumerable<IChatMessage>, CancellationToken?, Task<IEnumerable<IChatMessage>>> preprocessFunc)
    {
        InnerAgent = innerAgent;
        Name = name;
        PreprocessFunc = preprocessFunc;
    }

    public string Name { get; }

    /// <summary>
    ///     First preprocess the <paramref name="conversation" /> using the <see cref="PreprocessFunc" /> and then pass the
    ///     preprocessed conversation to the <see cref="InnerAgent" />."/>
    /// </summary>
    public async Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default)
    {
        var messages = await PreprocessFunc(conversation, ct);
        return await InnerAgent.CallAsync(messages, ct);
    }
}