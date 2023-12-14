using AgentChat.Core.Tests;
using Azure.AI.OpenAI;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AgentChat.OpenAI.Tests
{
    public class OpenAIExtensionTests
    {
        [Trait("Category", "openai")]
        [ApiKeyFact]
        public async Task UploadAssistantFileTestAsync()
        {
            var assistantId = "asst_gdFLde3S7BcAh4yybpMjVf4S";
            var file = "file-1ygJet3TxyyJ19zHEwCwH4M4";
            var client = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);
            var reply = await client.CreateAssistantFileAsync(assistantId, file);

            reply.Should().Be(file);
        }

        [Trait("Category", "openai")]
        [ApiKeyFact]
        public async Task CreateAssistantTestAsync()
        {
            var client = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);
            var assistantName = "test-assistant";
            var assistant = await client.CreateAssistantAsync(assistantName, "gpt-3.5-turbo");

            assistant.Should().NotBeNull();

            // remove assistant
            var assistantId2 = await client.RemoveAssistantAsync(assistant.Id!);
            assistantId2.Should().BeEquivalentTo(assistant.Id);
        }

        [Trait("Category", "openai")]
        [ApiKeyFact]
        public async Task CreateThreadTestAsync()
        {
            var client = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);
            var thread = await client.CreateThreadAsync();
            thread.Should().NotBeNull();
        }
    }
}
