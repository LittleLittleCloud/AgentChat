using AgentChat.Example.Share;
using Azure.AI.OpenAI;
using FluentAssertions;
using Xunit;

namespace AgentChat.Core.Tests
{
    public class AutoReplyAgentTest
    {
        [Fact]
        public async Task GPTAgentWithOneAutoreplyAsyncTest()
        {
            IAgent alice = Constant.GPT35.CreateAgent(
                name: "Alice",
                roleInformation: $@"You are a helpful AI assistant")
                .WithAutoReply((conversations) => Constant.GPT35.CreateChatMessage(ChatRole.Assistant, "I'm your father", from: "Alice"));

            var msg = Constant.GPT35.CreateChatMessage(ChatRole.User, "hey", from: "Bob");
            var reply = await alice.SendMessageAsync(msg);
            reply.From.Should().Be("Alice");
            reply.Content.Should().Be("I'm your father");
            reply.Role.Should().Be(ChatRole.Assistant);
        }

        [Fact]
        public async Task GPTAgentWithMultipleAutoReplyAsyncTest()
        {
            IAgent alice = Constant.GPT35.CreateAgent(
               name: "Alice",
               roleInformation: $@"You are a helpful AI assistant")
               .WithAutoReply((conversations) =>
               {
                   if (conversations.Count() == 1)
                   {
                       return Constant.GPT35.CreateChatMessage(ChatRole.Assistant, "I'm your father", from: "Alice");
                   }

                   return null;
               })
               .WithAutoReply((conversations) =>
               {
                   return Constant.GPT35.CreateChatMessage(ChatRole.Assistant, "HaHa kidding", from: "Alice");
               });

            var msg = Constant.GPT35.CreateChatMessage(ChatRole.User, "hey", from: "Bob");
            var reply = await alice.SendMessageAsync(msg);
            reply.From.Should().Be("Alice");
            reply.Content.Should().Be("I'm your father");
            reply.Role.Should().Be(ChatRole.Assistant);
            var conversation = new[] { msg, reply };
            reply = await alice.SendMessageAsync(conversation);
            reply.From.Should().Be("Alice");
            reply.Content.Should().Be("HaHa kidding");
            reply.Role.Should().Be(ChatRole.Assistant);
        }

        [Fact]
        public async Task PreprocessFunctionTestAsync()
        {
            IAgent echoAgent = new EchoAgent("echo");
            echoAgent = echoAgent.WithPreprocess((msgs) =>
            {
                return msgs.Select(msg => new Message(msg.Role, msg.Content?.ToUpper(), msg.Name, msg.From));
            });

            var msg = new Message(ChatRole.User, "heLLo", "Bob");
            var reply = await echoAgent.SendMessageAsync(msg);

            reply.Content.Should().Be("HELLO");
            reply.From.Should().Be("echo");

            echoAgent = echoAgent.WithPreprocess((msgs) =>
            {
                return msgs.Select(msg => new Message(msg.Role, msg.Content?.ToLower(), msg.Name, msg.From));
            });

            reply = await echoAgent.SendMessageAsync(msg);
            reply.Content.Should().Be("hello");
            reply.From.Should().Be("echo");
        }

        [Fact]
        public async Task PostProcessFunctionTestAsync()
        {
            IAgent echoAgent = new EchoAgent("echo");
            echoAgent = echoAgent.WithPostprocess((msg) =>
            {
                return new Message(msg.Role, msg.Content?.ToUpper(), msg.Name, msg.From);
            });

            var msg = new Message(ChatRole.User, "heLLo", "Bob");
            var reply = await echoAgent.SendMessageAsync(msg);

            reply.Content.Should().Be("HELLO");
            reply.From.Should().Be("echo");

            echoAgent = echoAgent.WithPostprocess((msg) =>
            {
                return new Message(msg.Role, msg.Content?.ToLower(), msg.Name, msg.From);
            });

            reply = await echoAgent.SendMessageAsync(msg);
            reply.Content.Should().Be("hello");
            reply.From.Should().Be("echo");
        }



        private class EchoAgent : IAgent
        {
            public EchoAgent(string name)
            {
                Name = name;
            }
            public string Name { get; }

            public Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default)
            {
                // return the most recent message
                var lastMessage = conversation.LastOrDefault();
                var reply = new Message(ChatRole.Assistant, lastMessage?.Content, null, Name);

                return Task.FromResult<IChatMessage>(reply);
            }
        }
    }
}
