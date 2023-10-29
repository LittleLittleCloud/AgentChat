## The OpenAI integration library for AgentChat

This library provides the integration with OpenAI chat models for AgentChat. It also provides a source generator that generates `FunctionDefinition` and wrapper caller according to the signature of a function.

## Short cut
- [Facilitate Chat FunctionCall for GPT-series model](#facilitate-chat-functioncall-for-gpt-series-model)

## Usage
### Facilitate [Chat FunctionCall](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.openai-readme?view=azure-dotnet-preview#use-chat-functions) for GPT-series model
`AgentChat` provides a source generator that generates `FunctionDefition` and wrapper caller according to the signature of a function. Simply add `AgentChat` to your project and add the `[FunctionAttribution]` to your function 

> Note
> - The function must be `partial`
> - To enable documentation comment parsing, you need to add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to your project file


```csharp
// file: ChatFunction.cs
using AgentChat;

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
