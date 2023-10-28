using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    public class GPT : IChatLLM
    {
        private OpenAIClient _client;
        private readonly string _model;
        private readonly float _temperature;
        private readonly int _maxToken;
        private readonly string[] _stopWords;
        private readonly FunctionDefinition[] functionDefinitions;

        public GPT(
            OpenAIClient client,
            string model,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null,
            FunctionDefinition[]? functionDefinitions = null)
        {
            this._client = client;
            this._model = model;
            this._temperature = temperature ?? 0f;
            this._maxToken = maxToken ?? 1024;
            this._stopWords = stopWords ?? Array.Empty<string>();
            this.functionDefinitions = functionDefinitions ?? Array.Empty<FunctionDefinition>();
        }

        public GPT(
            GPT other,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null,
            FunctionDefinition[]? functionDefinitions = null)
        {
            this._client = other._client;
            this._model = other._model;
            this._temperature = temperature ?? other._temperature;
            this._maxToken = maxToken ?? other._maxToken;
            this._stopWords = stopWords ?? other._stopWords;
            this.functionDefinitions = functionDefinitions ?? other.functionDefinitions;
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

        public static GPT CreateFromAzureOpenAI(
            string deployModel,
            string apiKey,
            string endPoint,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null,
            FunctionDefinition[]? functionDefinitions = null)
        {
            var client = new OpenAIClient(new Uri(endPoint), new Azure.AzureKeyCredential(apiKey));

            return new GPT(client, deployModel, temperature, maxToken, stopWords, functionDefinitions);
        }



        public async Task<IChatCompletion> GetChatCompletionsAsync(
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
                    _ => new ChatMessage(msg.Role, msg.Content),
                };
            });
            var options = new ChatCompletionsOptions(chatMessages)
            {
                Temperature = temperature ?? this._temperature,
                MaxTokens = maxToken ?? this._maxToken,
                Functions = this.functionDefinitions,
            };

            foreach(var stopWord in stopWords ?? this._stopWords)
            {
                options.StopSequences.Add(stopWord);
            }

            var response = await this._client.GetChatCompletionsAsync(this._model, options, ct ?? CancellationToken.None);

            if (response.Value is ChatCompletions completions)
            {
                var completion = completions.Choices.First();
                var chatCompletion = new GPTChatCompletion
                {
                    Message = new GPTChatMessage(completion.Message),
                    Usage = completions.Usage,
                };

                return chatCompletion;
            }
            else
            {
                throw new Exception("response is not ChatCompletions");
            }
        }

        public IChatMessage CreateChatMessage(ChatRole role, string? content = null, string? name = null, string? from = null)
        {
            var msg = new ChatMessage(role, content)
            {
                Name = name,
            };

            return new GPTChatMessage(msg)
            {
                From = from,
            };
        }
    }

    public class GPTChatMessage: IChatMessage
    {
        private readonly ChatMessage msg;

        public GPTChatMessage(ChatMessage msg)
        {
            this.msg = msg;
        }

        public ChatMessage ChatMessage => this.msg;

        public string? From { get; set; }

        public ChatRole Role => this.msg.Role;

        public string? Content => this.msg.Content;

        public FunctionCall? FunctionCall => this.msg.FunctionCall;

        public string? Name => this.msg.Name;
    }

    public class GPTChatCompletion : IChatCompletion
    {
        public IChatMessage? Message { get; set; }

        public CompletionsUsage? Usage { get; set; }
    }
}
