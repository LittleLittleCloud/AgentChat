# Dotnet interpreter: run csharp code with GPT function_call

## TL;DR
This example shows how to use GPT to resolve task by writing and running csharp code.

## Pre-requisites
To run this example, you need to have access to a GPT-0613 model.

## Disclaimer
This example is for a proof-of-concept purpose only. It's not production ready and not recommended to use in production.

Cost will be applied to your openai account when you run this example.

## How to run
1. [Configure environment for GPT access.](../../README.md#how-to-configure-environment-for-gpt-access)
2. Run the example with `dotnet run` command.

The built-in task is to print out the date today and the output should be the current date. You can also try the following tasks:
- what's the 10th fibonacci number?
- create 10 random int numbers that no greater than 100.
- create 10 empty folder under xxx directory.
- shut down the computer.

## Limitations
This example can't resolving a task if it requires multiple steps to complete. It also can't automatically fix the code if the code is not correct, which is quite common when using GPT to generate code that is not python.

To enable LLM to resolve more complex tasks, or allow it to self-correct the code, we can introduce another llm which provides feedback to the current llm. For example, we can introduce a coder llm to write the code, and a runner llm to run the code and provide feedback to the coder llm. Such back-and-forth coding-debugging workflow is demonstrated in [coder-runner example](../AgentChat.CoderRunner).
