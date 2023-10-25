using AgentChat.Example.Share;
using Azure.AI.OpenAI;
using FluentAssertions;
using Xunit;

namespace AgentChat.Core.Tests
{
    public class AutoReplyAgentTest
    {
        [Fact]
        public async Task AutoReplyAsyncTest()
        {
            IAgent alice = new EchoAgent("Alice")
                .WithAutoReply((conversations) => new Message(ChatRole.Assistant, "I'm your father", from: "Alice"));

            var msg = new Message(ChatRole.User, "hey", from: "Bob");
            var reply = await alice.SendMessageAsync(msg);
            reply.From.Should().Be("Alice");
            reply.Content.Should().Be("I'm your father");
            reply.Role.Should().Be(ChatRole.Assistant);
        }

        [Fact]
        public async Task MultipleAutoReplyAsyncTest()
        {
            IAgent alice = new EchoAgent("Alice")
               .WithAutoReply((conversations) =>
               {
                   if (conversations.Count() == 1)
                   {
                       return new Message(ChatRole.Assistant, "I'm your father", from: "Alice");
                   }

                   return null;
               })
               .WithAutoReply((conversations) =>
               {
                   return new Message(ChatRole.Assistant, "HaHa kidding", from: "Alice");
               });

            var msg = new Message(ChatRole.User, "hey", from: "Bob");
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
    }
}
