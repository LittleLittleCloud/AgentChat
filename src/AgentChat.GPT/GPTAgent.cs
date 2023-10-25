using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    public partial class GPTAgent : IAgent
    {
        private readonly GPT _gpt;
        private readonly string _name;
        private readonly string _roleInformation;
        private readonly Dictionary<FunctionDefinition, Func<string, Task<string>>> _functionMaps;
        private readonly float _temperature = 0f;

        public GPTAgent(
            GPT gpt,
            string name,
            string roleInformation,
            Dictionary<FunctionDefinition, Func<string, Task<string>>>? functionMaps = null)
        {
            _gpt = gpt;
            _name = name;
            _roleInformation = roleInformation;
            _functionMaps = functionMaps ?? new Dictionary<FunctionDefinition, Func<string, Task<string>>>();
        }


        public string Name { get => _name; }

        public string RoleInformation { get => _roleInformation; }

        public Dictionary<FunctionDefinition, Func<string, Task<string>>> FunctionMaps { get => _functionMaps; }

        public IEnumerable<GPTChatMessage> CreateSystemMessages()
        {
            var systemMessage = new ChatMessage(ChatRole.System, $"Your name is {this.Name}, {this._roleInformation}");

            return new GPTChatMessage[]
            {
                new GPTChatMessage(systemMessage),
            };
        }

        public async Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default)
        {
            var chatMessages = this.ProcessChatMessages(conversation)
                    .Select(chatMessage => new GPTChatMessage(chatMessage))
                    .ToList();
            var chatMessage = (await StepCallAsync(chatMessages, ct)).ChatMessage;

            // if chatMessage is function call, call that function
            if (chatMessage.FunctionCall is FunctionCall function)
            {
                if (_functionMaps?.FirstOrDefault(kv => kv.Key.Name == function.Name).Value is Func<string, Task<string>> func)
                {
                    var parameters = function.Arguments;
                    Console.WriteLine($"function name: {function.Name}");
                    Console.WriteLine($"raw function call: {function.Arguments}");
                    try
                    {
                        var functionResult = await func(parameters);
                        chatMessage.Content = functionResult;
                        chatMessage.Name = function.Name;

                    }
                    catch (Exception e)
                    {
                        var errorMessage = $"Error: {e.Message}";
                        chatMessage.Content = errorMessage;
                        chatMessage.Name = function.Name;
                    }
                }
                else
                {
                    var availableFunctions = _functionMaps?.Select(kv => kv.Key.Name)?.ToList() ?? new List<string>();
                    var unknownFunctionMessage = $"Unknown function: {function.Name}. Available functions: {string.Join(",", availableFunctions)}";
                    chatMessage.Content = unknownFunctionMessage;
                    chatMessage.FunctionCall = null;
                }

                return new GPTChatMessage(chatMessage)
                {
                    From = this.Name,
                };
            }
            else
            {
                return new GPTChatMessage(chatMessage)
                {
                    From = this.Name,
                };
            }
        }

        public async Task<GPTChatMessage> StepCallAsync(IEnumerable<GPTChatMessage> conversation, CancellationToken ct = default)
        {
            var systemMessages = CreateSystemMessages();
            var messages = systemMessages.Concat(conversation).ToList();

            var result = await _gpt.GetChatCompletionsAsync(
                messages,
                temperature: _temperature,
                stopWords: new[] { "<eof_msg>" },
                ct: ct);

            return result.Message as GPTChatMessage ?? throw new Exception("result is not GPTChatMessage");
        }
    }
}
