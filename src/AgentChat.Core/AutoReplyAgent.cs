using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AgentChat
{
    /// <summary>
    /// Agent that auto reply a message if the conversation matches the condition.
    /// </summary>
    public class AutoReplyAgent : IAgent
    {
        private readonly IAgent _agent;
        private readonly List<Func<IEnumerable<IChatMessage>, Task<IChatMessage?>>> _autoReplyMessageFuncs = new List<Func<IEnumerable<IChatMessage>, Task<IChatMessage?>>>();
        private readonly List<Func<IEnumerable<IChatMessage>, Task<IEnumerable<IChatMessage>>>> _preProcessFuncs = new List<Func<IEnumerable<IChatMessage>, Task<IEnumerable<IChatMessage>>>>();
        private readonly List<Func<IChatMessage, Task<IChatMessage>>> _postProcessFuncs = new List<Func<IChatMessage, Task<IChatMessage>>>();

        public AutoReplyAgent(IAgent agent)
        {
            _agent = agent;
        }

        public void AddAutoReplyMessage(Func<IEnumerable<IChatMessage>, Task<IChatMessage?>> autoReplyMessageFunc)
        {
            _autoReplyMessageFuncs.Add(autoReplyMessageFunc);
        }

        public void AddPreProcess(Func<IEnumerable<IChatMessage>, Task<IEnumerable<IChatMessage>>> preProcessFunc)
        {
            _preProcessFuncs.Add(preProcessFunc);
        }

        public void AddPostProcess(Func<IChatMessage, Task<IChatMessage>> postProcessFunc)
        {
            _postProcessFuncs.Add(postProcessFunc);
        }

        public string Name => _agent.Name;

        /// <summary>
        /// call the agent to generate reply message.
        /// It will first try to auto reply the message. If no auto reply is available, the agent will be called to generate the reply message.
        /// Before calling the agent, the conversation will be preprocessed by passing through all the preprocess functions from <see cref="AutoReplyAgent._preProcessFuncs"/>.
        /// Then the agent will be called to generate the reply message.
        /// And finally, the reply message will be postprocessed by passing through all the postprocess functions from <see cref="AutoReplyAgent._postProcessFuncs"/>.
        /// </summary>
        public async Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, System.Threading.CancellationToken ct = default)
        {
            // first, try auto reply
            foreach (var autoReplyMessageFunc in _autoReplyMessageFuncs)
            {
                var autoReplyMessage = await autoReplyMessageFunc(conversation);
                if (autoReplyMessage != null)
                {

                    return autoReplyMessage;
                }
            }

            // if no auto reply is available, call the agent
            // preprocess
            foreach (var preProcessFunc in _preProcessFuncs)
            {
                conversation = await preProcessFunc(conversation);
            }

            var replyMessage = await _agent.CallAsync(conversation, ct);

            // postprocess
            foreach (var postProcessFunc in _postProcessFuncs)
            {
                replyMessage = await postProcessFunc(replyMessage);
            }

            return replyMessage;
        }
    }
}
