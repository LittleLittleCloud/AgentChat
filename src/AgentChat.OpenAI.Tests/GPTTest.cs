using AgentChat.Core.Tests;
using Azure.AI.OpenAI;
using FluentAssertions;
using Xunit;

namespace AgentChat.OpenAI.Tests;

/// <summary>
/// 
/// </summary>
public partial class GPTTest
{
    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [Trait("Category", "openai")]
    [ApiKeyFact("AZURE_OPENAI_API_KEY")]
    public async Task GPTEnd2EndTestAsync()
    {
        var gpt35 = GPT.CreateFromAzureOpenAI(
            "gpt-35-turbo-16k",
            Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? throw new InvalidOperationException(),
            Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException(),
            0,
            128);

        var systemMessage = new Message(Role.System, "You return the next most possible number based on previous input");
        var userMessage = new Message(Role.User, "1");
        var assistantMessage = new Message(Role.Assistant, "2");
        var userMessage2 = new Message(Role.User, "3");
        var assistantMessage2 = new Message(Role.Assistant, "4");
        var userMessage3 = new Message(Role.User, "5");

        var replyMessage = await gpt35.GetChatCompletionsAsync(
            new[]
            {
                systemMessage,
                userMessage,
                assistantMessage,
                userMessage2,
                assistantMessage2,
                userMessage3
            });

        replyMessage.Message?.Role.Should().Be(Role.Assistant);
        replyMessage.Message?.Content.Should().Be("6");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [Trait("Category", "openai")]
    [ApiKeyFact("AZURE_OPENAI_API_KEY")]
    public async Task GPTFunctionCallTestAsync()
    {
        var gpt35 = GPT.CreateFromAzureOpenAI(
            "gpt-35-turbo-16k",
            Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? throw new InvalidOperationException(),
            Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException(),
            0,
            128,
            functionDefinitions: new[]
            {
                SayHiFunction
            });

        var systemMessage = new Message(Role.System, "You call sayHi function");
        var userMessage = new Message(Role.User, "Hi", "Alice");

        var replyMessage = await gpt35.GetChatCompletionsAsync(
            new[]
            {
                systemMessage,
                userMessage
            });

        replyMessage.Message.Should().BeOfType<GPTChatMessage>();

        var gptChatMessage = (GPTChatMessage)replyMessage.Message!;
        gptChatMessage.ChatMessage.Role.Should().Be(ChatRole.Assistant);
        gptChatMessage.ChatMessage.FunctionCall?.Name.Should().Be("SayHi");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [Trait("Category", "openai")]
    [ApiKeyFact("AZURE_OPENAI_GPT35_INSTRUCT_API_KEY")]
    public async Task GPTInstructEnd2EndTestAsync()
    {
        var gpt35Instruct = GPTInstruct.CreateFromAzureOpenAI(
            "gpt-35-turbo-instruct",
            Environment.GetEnvironmentVariable("AZURE_OPENAI_GPT35_INSTRUCT_API_KEY") ?? throw new InvalidOperationException(),
            Environment.GetEnvironmentVariable("AZURE_OPENAI_GPT35_INSTRUCT_ENDPOINT") ?? throw new InvalidOperationException(),
            0,
            128);

        var systemMessage = new Message(Role.System, "You return the next most possible number based on previous input");
        var userMessage = new Message(Role.User, "1");
        var assistantMessage = new Message(Role.Assistant, "2");
        var userMessage2 = new Message(Role.User, "3");
        var assistantMessage2 = new Message(Role.Assistant, "4");
        var userMessage3 = new Message(Role.User, "5");

        var replyMessage = await gpt35Instruct.GetChatCompletionsAsync(
            new[]
            {
                systemMessage,
                userMessage,
                assistantMessage,
                userMessage2,
                assistantMessage2,
                userMessage3
            });

        replyMessage.Message?.Role.Should().Be(Role.Assistant);
        replyMessage.Message?.Content.Should().Be("6");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [FunctionAttribution]
    public async Task<string> SayHi(string name) => $@"[Hi] {name}";
}