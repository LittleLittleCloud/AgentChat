﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;

namespace AgentChat.OpenAI;

public partial class GPTAgent : IAgent
{
    private readonly GPT _gpt;

    private readonly float _temperature = 0f;

    public Dictionary<FunctionDefinition, Func<string, Task<string>>> FunctionMaps { get; }

    public string RoleInformation { get; }

    public GPTAgent(
        GPT gpt,
        string name,
        string roleInformation,
        Dictionary<FunctionDefinition, Func<string, Task<string>>>? functionMaps = null)
    {
        _gpt = gpt;
        Name = name;
        RoleInformation = roleInformation;
        FunctionMaps = functionMaps ?? new Dictionary<FunctionDefinition, Func<string, Task<string>>>();
    }

    public string Name { get; }

    public async Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default)
    {
        var chatMessages = this.ProcessChatMessages(conversation)
            .Select(chatMessage => new GPTChatMessage(chatMessage))
            .ToList();
        var chatMessage = (await StepCallAsync(chatMessages, ct)).ChatMessage;

        // if chatMessage is function call, call that function
        if (chatMessage.FunctionCall is FunctionCall function)
        {
            if (FunctionMaps?.FirstOrDefault(kv => kv.Key.Name == function.Name).Value is Func<string, Task<string>> func)
            {
                var parameters = function.Arguments;

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
                var availableFunctions = FunctionMaps?.Select(kv => kv.Key.Name)?.ToList() ?? new List<string>();

                var unknownFunctionMessage =
                    $"Unknown function: {function.Name}. Available functions: {string.Join(",", availableFunctions)}";
                chatMessage.Content = unknownFunctionMessage;
                chatMessage.FunctionCall = null;
            }

            return new GPTChatMessage(chatMessage)
            {
                From = Name
            };
        }

        return new GPTChatMessage(chatMessage)
        {
            From = Name
        };
    }

    public IEnumerable<GPTChatMessage> CreateSystemMessages()
    {
        var systemMessage = new ChatMessage(ChatRole.System, $"Your name is {Name}, {RoleInformation}");

        return new GPTChatMessage[]
        {
            new(systemMessage)
        };
    }

    public async Task<GPTChatMessage> StepCallAsync(IEnumerable<GPTChatMessage> conversation, CancellationToken ct = default)
    {
        var systemMessages = CreateSystemMessages();
        var messages = systemMessages.Concat(conversation).ToList();

        var result = await _gpt.GetChatCompletionsAsync(
            messages,
            _temperature,
            stopWords: new[] { "<eof_msg>" },
            ct: ct);

        return result.Message as GPTChatMessage ?? throw new Exception("result is not GPTChatMessage");
    }
}