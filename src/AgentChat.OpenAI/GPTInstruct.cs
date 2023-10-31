using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static AgentChat.IChatLLM;

namespace AgentChat.OpenAI
{
    public class GPTInstruct : IChatLLM
    {
        private OpenAIClient _client;
        private readonly string _model;
        private readonly float _temperature;
        private readonly int _maxToken;
        private readonly string[] _stopWords;

        public GPTInstruct(
            OpenAIClient client,
            string model,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null)
        {
            this._client = client;
            this._model = model;
            this._temperature = temperature ?? 0f;
            this._maxToken = maxToken ?? 1024;
            this._stopWords = stopWords ?? Array.Empty<string>();
        }

        public GPTInstruct(
            GPTInstruct other,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null)
            :this(
                 other._client,
                 other._model,
                 temperature ?? other._temperature,
                 maxToken ?? other._maxToken,
                 stopWords ?? other._stopWords)
        {
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

        public static GPTInstruct CreateFromAzureOpenAI(
            string deployModel,
            string apiKey,
            string endPoint,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null)
        {
            var client = new OpenAIClient(new Uri(endPoint), new Azure.AzureKeyCredential(apiKey));

            return new GPTInstruct(client, deployModel, temperature, maxToken, stopWords);
        }

        public async Task<IChatLLM.ChatCompletion> GetChatCompletionsAsync(
            IEnumerable<IChatMessage> messages,
            float? temperature = null,
            int? maxToken = null,
            string[]? stopWords = null,
            CancellationToken? ct = null)
        {
            var prompts = messages.Select(m => m.ToChatML()).ToList();
            prompts.Add($"<|im_start|>{ChatRole.Assistant}{Environment.NewLine}");
            var prompt = string.Join(Environment.NewLine, prompts);
            var completionOption = new CompletionsOptions(new[] { prompt } )
            {
                Temperature = temperature ?? _temperature,
                MaxTokens = maxToken ?? _maxToken,
                Echo = false,
            };
            stopWords = stopWords ?? _stopWords;
            
            foreach(var stopWord in stopWords.Concat(new[] {"<|im_end|>"}))
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
                    CompletionTokens = completions.Usage.CompletionTokens,
                };

                return chatCompletion;
            }
            else
            {
                throw new Exception("Invalid response");
            }
        }
    }
}
