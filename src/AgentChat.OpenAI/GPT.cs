using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using static AgentChat.IChatLLM;

namespace AgentChat.OpenAI;

public class GPT : IChatLLM
{
    private readonly int _maxToken;

    private readonly string _model;

    private readonly string[] _stopWords;

    private readonly float _temperature;

    private readonly FunctionDefinition[] functionDefinitions;

    private readonly OpenAIClient _client;

    public GPT(
        OpenAIClient client,
        string model,
        float? temperature = null,
        int? maxToken = null,
        string[]? stopWords = null,
        FunctionDefinition[]? functionDefinitions = null)
    {
        _client = client;
        _model = model;
        _temperature = temperature ?? 0f;
        _maxToken = maxToken ?? 1024;
        _stopWords = stopWords ?? Array.Empty<string>();
        this.functionDefinitions = functionDefinitions ?? Array.Empty<FunctionDefinition>();
    }

    public GPT(
        GPT other,
        float? temperature = null,
        int? maxToken = null,
        string[]? stopWords = null,
        FunctionDefinition[]? functionDefinitions = null)
    {
        _client = other._client;
        _model = other._model;
        _temperature = temperature ?? other._temperature;
        _maxToken = maxToken ?? other._maxToken;
        _stopWords = stopWords ?? other._stopWords;
        this.functionDefinitions = functionDefinitions ?? other.functionDefinitions;
    }

    public async Task<ChatCompletion> GetChatCompletionsAsync(
        IEnumerable<IChatMessage> messages,
        float? temperature = null,
        int? maxToken = null,
        string[]? stopWords = null,
        CancellationToken? ct = null)
    {
        var chatMessages = messages.Select(msg =>
        {
            return msg switch
            {
                GPTChatMessage gptMsg => gptMsg.ChatMessage,
                ChatMessage chatMessage => chatMessage,
                _ => new ChatMessage(msg.Role.ToChatRole(), msg.Content)
            };
        });

        var options = new ChatCompletionsOptions(chatMessages)
        {
            Temperature = temperature ?? _temperature,
            MaxTokens = maxToken ?? _maxToken,
            Functions = functionDefinitions
        };

        foreach (var stopWord in stopWords ?? _stopWords)
        {
            options.StopSequences.Add(stopWord);
        }

        var response = await _client.GetChatCompletionsAsync(_model, options, ct ?? CancellationToken.None);

        if (response.Value is ChatCompletions completions)
        {
            var completion = completions.Choices.First();

            var chatCompletion = new ChatCompletion
            {
                Message = new GPTChatMessage(completion.Message),
                PromptTokens = completions.Usage.PromptTokens,
                TotalTokens = completions.Usage.TotalTokens,
                CompletionTokens = completions.Usage.CompletionTokens
            };

            return chatCompletion;
        }

        throw new Exception("response is not ChatCompletions");
    }

    public static GPT CreateFromAzureOpenAI(
        string deployModel,
        string apiKey,
        string endPoint,
        float? temperature = null,
        int? maxToken = null,
        string[]? stopWords = null,
        FunctionDefinition[]? functionDefinitions = null)
    {
        var client = new OpenAIClient(new Uri(endPoint), new AzureKeyCredential(apiKey));

        return new GPT(client, deployModel, temperature, maxToken, stopWords, functionDefinitions);
    }

    public static GPT CreateFromOpenAI(
        string apiKey,
        string model,
        float? temperature = null,
        int? maxToken = null,
        string[]? stopWords = null,
        FunctionDefinition[]? functionDefinitions = null)
    {
        var client = new OpenAIClient(apiKey);
        return new GPT(client, model, temperature, maxToken, stopWords, functionDefinitions);
    }
}

public class GPTChatMessage : IChatMessage
{
    public FunctionCall? FunctionCall => ChatMessage.FunctionCall;

    public string? Name => ChatMessage.Name;

    public ChatMessage ChatMessage { get; }

    public GPTChatMessage(ChatMessage msg)
    {
        this.ChatMessage = msg;
    }

    public string? From { get; set; }

    public Role Role
    {
        get => ChatMessage.Role.ToRole();
        set => ChatMessage.Role = value.ToChatRole();
    }

    public string? Content
    {
        get => ChatMessage.Content;
        set => ChatMessage.Content = value;
    }

    public AgentFunction? Function
    {
        get
        {
            if (ChatMessage.FunctionCall?.Name is string functionName && ChatMessage.FunctionCall?.Arguments is string arguments)
            {
                return new AgentFunction
                {
                    Name = functionName,
                    Arguments = arguments
                };
            }

            return null;
        }
        set
        {
            if (value is AgentFunction function)
            {
                ChatMessage.FunctionCall = new FunctionCall(function.Name, function.Arguments);
            }
            else
            {
                ChatMessage.FunctionCall = null;
            }
        }
    }
}