# Group Chat Examples for Dotnet

This repo contains the code examples for group chat.

[![CI](https://github.com/LittleLittleCloud/CopilotBuilder/actions/workflows/ci.yml/badge.svg)](https://github.com/LittleLittleCloud/CopilotBuilder/actions/workflows/ci.yml)
[![nightly-build](https://github.com/LittleLittleCloud/CopilotBuilder/actions/workflows/nightly-build.yml/badge.svg)](https://github.com/LittleLittleCloud/CopilotBuilder/actions/workflows/nightly-build.yml)

## How to configure environment for GPT access
In order to run examples under this repo, you need to provide the following environment variables:

#### OpenAI
- `OPENAI_API_KEY` - your OpenAI API key

#### Azure OpenAI
- `AZURE_OPENAI_ENDPOINT` - your Azure OpenAI endpoint
- `AZURE_OPENAI_API_KEY` - your Azure OpenAI key
- (Optional) `AZURE_GPT_35_MODEL_ID` - GPT-3.5 model name (default: `gpt-35-turbo`)
- (Optional) `AZURE_GPT_4_MODEL_ID` - GPT-4 model name (default: `gpt-4.0`)

## Examples
<!-- table -->
<!-- column: example name, path, description -->
| Name | Description |
| ------- | ----------- |
| [dotnet interpreter](./src/GroupChatExample.DotnetInterpreter/) | Using LLM to resolve task by writing and running csharp code. |
| [coder + runner](./src/GroupChatExample.CoderRunner/) | Group chat using coder-runner pattern. This example shows how to use a coder agent and a runner agent to find the most recent PR from [ML.Net](http://github.com/dotnet/machinelearning) using github api and save it under pr.txt. |
| [coder + runner + examplar](./src/GroupChatExample.CoderRunnerExamplar/) | Group chat using coder-runner-examplar pattern. This example shows how to use a mlnet examplar agent, a coder agent and a runner agent to train a lightgbm binary classifier on a dummy dataset and save the model as lgbm.mlnet.  |
## Group chat
check out our [AutoGen](https://github.com/microsoft/autogen) library for more information over group chat.