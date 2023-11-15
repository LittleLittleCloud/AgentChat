# An unofficial implementation of AutoGen for dotnet

This repo contains an unofficial implementation of AutoGen for dotnet. It is a library that allows you to build a group chat bot that can help you to resolve tasks by writing and running code.

[![CI](https://github.com/LittleLittleCloud/AgentChat/actions/workflows/ci.yml/badge.svg)](https://github.com/LittleLittleCloud/AgentChat/actions/workflows/ci.yml)
[![nightly-build](https://github.com/LittleLittleCloud/AgentChat/actions/workflows/nightly-build.yml/badge.svg)](https://github.com/LittleLittleCloud/AgentChat/actions/workflows/nightly-build.yml)

## Usage
First add the following package reference to your project file:
```xml
<ItemGroup>
    <PackageReference Include="AgentChat.Core" />
    <!-- Add AgentChat.OpenAI to connect to OpenAI chat models -->
    <PackageReference Include="AgentChat.OpenAI" />
</ItemGroup>
```

> Nightly Build Feed: https://www.myget.org/F/agentchat/api/v3/index.json

Then you can using the following code to create an agent chat.

#### TwoAgent Chat
```csharp
using AgentChat;
var gpt35 = GPT.CreateFromOpenAI(OPENAI_API_KEY, GPT_35_MODEL_ID);
var alice = gpt35.CreateAgent(
    name: "Alice",
    roleInformation: "You are a helpful AI assistant.");

var bob = gpt35.CreateAgent(
    name: "Bob",
    roleInformation: "You are a helpful AI assistant.");

var chatHistory = await alice.SendMessagesAsync(
    bob,
    "Hi, I am Alice.",
    maxRound: 1);

// chatHistory
// From Alice: Hi, I am Alice.
// From Bob: Hi, I am Bob.
```

#### Group Chat
```csharp
using AgentChat;
var gpt35 = GPT.CreateFromOpenAI(OPENAI_API_KEY, GPT_35_MODEL_ID);
var alice = gpt35.CreateAgent(
    name: "Alice",
    roleInformation: "You are a helpful AI assistant.");

var bob = gpt35.CreateAgent(
    name: "Bob",
    roleInformation: "You are a helpful AI assistant.");

var carol = gpt35.CreateAgent(
    name: "Carol",
    roleInformation: "You are a helpful AI assistant.");

var group = new GroupChat(
    chatLLM: gpt35,
    admin: alice,
    agents: new[] { bob, carol });

var chatHistory = await alice.SendMessagesAsync(
    group,
    "Hi, I am Alice.",
    maxRound: 3);
// chatHistory
// From Alice: Hi, I am Alice.
// From Bob: Hi, I am Bob.
// From Carol: Hi, I am Carol.
```

#### Function call
You can augment agent chat with function call.
```csharp
// file: SayNameFunction.cs

using AgentChat;
/// <summary>
/// Say name.
/// </summary>
/// <param name="name">name.</param>
[FunctionAttribution]
public async Task<string> SayName(string name)
{
    return $"Your name is {name}.";
}

// file: Program.cs
using AgentChat;
var sayNameFunction = new SayNameFunction();
var gpt35 = GPT.CreateFromOpenAI(OPENAI_API_KEY, GPT_35_MODEL_ID);
var heisenberg = gpt35.CreateAgent(
    name: "Heisenberg",
    roleInformation: "You are Heisenberg.");

var bob = gpt35.CreateAgent(
    name: "Bob",
    roleInformation: "You call SayName function.",
    functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>>{
        { sayNameFunction.SayNameFunction, sayNameFunction.SayNameWrapper }
    });

var chatHistory = await heisenberg.SendMessagesAsync(
    bob,
    "Say, My, Name.",
    maxRound: 1);

// chatHistory
// From Heisenberg: Say, My, Name.
// From Bob: Your name is Heisenberg.
```

`AgentChat.OpenAI` provides a source generator that generates `FunctionDefition` and wrapper caller according to the signature of a function. For more information, please check [Facilitate Chat FunctionCall for GPT-series model](./src/AgentChat.OpenAI/README.md#facilitate-chat-functioncall-for-gpt-series-model).

## Notebook Examples
You can find notebook examples from [here](./notebook/).

## More Examples
You can find more examples from below table.

<!-- table -->
<!-- column: example name, path, description -->
| Name | Description |
| ------- | ----------- |
| [dotnet interpreter](./src/AgentChat.DotnetInterpreter/) | Using LLM to resolve task by writing and running csharp code. |
| [coder + runner](./src/AgentChat.CoderRunner/) | Group chat using coder-runner pattern. This example shows how to use a coder agent and a runner agent to find the most recent PR from [ML.Net](http://github.com/dotnet/machinelearning) using github api and save it under pr.txt. |
| [coder + runner + examplar](./src/AgentChat.CoderRunnerExamplar/) | Group chat using coder-runner-examplar pattern. This example shows how to use a mlnet examplar agent, a coder agent and a runner agent to train a lightgbm binary classifier on a dummy dataset and save the model as lgbm.mlnet.  |


To run the examples, you first need to configure the environment variables. See [How to configure environment for GPT access](#how-to-configure-environment-for-gpt-access) for more information.

## Group chat
check out our [AutoGen](https://github.com/microsoft/autogen) library for more information over group chat.

### How to configure environment for GPT access
In order to run examples under this repo, you need to provide the following environment variables:
#### OpenAI
- `OPENAI_API_KEY` - your OpenAI API key

#### Azure OpenAI
- `AZURE_OPENAI_ENDPOINT` - your Azure OpenAI endpoint
- `AZURE_OPENAI_API_KEY` - your Azure OpenAI key
- (Optional) `AZURE_GPT_35_MODEL_ID` - GPT-3.5 model name (default: `gpt-35-turbo`)
- (Optional) `AZURE_GPT_4_MODEL_ID` - GPT-4 model name (default: `gpt-4.0`)
