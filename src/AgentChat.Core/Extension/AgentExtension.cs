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
        /// Create an <see cref="AutoReplyAgent"/> from the <paramref name="agent"/>.
        /// </summary>
        /// <param name="agent">inner agent</param>
        /// <param name="name">name of the <see cref="AutoReplyAgent"/></param>
        /// <param name="autoReplyMessageFunc">function to determine the auto reply message.
        /// If the function returns a message, that message will be sent as auto reply.
        /// If the function returns null, the <paramref name="agent"/> will be called to generate the reply message.</param>
        /// <returns><see cref="AutoReplyAgent"/></returns>
        public static AutoReplyAgent CreateAutoReplyAgent(
            this IAgent agent,
            string name,
            Func<IEnumerable<IChatMessage>, CancellationToken?, Task<IChatMessage?>> autoReplyMessageFunc)
        {
            return new AutoReplyAgent(agent, name, autoReplyMessageFunc);
        }


        /// <summary>
        /// Create an <see cref="PreprocessAgent"/> from the <paramref name="innerAgent"/>.
        /// </summary>
        /// <param name="innerAgent">The inner agent</param>
        /// <param name="preprocessFunc">preprocess function to be added</param>
        /// <returns><see cref="PreprocessAgent"/></returns>
        public static PreprocessAgent CreatePreprocessAgent(
            this IAgent innerAgent,
            string name,
            Func<IEnumerable<IChatMessage>, CancellationToken?, Task<IEnumerable<IChatMessage>>> preprocessFunc)
        {
           return new PreprocessAgent(innerAgent, name, preprocessFunc);
        }

        /// <summary>
        /// Create an <see cref="PostProcessAgent"/> from the <paramref name="innerAgent"/>.
        /// </summary>
        /// <param name="innerAgent">inner agent.</param>
        /// <param name="postprocessFunc">post process function to be added.
        /// The function takes the conversation history, the reply message and the cancellation token as input.
        /// And returns the post processed reply message.</param>
        /// <returns><see cref="PostProcessAgent"/></returns>
        public static PostProcessAgent CreatePostProcessAgent(
            this IAgent innerAgent,
            string name,
            Func<IEnumerable<IChatMessage>, IChatMessage, CancellationToken?, Task<IChatMessage>> postprocessFunc)
        {
            return new PostProcessAgent(innerAgent, name, postprocessFunc);
        }
    }
}
