using AgentChat.Core.Tests;
using Azure.AI.OpenAI;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace AgentChat.OpenAI.Tests
{
    public partial class OpenAIAssistantAgentTests
    {
        private ITestOutputHelper output;

        public OpenAIAssistantAgentTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [ApiKeyFact]
        public async Task AssistantAgentHelloWorldTestAsync()
        {
            var client = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NullReferenceException("OPENAI_API_KEY is null"));

            var assistant = await OpenAIAssistantAgent.CreateAsync(
                client: client,
                name: "test",
                roleInformation: "You are a helpful AI assistant",
                description: "test");

            var reply = await assistant.SendMessageAsync("Hey what's 2+2?", ct: default);

            reply.From.Should().Be(assistant.Name);
            reply.Role.Should().BeEquivalentTo(Role.Assistant);
            reply.Content.Should().Contain("4");

            reply = await assistant.SendMessageAsync("I need to solve the equation 3x + 11 = 14. Can you help me?", ct: default);

            this.output.WriteLine(reply.FormatMessage());

            // remove assistant
            await client.RemoveAssistantAsync(assistant.ID!);
        }

        [ApiKeyFact]
        public async Task AssistantAgentWithCodeInterpreterTestAsync()
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NullReferenceException("OPENAI_API_KEY is null");
            var client = new OpenAIClient(apiKey);
            var assistant = await OpenAIAssistantAgent.CreateAsync(
                client: client,
                name: "test",
                roleInformation: "You are a helpful AI assistant",
                useCodeInterpreter: true);

            var reply = await assistant.SendMessageAsync("what's the 13th of fibonacci?", ct: default);
            reply.From.Should().Be(assistant.Name);
            reply.Role.Should().BeEquivalentTo(Role.Assistant);
            reply.Content.Should().Contain("144");

            // remove assistant
            await client.RemoveAssistantAsync(assistant.ID!);
        }

        /// <summary>
        /// Guess integer number between 1 and 10
        /// </summary>
        /// <param name="number">number to guess</param>
        [FunctionAttribution]
        public async Task<string> GuessNumber(int number)
        {
            if (number < 5)
            {
                return "Too small";
            }
            else if (number > 5)
            {
                return "Too big";
            }
            else
            {
                return "Correct";
            }
        }

        [ApiKeyFact]
        public async Task AssistantAgentFunctionCallTestAsync()
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new NullReferenceException("OPENAI_API_KEY is null");
            var client = new OpenAIClient(apiKey);

            var assistant = await OpenAIAssistantAgent.CreateAsync(
                client: client,
                name: "test",
                roleInformation: "You are a helpful AI assistant",
                functionMaps: new Dictionary<FunctionDefinition, Func<string, Task<string>>>
                {
                    { GuessNumberFunction, GuessNumberWrapper },
                });

            var reply = await assistant.SendMessageAsync("Guess a number between 1 and 10, and tell me the number once you guess it", ct: default);

            reply.From.Should().Be(assistant.Name);
            reply.Role.Should().BeEquivalentTo(Role.Assistant);
            reply.Content.Should().Contain("5");

            // remove assistant
            await client.RemoveAssistantAsync(assistant.ID!);
        }
    }
}
