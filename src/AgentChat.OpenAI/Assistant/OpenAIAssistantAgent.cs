using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat.OpenAI
{
    /// <summary>
    /// OpenAI assistant agent.
    /// </summary>
    public class OpenAIAssistantAgent : IAgent
    {
        private readonly OpenAIClient _client;
        private readonly OpenAIAssistantObject _assistant;
        private readonly Dictionary<FunctionDefinition, Func<string, Task<string>>> _functionMaps = new Dictionary<FunctionDefinition, Func<string, Task<string>>>();
        
        public OpenAIAssistantAgent(
            OpenAIClient client,
            OpenAIAssistantObject assistant,
            Dictionary<FunctionDefinition, Func<string, Task<string>>>? functionMaps = null)
        {
            this._client = client;
            this._assistant = assistant;
            this._functionMaps = functionMaps ?? new Dictionary<FunctionDefinition, Func<string, Task<string>>>();
        }

        /// <summary>
        /// Create a new OpenAI assistant agent.
        /// </summary>
        /// <param name="name">agent name</param>
        /// <param name="roleInformation">system instructions</param>
        /// <param name="description">assistant description</param>
        /// <param name="model">the llm model to use.</param>
        /// <param name="fileIds">A lsit of file ids attach to assistant</param>
        public static async Task<OpenAIAssistantAgent> CreateAsync(
            OpenAIClient client,
            string name,
            string roleInformation,
            string? description = null,
            string model = "gpt-3.5-turbo",
            bool useCodeInterpreter = false,
            bool useRetrieval = false,
            Dictionary<FunctionDefinition, Func<string, Task<string>>>? functionMaps = null,
            string[]? fileIds = null,
            CancellationToken? ct = default)
        {
            var functionDefinitions = functionMaps?.Keys.ToArray();
            var assistant = await client.CreateAssistantAsync(name, model, useCodeInterpreter, useRetrieval, roleInformation, description, functionDefinitions, fileIds, ct);
            return new OpenAIAssistantAgent(client, assistant, functionMaps);
        }

        public string Name => this._assistant.Name ?? throw new NullReferenceException("Name is null");

        public string ID => this._assistant.Id ?? throw new NullReferenceException("Id is null");

        public async Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default)
        {
            // step 1, create thread
            var thread = await this._client.CreateThreadAsync(ct: ct);

            // step 2, append messages to thread
            var msgs = new List<OpenAIThreadMessageObject>();
            foreach (var message in conversation)
            {
                var msg = await this._client.CreateMessageAsync(message, thread.Id!, ct: ct);
                msgs.Add(msg);
            }

            // step 3, create run
            var run = await this._client.CreateRunAsync(thread.Id!, _assistant.Id!, ct: ct);

            // step 4 query status
            var terminateStatus = new[]
            {
                OpenAIAssistantRunStatus.Completed,
                OpenAIAssistantRunStatus.Failed,
                OpenAIAssistantRunStatus.Expired,
                OpenAIAssistantRunStatus.Cancelled,
            };

            while (!terminateStatus.Contains(run.Status))
            {
                await Task.Delay(1000, ct);
                run = await this._client.RetrieveRunAsync(thread.Id!, run.Id!, ct: ct);

                // process Completed
                if (run.Status == OpenAIAssistantRunStatus.Completed)
                {
                    var newMessages = await this._client.ListMessagesAsync(
                        thread.Id!,
                        before: msgs.LastOrDefault()?.Id,
                        ct: ct);
                    var contentBuilder = new StringBuilder();
                    foreach (var msg in newMessages.Reverse())
                    {
                        foreach (var content in msg.Content ?? throw new Exception("content is null"))
                        {
                            if (content is OpenAIThreadTextMessageContentObject text)
                            {
                                contentBuilder.AppendLine(text.Text?.Value);
                            }
                            else
                            {
                                throw new Exception("content other than text is not support yet.");
                            }
                        }
                    }
                    return new Message(Role.Assistant, contentBuilder.ToString(), this.Name);
                }

                // process Failed, cancelled, expired
                if (terminateStatus.Contains(run.Status))
                {
                    throw new Exception($"Fail to produce an output. Run status is {run.Status}");
                }

                // process Queued, Cancelling, in progress
                if (run.Status == OpenAIAssistantRunStatus.Queued ||
                    run.Status == OpenAIAssistantRunStatus.Cancelling ||
                    run.Status == OpenAIAssistantRunStatus.InProgress)
                {
                    // do nothing
                    continue;
                }

                // process RequiresAction
                if (run.Status == OpenAIAssistantRunStatus.RequiresAction)
                {
                    // TODO
                    // handle action
                    var toolCalls = run.RequiredAction?.SubmitToolOutputs?.ToolCalls ?? throw new Exception("ToolCalls is null");

                    var toolCallResult = new Dictionary<string, string>();
                    foreach (var toolCall in toolCalls)
                    {
                        var functionName = toolCall.Function?.Name ?? throw new Exception("Function name is null");
                        var funcMap = this._functionMaps.ToDictionary(kv => kv.Key.Name, kv => kv.Value);

                        if (funcMap.ContainsKey(functionName))
                        {
                            var result = await funcMap[functionName](toolCall.Function.Arguments!);
                            toolCallResult[toolCall.Id!] = result;
                        }
                        else
                        {
                            var notFoundMessage = $"Function {functionName} is not found in the function map.";
                            var availableFunctions = string.Join(", ", funcMap.Keys);
                            var errorMessage = $"{notFoundMessage} Available functions are {availableFunctions}";
                            toolCallResult[toolCall.Id!] = errorMessage;
                        }
                    }

                    run = await this._client.SubmitToolOutputsAsync(thread.Id!, run.Id!, toolCallResult, ct: ct);
                    continue;
                }

                // process unknown status
                throw new Exception($"Unknown run status {run.Status}");
            }

            throw new Exception("Should not reach here.");
        }
    }
}
