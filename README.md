# An unofficial implementation of AutoGen for dotnet

This repo contains an unofficial implementation of AutoGen for dotnet. It is a library that allows you to build a group chat bot that can help you to resolve tasks by writing and running code.

[![CI](https://github.com/LittleLittleCloud/AgentChat/actions/workflows/ci.yml/badge.svg)](https://github.com/LittleLittleCloud/AgentChat/actions/workflows/ci.yml)
[![nightly-build](https://github.com/LittleLittleCloud/AgentChat/actions/workflows/nightly-build.yml/badge.svg)](https://github.com/LittleLittleCloud/AgentChat/actions/workflows/nightly-build.yml)

## Usage
First add the following package reference to your project file:
```xml
<ItemGroup>
    <PackageReference Include="AgentChat.Core" />
    <!-- Add AgentChat.GPT to connect to GPT models -->
    <PackageReference Include="AgentChat.GPT" />
</ItemGroup>
```

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

#### Function call support
`AgentChat` provides a source generator that generates `FunctionDefition` and wrapper caller according to the signature of a function. For more information, please check [Facilitate Chat FunctionCall for GPT-series model](./src/AgentChat.Core/README.md#facilitate-chat-functioncall-for-gpt-series-model).

## More Examples
You can find more examples under [src](./src/) folder.

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