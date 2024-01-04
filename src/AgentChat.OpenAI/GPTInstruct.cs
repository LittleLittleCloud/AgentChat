using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using static AgentChat.IChatLLM;

namespace AgentChat.OpenAI;

public class GPTInstruct : IChatLLM
{
    private readonly int _maxToken;

    private readonly string _model;

    private readonly string[] _stopWords;

    private readonly float _temperature;

    private readonly OpenAIClient _client;

    public GPTInstruct(
        OpenAIClient client,
        string model,
        float? temperature = null,
        int? maxToken = null,
        string[]? stopWords = null)
    {
        _client = client;
        _model = model;
        _temperature = temperature ?? 0f;
        _maxToken = maxToken ?? 1024;
        _stopWords = stopWords ?? Array.Empty<string>();
    }

    public GPTInstruct(
        GPTInstruct other,
        float? temperature = null,
        int? maxToken = null,
        string[]? stopWords = null)
        : this(
            other._client,
            other._model,
            temperature ?? other._temperature,
            maxToken ?? other._maxToken,
            stopWords ?? other._stopWords)
    {
    }

    public async Task<ChatCompletion> GetChatCompletionsAsync(
        IEnumerable<IChatMessage> messages,
        float? temperature = null,
        int? maxToken = null,
        string[]? stopWords = null,
        CancellationToken? ct = null)
    {
        var prompts = messages.Select(m => m.ToChatML()).ToList();
        prompts.Add($"<|im_start|>{ChatRole.Assistant}{Environment.NewLine}");
        var prompt = string.Join(Environment.NewLine, prompts);

        var completionOption = new CompletionsOptions(new[] { prompt })
        {
            Temperature = temperature ?? _temperature,
            MaxTokens = maxToken ?? _maxToken,
            Echo = false
        };
        stopWords = stopWords ?? _stopWords;

        foreach (var stopWord in stopWords.Concat(new[] { "<|im_end|>" }))
        {
            completionOption.StopSequences.Add(stopWord);
        }

        var response = await _client.GetCompletionsAsync(_model, completionOption, ct ?? CancellationToken.None);

        if (response.Value is Completions completions)
        {
            var completion = completions.Choices.First().Text;
            var chatMessage = new Message(Role.Assistant, completion);

            var chatCompletion = new ChatCompletion
            {
                Message = chatMessage,
                PromptTokens = completions.Usage.PromptTokens,
                TotalTokens = completions.Usage.TotalTokens,
                CompletionTokens = completions.Usage.CompletionTokens
            };

            return chatCompletion;
        }

        throw new Exception("Invalid response");
    }

    public static GPTInstruct CreateFromAzureOpenAI(
        string deployModel,
        string apiKey,
        string endPoint,
        float? temperature = null,
        int? maxToken = null,
        string[]? stopWords = null)
    {
        var client = new OpenAIClient(new Uri(endPoint), new AzureKeyCredential(apiKey));

        return new GPTInstruct(client, deployModel, temperature, maxToken, stopWords);
    }

    public static GPTInstruct CreateFromOpenAI(
        string apiKey,
        string model,
        float? temperature = null,
        int? maxToken = null,
        string[]? stopWords = null)
    {
        var client = new OpenAIClient(apiKey);
        return new GPTInstruct(client, model, temperature, maxToken, stopWords);
    }
}