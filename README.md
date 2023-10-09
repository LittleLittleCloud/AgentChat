# Copilot Builder - augment LLM calls naively

This repo is the home for `CopilotBuilder.Helper`, a library that works with `Copilot Builder` to augment the calls to `LLM`.

[![CI](https://github.com/LittleLittleCloud/CopilotBuilder/actions/workflows/ci.yml/badge.svg)](https://github.com/LittleLittleCloud/CopilotBuilder/actions/workflows/ci.yml)
[![nightly-build](https://github.com/LittleLittleCloud/CopilotBuilder/actions/workflows/nightly-build.yml/badge.svg)](https://github.com/LittleLittleCloud/CopilotBuilder/actions/workflows/nightly-build.yml)
[![CopilotBuilder.Helper package in ModelBuilder@Local feed in Azure Artifacts](https://devdiv.feeds.visualstudio.com/_apis/public/Packaging/Feeds/ModelBuilder@Local/Packages/15507809-ade3-4dca-8e6a-24240053710c/Badge)](https://devdiv.visualstudio.com/DevDiv/_artifacts/feed/ModelBuilder@Local/NuGet/CopilotBuilder.Helper?preferRelease=true)

## Short cut
- [Facilitate Chat FunctionCall for GPT-series model](#facilitate-chat-functioncall-for-gpt-series-model)
- [Dotnet interpreter](#dotnet-interpreter)

## Usage
### Facilitate [Chat FunctionCall](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.openai-readme?view=azure-dotnet-preview#use-chat-functions) for GPT-series model
`CopilotBuilder.Helper` provides a source generator that generates `FunctionDefition` and wrapper caller according to the signature of a function. Simply add `CopilotBuilder.Helper` to your project and add the `[FunctionAttribution]` to your function 

> Note
> - The function must be `partial`
> - To enable documentation comment parsing, you need to add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to your project file


```csharp
// file: ChatFunction.cs
using CopilotBuilder.Helper;

public partial class ChatFunction
{
    /// <summary>
    /// Get the current weather in a given location.
    /// </summary>
    /// <param name="city">The city to get the weather for</param>
    /// <param name="date">The date to get the weather for</param>
    [FunctionAttribution]
    public string GetCurrentWeather(string city, string date)
    {
        return $"The weather in {city} on {date} will be sunny.";
    }
}
```

The source generator will generate the `FunctionDefinition` and wrapper caller function which takes in a string argument and map it back to original function based on the documentation comment and the signature of the function:

```csharp
using Azure.AI.OpenAI;
using System.Text.Json;

// file: ChatFunction.generated.cs
public partial class ChatFunction
{
    public FunctionDefinition GetCurrentWeatherFunction
    {
        get => {
            new FunctionDefinition()
            {
                Name = "GetCurrentWeather",
                Description = "Get the current weather in a given location.",
                Parameters = BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        city = new
                        {
                            Type = "string",
                            Description = "The city to get the weather for",
                        },
                        date = new
                        {
                            Type = "string",
                            Description = "The date to get the weather for",
                        }
                    },
                },
                new JsonSerializerOptions() {  PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            };
        }
    }

    private class GetCurrentWeatherSchema
    {
        public string city { get; set; }
        public string date { get; set; }
    }

    public string GetCurrentWeather(string arguments)
    {
        var schema = JsonSerializer.Deserialize<GetCurrentWeatherSchema>(arguments);
        return GetCurrentWeather(schema.city, schema.date);
    }
}
```

After that, you can use generated `GetCurrentWeatherFunction` in the following LLM call:

```csharp
// file: ChatFunction.cs
// OpenAIClient client;
var conversationMessages = new List<ChatMessage>()
{
    new(ChatRole.User, "What is the weather like in Boston?"),
};

var chatCompletionsOptions = new ChatCompletionsOptions();
foreach (ChatMessage chatMessage in conversationMessages)
{
    chatCompletionsOptions.Messages.Add(chatMessage);
}

chatCompletionsOptions.Functions.Add(GetCurrentWeatherFunction);

Response<ChatCompletions> response = await client.GetChatCompletionsAsync(
    "gpt-35-turbo-0613",
    chatCompletionsOptions);
ChatChoice responseChoice = response.Value.Choices[0];
if (responseChoice.FinishReason == CompletionsFinishReason.FunctionCall && responseChoice.Function.Name == GetCurrentWeatherFunction.Name)
{
    var result = GetCurrentWeather(responseChoice.Arguments);
    Console.WriteLine(result);
    // The weather in Boston on 2021-06-13 will be sunny.
}
```

You can refer to [src/CopilotBuilder.SourceGenerator.Tests/FunctionExamples.cs](./src/CopilotBuilder.SourceGenerator.Tests/FunctionExamples.cs) for more examples. You can also find some more cases in [OpenAPIAgent](./src/CopilotBuilder.Example/OpenAPIAgent/) as well.

### Dotnet interpreter
`CopilotBuilder.Helper` provides a dotnet interpreter that can be used to run dotnet script from LLM response. This powerful feature allows llm to address almost arbitrary scenarios as long as it can be resolved by dotnet script.

To consume dotnet interpreter, simply wrap it as a `FunctionCall` so LLM can call it:
```csharp
using CopilotBuilder.DotnetInteractiveService;

public partial class OpenAPIAgent
{
    private InteractiveService? _interactiveService = null;

    public async Task InitializeAsync()
    {
        var workDir = Path.Combine(Path.GetTempPath(), "OpenAPIAgent");
        if (!Directory.Exists(workDir))
        {
            Directory.CreateDirectory(workDir);
        }

        _interactiveService = new InteractiveService(workDir);
        await _interactiveService.StartAsync(workDir, default);
    }

    /// <summary>
    /// Run dotnet script code in dotnet interactive notebook and return the result.
    /// </summary>
    /// <param name="code">dotnet script in plain text, don't include nuget install script, required.</param>
    /// <param name="nugetDependencies">nuget package dependencies, required.</param>
    [FunctionAttribution]
    public async Task<string> ExecuteNotebook(string code, string[]? nugetDependencies = null)
    {
        if (this._interactiveService == null)
        {
            throw new Exception("InteractiveService is not initialized.");
        }

        // submit code to dotnet interpreter
        var result = await this._interactiveService.SubmitCodeAsync(code, default);
        return result ?? "no output is available.";
    }
}
```

The complete example can be found in [OpenAPIAgent](./src/CopilotBuilder.Example/OpenAPIAgent/OpenAIAgent.dotnetRunnerFunction.cs).


