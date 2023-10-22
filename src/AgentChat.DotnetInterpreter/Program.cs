// See https://aka.ms/new-console-template for more information


using Azure.AI.OpenAI;
using AgentChat.DotnetInteractiveService;
using AgentChat.DotnetInterpreter;

var workDir = Path.Combine(Path.GetTempPath(), "InteractiveService");
if (Directory.Exists(workDir))
{
    Directory.Delete(workDir, true);
}

Directory.CreateDirectory(workDir);

using var service = new InteractiveService(workDir);
await service.StartAsync(workDir, default);
using var runCodeFunction = new RunCodeFunction(service);
var openai_key = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set");
var openAIClient = new OpenAIClient(openai_key);

var systemMessage = new ChatMessage
{
    Role = ChatRole.System,
    Content = @"You are a helpful AI assistant. You resolve tasks using RunCode function. 
Here're some requirements:
- Always write the code in C#.
- Put the code in a single line.
- The code should contain Console.WriteLine to print out the result.
"
};

var userMessage = new ChatMessage
{
    Role = ChatRole.User,
    Content = "what's the 10th fibonacci number?",
};

var option = new ChatCompletionsOptions
{
    Functions = new[] { runCodeFunction.FunctionDefinition },
    Temperature = 0,
    MaxTokens = 256,
};

option.Messages.Add(systemMessage);
option.Messages.Add(userMessage);

var response = await openAIClient.GetChatCompletionsAsync("gpt-3.5-turbo-0613", option);
if (response.Value.Choices.First().Message.FunctionCall is FunctionCall fc && fc.Name == runCodeFunction.FunctionDefinition.Name)
{
    var result = await runCodeFunction.RunCodeAsync(fc.Arguments);
    Console.WriteLine(result);
}
else
{
    Console.WriteLine(response.Value.Choices.First().Message.Content);
}
