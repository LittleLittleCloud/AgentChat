using AgentChat.Core.Tests;
using FluentAssertions;
using Xunit;

namespace AgentChat.OpenAI.Tests;

/// <summary>
/// 
/// </summary>
public partial class GPTAgentTest
{
    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [Trait("Category", "openai")]
    [ApiKeyFact("AZURE_OPENAI_GPT35_INSTRUCT_API_KEY")]
    public async Task GPTInstructAgentTest()
    {
        var gpt35Instruct = GPTInstruct.CreateFromAzureOpenAI(
            "gpt-35-turbo-instruct",
            Environment.GetEnvironmentVariable("AZURE_OPENAI_GPT35_INSTRUCT_API_KEY") ?? throw new InvalidOperationException(),
            Environment.GetEnvironmentVariable("AZURE_OPENAI_GPT35_INSTRUCT_ENDPOINT") ?? throw new InvalidOperationException(),
            0,
            128);

        var agent = gpt35Instruct.CreateAgent(
            "Alice",
            @"Your name is Alice.
When you are asked to create a math question, you create a pre-school math question. Remember to start your reply with [MATH_QUESTION]
e.g.: [MATH_QUESTION]: // math question",
            0);

        var reply = await agent.SendMessageAsync("what's your name");
        reply.From.Should().Be("Alice");
        reply.Content.Should().Contain("Alice");
        reply.Role.Should().Be(Role.Assistant);

        reply = await agent.SendMessageAsync("create a math question");
        reply.From.Should().Be("Alice");
        reply.Content.Should().Contain("[MATH_QUESTION]");
    }
}