using Azure.AI.OpenAI;
using Azure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat.OpenAI
{
    public static class OpenAIClientExtension
    {
        /// <summary>
        /// Create a file for the assistant. <seealso cref="https://platform.openai.com/docs/api-reference/assistants/createAssistantFile"/>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="assistantId"></param>
        /// <param name="fileId"></param>
        /// <returns></returns>
        public static async Task<string> CreateAssistantFileAsync(
            this OpenAIClient client,
            string assistantId,
            string fileId,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/assistants/{assistantId}/files";
            var body = new
            {
                file_id = fileId
            };
            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Post;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }

            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");
            postRequest.Content = RequestContent.Create(body);

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);

            if (reply.Status != 200)
            {
                throw new Exception($"fail to create assistant file {fileId}");
            }

            var responseBody = reply.Content.ToObjectFromJson<JsonDocument>();
            return responseBody.RootElement.GetProperty("id").GetString() ?? throw new Exception("fail to get file id");
        }

        public static async Task<OpenAIAssistantObject> CreateAssistantAsync(
            this OpenAIClient client,
            string name,
            string model,
            bool useCodeInterpreter = false,
            bool useRetrieval = false,
            string? instructions = null,
            string? description = null,
            IEnumerable<FunctionDefinition>? functionDefinitions = null,
            IEnumerable<string>? fileIds = null,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/assistants";
            var tools = new List<object>();
            if (useCodeInterpreter)
            {
                tools.Add(new
                {
                    type = "code_interpreter"
                });
            }

            if (useRetrieval)
            {
                tools.Add(new
                {
                    type = "retrieval"
                });
            }

            if (functionDefinitions is IEnumerable<FunctionDefinition> functions)
            {
                foreach (var function in functions)
                {
                    tools.Add(new
                    {
                        type = "function",
                        function = new
                        {
                            description = function.Description,
                            name = function.Name,
                            parameters = function.Parameters.ToObjectFromJson<object>(),
                        }
                    });
                }
            }
            var body = new
            {
                name = name,
                description = description,
                instructions = instructions,
                model = model,
                file_ids = fileIds ?? new List<string>(),
                tools = tools,
            };
            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Post;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }

            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");
            postRequest.Content = RequestContent.Create(body);

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);

            if (reply.Status != 200)
            {
                throw new Exception($"fail to create assistant {name}, error: {reply.Content}");
            }

            var responseBody = reply.Content.ToObjectFromJson<OpenAIAssistantObject>();
            return responseBody ?? throw new Exception("fail to get assistant id");
        }

        public static async Task<OpenAIAssistantObject> RetrieveAssistantAsync(
            this OpenAIClient client,
            string assistantId,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/assistants/{assistantId}";
            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Get;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }
            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);

            if (reply.Status != 200)
            {
                throw new Exception($"fail to get assistant {assistantId}");
            }

            var responseBody = reply.Content.ToObjectFromJson<OpenAIAssistantObject>();

            return responseBody ?? throw new Exception("fail to get assistant id");
        }

        public static async Task<string> RemoveAssistantAsync(
            this OpenAIClient client,
            string assistantId,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/assistants/{assistantId}";
            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Delete;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }

            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);
            if (reply.Status != 200)
            {
                throw new Exception($"fail to remove assistant {assistantId}");
            }

            var responseBody = reply.Content.ToObjectFromJson<JsonDocument>();
            var deleted = responseBody.RootElement.GetProperty("deleted").GetBoolean();
            if (deleted)
            {
                return assistantId;
            }
            else
            {
                throw new Exception($"fail to remove assistant {assistantId}");
            }
        }

        public static async Task<OpenAIThreadObject> CreateThreadAsync(
            this OpenAIClient client,
            IEnumerable<OpenAIThreadMessageContentObject>? messages = null,
            Dictionary<string, string>? metaData = null,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/threads";
            var body = new
            {
                messages = messages ?? new List<OpenAIThreadMessageContentObject>(),
                metadata = metaData,
            };
            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Post;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }

            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");
            postRequest.Content = RequestContent.Create(body);

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);

            if (reply.Status != 200)
            {
                throw new Exception($"fail to create thread");
            }

            var responseBody = reply.Content.ToObjectFromJson<OpenAIThreadObject>();

            return responseBody ?? throw new Exception("fail to get thread id");
        }

        public static async Task<OpenAIThreadObject> RetrieveThreadAsync(
            this OpenAIClient client,
            string threadId,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/threads/{threadId}";
            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Get;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }

            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);

            if (reply.Status != 200)
            {
                throw new Exception($"fail to get thread {threadId}");
            }

            var responseBody = reply.Content.ToObjectFromJson<OpenAIThreadObject>();

            return responseBody ?? throw new Exception("fail to get thread id");
        }

        public static async Task<string> DeleteThreadAsync(
            this OpenAIClient client,
            string threadId,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/threads/{threadId}";
            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Delete;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }

            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);
            if (reply.Status != 200)
            {
                throw new Exception($"fail to remove thread {threadId}");
            }

            var responseBody = reply.Content.ToObjectFromJson<JsonDocument>();
            var deleted = responseBody.RootElement.GetProperty("deleted").GetBoolean();
            if (deleted)
            {
                return threadId;
            }
            else
            {
                throw new Exception($"fail to remove thread {threadId}");
            }
        }

        public static async Task<OpenAIThreadMessageObject> CreateMessageAsync(
            this OpenAIClient client,
            IChatMessage message,
            string threadId,
            Dictionary<string, string>? metaData = null,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/threads/{threadId}/messages";
            var body = new
            {
                role = message.Role.ToString(),
                content = message.Content,
                metadata = metaData,
            };

            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Post;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }

            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");
            postRequest.Content = RequestContent.Create(body);

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);
            if (reply.Status != 200)
            {
                throw new Exception($"fail to create message");
            }

            var responseBody = reply.Content.ToObjectFromJson<OpenAIThreadMessageObject>();
            return responseBody ?? throw new Exception("fail to get message id");
        }

        public static async Task<OpenAIThreadMessageObject> RetrieveMessageAsync(
            this OpenAIClient client,
            string threadId,
            string messageId,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/threads/{threadId}/messages/{messageId}";
            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Get;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }

            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);

            if (reply.Status != 200)
            {
                throw new Exception($"fail to get message {messageId}");
            }

            var responseBody = reply.Content.ToObjectFromJson<OpenAIThreadMessageObject>();

            return responseBody ?? throw new Exception("fail to get message id");
        }

        public static async Task<IEnumerable<OpenAIThreadMessageObject>> ListMessagesAsync(
            this OpenAIClient client,
            string threadId,
            int limit = 20,
            string order = "desc",
            string? after = null,
            string? before = null,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/threads/{threadId}/messages";
            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Get;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }

            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");
            postRequest.Uri.AppendQuery("limit", limit.ToString());
            postRequest.Uri.AppendQuery("order", order);
            if (after is string afterString)
            {
                postRequest.Uri.AppendQuery("after", afterString);
            }

            if (before is string beforeString)
            {
                postRequest.Uri.AppendQuery("before", beforeString);
            }


            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);
            if (reply.Status != 200)
            {
                throw new Exception($"fail to list messages");
            }
            var content = reply.Content.ToObjectFromJson<JsonDocument>();
            var data = content.RootElement.GetProperty("data");
            var messages = new List<OpenAIThreadMessageObject>();
            foreach (var message in data.EnumerateArray())
            {
                messages.Add(message.Deserialize<OpenAIThreadMessageObject>()!);
            }

            return messages ?? throw new Exception("fail to get messages");
        }

        public static async Task<OpenAIThreadRunObject> CreateRunAsync(
            this OpenAIClient client,
            string threadId,
            string assistantId,
            string? instructions = null,
            Dictionary<string, string>? metaData = null,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/threads/{threadId}/runs";

            var body = new
            {
                assistant_id = assistantId,
                instructions = instructions,
                metadata = metaData,
            };

            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Post;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }

            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");
            postRequest.Content = RequestContent.Create(body);

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);

            if (reply.Status != 200)
            {
                throw new Exception($"fail to create run");
            }

            var responseBody = reply.Content.ToObjectFromJson<OpenAIThreadRunObject>();

            return responseBody ?? throw new Exception("fail to get run id");
        }

        public static async Task<OpenAIThreadRunObject> RetrieveRunAsync(
            this OpenAIClient client,
            string threadId,
            string runId,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/threads/{threadId}/runs/{runId}";
            var postRequest = client.Pipeline.CreateRequest();
            postRequest.Method = RequestMethod.Get;
            postRequest.Uri.Reset(new Uri(baseUrl));
            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }
            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);

            if (reply.Status != 200)
            {
                throw new Exception($"fail to get run {runId}");
            }

            var responseBody = reply.Content.ToObjectFromJson<OpenAIThreadRunObject>();

            return responseBody ?? throw new Exception("fail to get run id");
        }

        public static async Task<OpenAIThreadRunObject> SubmitToolOutputsAsync(
            this OpenAIClient client,
            string threadId,
            string runId,
            Dictionary<string, string> toolOutputs,
            CancellationToken? ct = null)
        {
            var baseUrl = $"https://api.openai.com/v1/threads/{threadId}/runs/{runId}/submit_tool_outputs";

            var body = new
            {
                tool_outputs = toolOutputs.Select(
                    kv =>
                    {
                        return new
                        {
                            tool_call_id = kv.Key,
                            output = kv.Value,
                        };
                    }),
            };

            var postRequest = client.Pipeline.CreateRequest();

            postRequest.Method = RequestMethod.Post;
            postRequest.Uri.Reset(new Uri(baseUrl));

            // remove all header
            foreach (var header in postRequest.Headers)
            {
                postRequest.Headers.Remove(header.Name);
            }

            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("OpenAI-Beta", "assistants=v1");
            postRequest.Content = RequestContent.Create(body);

            var reply = await client.Pipeline.SendRequestAsync(postRequest, ct ?? CancellationToken.None);

            if (reply.Status != 200)
            {
                throw new Exception($"fail to submit tool outputs");
            }

            var responseBody = reply.Content.ToObjectFromJson<OpenAIThreadRunObject>();

            return responseBody ?? throw new Exception("fail to get run id");
        }
    }
}
