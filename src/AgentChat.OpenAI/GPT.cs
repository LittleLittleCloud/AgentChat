using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AgentChat.IChatLLM;

namespace AgentChat.OpenAI
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
                    _ => new ChatMessage(msg.Role.ToChatRole(), msg.Content),
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
                var chatCompletion = new ChatCompletion
                {
                    Message = new GPTChatMessage(completion.Message),
                    PromptTokens = completions.Usage.PromptTokens,
                    TotalTokens = completions.Usage.TotalTokens,
                    CompletionTokens = completions.Usage.CompletionTokens,
                };

                return chatCompletion;
            }
            else
            {
                throw new Exception("response is not ChatCompletions");
            }
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

        public Role Role
        {
            get => this.msg.Role.ToRole();
            set => this.msg.Role = value.ToChatRole();
        }

        public string? Content
        {
            get => this.msg.Content;
            set => this.msg.Content = value;
        }

        public AgentFunction? Function
        {
            get
            {
                if (this.msg.FunctionCall?.Name is string functionName && this.msg.FunctionCall?.Arguments is string arguments)
                {
                    return new AgentFunction
                    {
                        Name = functionName,
                        Arguments = arguments,
                    };
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value is AgentFunction function)
                {
                    this.msg.FunctionCall = new FunctionCall(function.Name, function.Arguments);
                }
                else
                {
                    this.msg.FunctionCall = null;
                }
            }
        }

        public FunctionCall? FunctionCall => this.msg.FunctionCall;

        public string? Name => this.msg.Name;
    }
}
