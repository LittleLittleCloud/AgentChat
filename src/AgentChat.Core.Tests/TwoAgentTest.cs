using AgentChat.Example.Share;
using Azure.AI.OpenAI;
using FluentAssertions;

namespace AgentChat.Core.Tests
{
    public partial class TwoAgentTest
    {
        /// <summary>
        /// tell your name
        /// </summary>
        /// <param name="name">your name.</param>
        [FunctionAttribution]
        public async Task<string> TellYourName(string name)
        {
            return $"My name is {name}";
        }

        [ApiKeyFact("AZURE_OPENAI_API_KEY")]
        public async Task TwoAgentChatTest()
        {
            var alice = Constant.GPT35.CreateAgent(
                name: "Alice",
                roleInformation: $@"You are a helpful AI assistant");

            var bob = Constant.GPT35.CreateAgent(
                name: "Bob",
                roleInformation: $@"You call TellYourName function",
                functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
                {
                    { this.TellYourNameFunction, this.TellYourNameWrapper },
                });

            var msgs = await alice.SendMessageAsync(bob, "hey what's your name", maxRound: 1);

            msgs.Should().HaveCount(2);
            msgs.First().Content.Should().Be("hey what's your name");
            msgs.First().From.Should().Be(alice.Name);
            msgs.Last().Content.Should().Be("My name is Bob");
            msgs.Last().From.Should().Be(bob.Name);
        }
    }
}
