using AgentChat.Core.Tests;
using Azure.AI.OpenAI;
using FluentAssertions;
using Xunit;

namespace AgentChat.OpenAI.Tests;

/// <summary>
/// 
/// </summary>
public class OpenAIExtensionTests
{
    /// <summary>
    /// 
    /// </summary>
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

    /// <summary>
    /// 
    /// </summary>
    [Trait("Category", "openai")]
    [ApiKeyFact]
    public async Task CreateThreadTestAsync()
    {
        var client = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);
        var thread = await client.CreateThreadAsync();
        thread.Should().NotBeNull();
    }

    /// <summary>
    /// 
    /// </summary>
    [Trait("Category", "openai")]
    [ApiKeyFact]
    public async Task UploadAssistantFileTestAsync()
    {
        var client = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);
        var assistantName = "test-assistant";
        var assistant = await client.CreateAssistantAsync(assistantName, "gpt-3.5-turbo");
        assistant.Should().NotBeNull();

        if (assistant.Id == null)
        {
            throw new Exception("Assistant has a null id");
        }

        var file = "file-1ygJet3TxyyJ19zHEwCwH4M4";

        try
        {
            var reply = await client.CreateAssistantFileAsync(assistant.Id, file);

            reply.Should().Be(file);
        }
        finally
        {
            // remove assistant
            var assistantId2 = await client.RemoveAssistantAsync(assistant.Id!);
            assistantId2.Should().BeEquivalentTo(assistant.Id);
        }
    }
}