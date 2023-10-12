using Azure.AI.OpenAI;
using GroupChatExample.DotnetInteractiveService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GroupChatExample.Helper
{
    public partial class ChatAgent : IDisposable, IAgent
    {
        private readonly string _model;
        private readonly string _name;
        private readonly string _roleInformation;
        private OpenAIClient _client;
        private readonly Dictionary<FunctionDefinition, Func<string, Task<string>>> _functionMaps;

        public ChatAgent(
            OpenAIClient client,
            string model,
            string name,
            string roleInformation,
            Dictionary<FunctionDefinition, Func<string, Task<string>>>? functionMaps = null)
        {
            this._model = model;
            this._client = client;
            _name = name;
            _roleInformation = roleInformation;
            _functionMaps = functionMaps ?? new Dictionary<FunctionDefinition, Func<string, Task<string>>>();
        }


        public string Name { get => _name; }

        public string RoleInformation { get => _roleInformation; }

        public Dictionary<FunctionDefinition, Func<string, Task<string>>> FunctionMaps { get => _functionMaps; }

        public IEnumerable<ChatMessage> CreateSystemMessages()
        {
            var systemMessage = new ChatMessage(ChatRole.System, $"You are {this.Name}, {this._roleInformation}");

            return new ChatMessage[]
            {
                systemMessage,
            };
        }

        public async Task<ChatMessage> CallAsync(IEnumerable<ChatMessage> conversation, CancellationToken ct = default)
        {
            var chatMessage = await StepCallAsync(conversation, ct);

            // if chatMessage is function call, call that function
            if (chatMessage.FunctionCall is FunctionCall function)
            {
                ChatMessage? functionResultMessage = null;
                if (_functionMaps?.FirstOrDefault(kv => kv.Key.Name == function.Name).Value is Func<string, Task<string>> func)
                {
                    var parameters = function.Arguments;
                    Console.WriteLine($"function name: {function.Name}");
                    Console.WriteLine($"raw function call: {function.Arguments}");
                    try
                    {
                        var functionResult = await func(parameters);
                        functionResultMessage = new ChatMessage(ChatRole.Function, functionResult);
                        functionResultMessage.Name = function.Name;

                    }
                    catch (Exception e)
                    {
                        functionResultMessage = new ChatMessage(ChatRole.Function, $"Error: {e.Message}");
                        functionResultMessage.Name = function.Name;
                    }
                }
                else
                {
                    var availableFunctions = _functionMaps?.Select(kv => kv.Key.Name)?.ToList() ?? new List<string>();
                    functionResultMessage = new ChatMessage(ChatRole.User, $"Unknown function: {function.Name}. Available functions: {string.Join(",", availableFunctions)}");
                }

                //return await CallAsync(conversation.Concat(new ChatMessage[] { chatMessage, functionResultMessage }), ct);

                return functionResultMessage;
            }
            else
            {
                return chatMessage;
            }
        }

        public async Task<ChatMessage> StepCallAsync(IEnumerable<ChatMessage> conversation, CancellationToken ct = default)
        {
            var systemMessages = CreateSystemMessages();
            var messages = systemMessages.Concat(conversation).ToList();

            var option = new ChatCompletionsOptions()
            {
                Temperature = 0.7f,
                MaxTokens = 1024,
                Functions = _functionMaps?.Select(kv => kv.Key)?.ToList() ?? new List<FunctionDefinition>(),
            };

            option.StopSequences.Add("<eof_name>:");

            foreach (var message in messages)
            {
                option.Messages.Add(message);
            }

            var result = await _client.GetChatCompletionsWithRetryAsync(this._model, option, ct);

            if (result.Value.Choices.Last().FinishReason == CompletionsFinishReason.Stopped)
            {
                var message = result.Value.Choices.Last().Message;
                if (message.Content.StartsWith("From") && message.Content.Split(' ').Length == 2)
                {
                    message.Content += "<eof_name>:";
                }
            }

            return result.Value.Choices.Last().Message;
        }

        public void Dispose()
        {
        }
    }
}
