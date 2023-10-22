using AgentChat.Example.Share;
using Azure.AI.OpenAI;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        //[ApiKeyFact("AZURE_OPENAI_API_KEY")]
        //public async Task TwoAgentChatTest()
        //{
        //    var alice = new GPTAgent(
        //        Constant.GPT35,
        //        "Alice",
        //        $@"You are a helpful AI assistant");

        //    var bob = new GPTAgent(
        //        Constant.GPT35,
        //        "Bob",
        //        $@"You are a helpful AI assistant",
        //        new Dictionary<FunctionDefinition, Func<string, Task<string>>>
        //        {
        //            { this.TellYourNameFunction, this.TellYourNameWrapper },
        //        });

        //    var msgs = await alice.SendMessageAsync("hey what's your name?", bob, 1);

        //    msgs.Should().HaveCount(2);
        //    msgs.First().Content.Should().Be("hey what's your name?");
        //    msgs.First().Role.Should().Be(ChatRole.Assistant);
        //    msgs.Last().Content.Should().Be("My name is Bob");
        //    msgs.Last().Role.Should().Be(ChatRole.User);
        //}
    }
}
