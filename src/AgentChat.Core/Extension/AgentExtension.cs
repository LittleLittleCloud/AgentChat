using AgentChat;
using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    public static class AgentExtension
    {
        public static async Task<IEnumerable<IChatMessage>> SendMessageToGroupAsync(
            this IAgent agent,
            GroupChat groupChat,
            string msg,
            int maxRound = 10,
            bool throwWhenMaxRoundReached = false,
            CancellationToken ct = default)
        {
            var chatMessage = new Message(ChatRole.User, msg, from: agent.Name);

            return await agent.SendMessageToGroupAsync(groupChat, chatMessage, maxRound, throwWhenMaxRoundReached, ct);
        }

        public static async Task<IEnumerable<IChatMessage>> SendMessageToGroupAsync(
            this IAgent agent,
            GroupChat groupChat,
            IChatMessage msg,
            int maxRound = 10,
            bool throwWhenMaxRoundReached = false,
            CancellationToken ct = default)
        {
            if (msg.From != agent.Name)
            {
                throw new ArgumentException("The message is not from the agent", nameof(msg));
            }

            return await groupChat.CallAsync(new[] { msg }, maxRound, throwWhenMaxRoundReached);
        }

        public static async Task<IChatMessage> SendMessageAsync(
            this IAgent agent,
            CancellationToken ct = default)
        {
            return await agent.SendMessageAsync(Enumerable.Empty<IChatMessage>(), ct);
        }

        public static async Task<IChatMessage> SendMessageAsync(
            this IAgent agent,
            string msg,
            CancellationToken ct = default)
        {
            var userMessage = new Message(ChatRole.User, msg);

            return await agent.SendMessageAsync(userMessage, ct);
        }

        public static async Task<IChatMessage> SendMessageAsync(
            this IAgent agent,
            IChatMessage msg,
            CancellationToken ct = default)
        {
            var history = new[] { msg };

            return await agent.SendMessageAsync(history, ct);
        }

        public static async Task<IChatMessage> SendMessageAsync(
            this IAgent agent,
            IEnumerable<IChatMessage> history,
            CancellationToken ct = default)
        {
            return await agent.CallAsync(history, ct);
        }

        /// <inheritdoc cref="SendMessageToAgentAsync(IAgent, IAgent, IEnumerable{IChatMessage}?, int, bool, CancellationToken)"/>
        /// <param name="message">message to add. This message will be added as <see cref="ChatRole.User"/> message.</param>
        public static async Task<IEnumerable<IChatMessage>> SendMessageToAgentAsync(
            this IAgent agent,
            IAgent receiver,
            int maxRound = 10,
            bool throwWhenMaxRoundReached = false,
            CancellationToken ct = default)
        {
            return await agent.SendMessageToAgentAsync(receiver, Enumerable.Empty<IChatMessage>(), maxRound, throwWhenMaxRoundReached, ct);
        }

        /// <inheritdoc cref="SendMessageToAgentAsync(IAgent, IAgent, IEnumerable{IChatMessage}?, int, bool, CancellationToken)"/>
        /// <param name="message">message to add. This message will be added as <see cref="ChatRole.User"/> message.</param>
        public static async Task<IEnumerable<IChatMessage>> SendMessageToAgentAsync(
            this IAgent agent,
            IAgent receiver,
            string message,
            int maxRound = 10,
            bool throwWhenMaxRoundReached = false,
            CancellationToken ct = default)
        {
            var chatMessage = new Message(ChatRole.User, message, from: agent.Name);

            var chatHistory = new[] { chatMessage };

            return await agent.SendMessageToAgentAsync(receiver, chatHistory, maxRound, throwWhenMaxRoundReached, ct);
        }

        /// <inheritdoc cref="SendMessageToAgentAsync(IAgent, IAgent, IEnumerable{IChatMessage}?, int, bool, CancellationToken)"/>
        /// <param name="message">message to add. This message will be added as <see cref="ChatRole.User"/> message.</param>
        public static async Task<IEnumerable<IChatMessage>> SendMessageToAgentAsync(
            this IAgent agent,
            IAgent receiver,
            IChatMessage message,
            int maxRound = 10,
            bool throwWhenMaxRoundReached = false,
            CancellationToken ct = default)
        {
            var chatHistory = new[] { message };

            return await agent.SendMessageToAgentAsync(receiver, chatHistory, maxRound, throwWhenMaxRoundReached, ct);
        }

        /// <summary>
        /// Send message to another agent.
        /// </summary>
        /// <param name="agent">sender agent.</param>
        /// <param name="receiver">receiver agent.</param>
        /// <param name="chatHistory">chat history.</param>
        /// <param name="maxRound">max conversation round.</param>
        /// <param name="throwWhenMaxRoundReached">if true, throw an exception when <paramref name="maxRound"/> reached.</param>
        /// <returns>conversation history</returns>
        public static async Task<IEnumerable<IChatMessage>> SendMessageToAgentAsync(
            this IAgent agent,
            IAgent receiver,
            IEnumerable<IChatMessage> chatHistory,
            int maxRound = 10,
            bool throwWhenMaxRoundReached = false,
            CancellationToken ct = default)
        {
            var groupChat = new SequentialGroupChat(new[] { agent, receiver });

            return await groupChat.CallAsync(chatHistory, maxRound, throwWhenMaxRoundReached);
        }

        /// <summary>
        /// Add auto reply message to the agent. If the conversation matches the condition, the auto reply message will be sent as reply. Otherwise, the agent will be called to generate the reply message.
        /// Multiple auto reply messages can be added to the agent.
        /// And the auto reply messages will be called in the order they are added.
        /// </summary>
        /// <param name="agent">agent to add auto reply</param>
        /// <param name="autoReplyMessageFunc">function to determine the auto reply message.
        /// If the function returns a message, that message will be sent as auto reply.
        /// If the function returns null, the <paramref name="agent"/> will be called to generate the reply message.</param>
        public static IAgent WithAutoReply(
            this IAgent agent,
            Func<IEnumerable<IChatMessage>, IChatMessage?> autoReplyMessageFunc)
        {
            var func = new Func<IEnumerable<IChatMessage>, Task<IChatMessage?>>(x => Task.FromResult(autoReplyMessageFunc(x)));
            
            return WithAutoReply(agent, func);
        }

        /// <inheritdoc cref="WithAutoReply(IAgent, Func{IEnumerable{IChatMessage}, IChatMessage?})"/>
        public static IAgent WithAutoReply(
            this IAgent agent,
            Func<IEnumerable<IChatMessage>, Task<IChatMessage?>> autoReplyMessageFunc)
        {
            if (agent is AutoReplyAgent autoReply)
            {
                autoReply.AddAutoReplyMessage(autoReplyMessageFunc);

                return autoReply;
            }
            else
            {
                var newAgent = new AutoReplyAgent(agent);
                newAgent.AddAutoReplyMessage(autoReplyMessageFunc);

                return newAgent;
            }
        }

        /// <inheritdoc cref="WithPreprocess(IAgent, Func{IEnumerable{IChatMessage}, Task{IEnumerable{IChatMessage}}})"/>
        public static IAgent WithPreprocess(
            this IAgent agent,
            Func<IEnumerable<IChatMessage>, IEnumerable<IChatMessage>> preprocessFunc)
        {
            var func = new Func<IEnumerable<IChatMessage>, Task<IEnumerable<IChatMessage>>>(x => Task.FromResult(preprocessFunc(x)));

            return agent.WithPreprocess(func);
        }

        /// <summary>
        /// Add preprocess function to the agent.
        /// The preprocess function will be called before calling the agent to generate the reply message and after no auto reply is available.
        /// Multiple preprocess functions can be added to the agent.
        /// And the preprocess functions will be called in the order they are added.
        /// </summary>
        /// <param name="agent">The agent to add preprocess function</param>
        /// <param name="preprocessFunc">preprocess function to be added</param>
        public static IAgent WithPreprocess(
            this IAgent agent,
            Func<IEnumerable<IChatMessage>, Task<IEnumerable<IChatMessage>>> preprocessFunc)
        {
            if (agent is AutoReplyAgent autoReply)
            {
                autoReply.AddPreProcess(preprocessFunc);

                return autoReply;
            }
            else
            {
                var newAgent = new AutoReplyAgent(agent);
                newAgent.AddPreProcess(preprocessFunc);

                return newAgent;
            }
        }

        /// <inheritdoc cref="WithPostprocess(IAgent, Func{IChatMessage, Task{IChatMessage}})"/>
        public static IAgent WithPostprocess(
            this IAgent agent,
            Func<IChatMessage, IChatMessage> postprocessFunc)
        {
            var func = new Func<IChatMessage, Task<IChatMessage>>(x => Task.FromResult(postprocessFunc(x)));

            return agent.WithPostprocess(func);
        }

        /// <summary>
        /// Add postprocess function to the agent.
        /// The postprocess function will be called after the agent generate the reply message.
        /// Multiple postprocess functions can be added to the agent.
        /// And the postprocess functions will be called in the order they are added.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="postprocessFunc"></param>
        /// <returns></returns>
        public static IAgent WithPostprocess(
            this IAgent agent,
            Func<IChatMessage, Task<IChatMessage>> postprocessFunc)
        {
            if (agent is AutoReplyAgent autoReply)
            {
                autoReply.AddPostProcess(postprocessFunc);

                return autoReply;
            }
            else
            {
                var newAgent = new AutoReplyAgent(agent);
                newAgent.AddPostProcess(postprocessFunc);

                return newAgent;
            }
        }
    }
}
