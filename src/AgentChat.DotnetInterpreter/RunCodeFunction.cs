using Azure.AI.OpenAI;
using AgentChat.DotnetInteractiveService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentChat.DotnetInterpreter
{
    public class RunCodeFunction : IDisposable
    {
        private static JsonSerializerOptions serializeOption = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly InteractiveService? dotnetInteractiveService;

        public RunCodeFunction(InteractiveService interactiveService)
        {
            this.dotnetInteractiveService = interactiveService;
        }
        

        public FunctionDefinition FunctionDefinition { get; } = new FunctionDefinition
        {
            Name = @"RunCode",
            Description = "Run dotnet code block and return result. The code block need to be in a single line",
            Parameters = BinaryData.FromObjectAsJson(new
            {
                Type = "object",
                Properties = new
                {
                    code = new
                    {
                        Type = @"string",
                        Description = @"code block to run, in a single line",
                    },
                },
                Required = new[]
        {
            "code",
        },
            }, serializeOption),
        };

        public async Task<string> RunCodeAsync(string args)
        {
            if (this.dotnetInteractiveService == null)
            {
                throw new Exception("InteractiveService is not initialized.");
            }

            string code = string.Empty;

            try
            {
                var kwargs = JsonSerializer.Deserialize<Dictionary<string, string>>(args, serializeOption);
                code = kwargs?["code"] ?? string.Empty;
            }
            catch(Exception ex)
            {
                code = string.Empty;
            }

            var result = await this.dotnetInteractiveService.SubmitCSharpCodeAsync(code, default);
            if (result != null)
            {
                // if result contains Error, return entire message
                if (result.Contains("Error"))
                {
                    return result;
                }
                // if result is over 100 characters, only return the first 100 characters.
                if (result.Length > 100)
                {
                    result = result.Substring(0, 100) + "(...)";

                    return result;
                }

                return result;
            }

            return "Code run successfully. no output is available.";
        }
        public void Dispose()
        {
            this.dotnetInteractiveService?.Dispose();
        }
    }
}
