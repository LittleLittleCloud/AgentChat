# GroupChat: coder and runner

## TL;DR
This example demostrate the coder-runner pattern in group chat. The coder llm writes the code, and the runner llm runs the code and provides feedback to the coder llm. The coder llm then uses the feedback to improve the code. This workflow is repeated until the task is resolved. You can also find a sample pr.txt and chat_history.txt under this [workDir](./workDir/) folder.

## Pre-requisites
To run this example, you need to have access to a GPT-0613 model.
To achieve a better result, it's recommended to use GPT-4 series model on coder agent.

## Disclaimer
This example is for a proof-of-concept purpose only. It's not production ready and not recommended to use in production.

The output result might be vary because the output of GPT-3 is not deterministic.

Cost will be applied to your openai account when you run this example.

## How to run
1. Set up the environment variable `OPENAI_API_KEY` with your openai api key.
2. Run the example with `dotnet run` command.

## How it works
This example create a group chat which contains an admin agent, a coder agent and a runner agent.
- Admin: responsible for creating tasks and assigning them to coder and runner.
- Coder: responsible for writing code to resolve the task. It first creates a step-by-step plan to resolve the task, then write the code to implement the plan one step at a time.
- Runner: responsible for running the code and providing feedback to the coder. It runs the code by sending it to `dotnet-interactive`, then parse the output and send it back to the coder.

The workflow is as follows:
1. Admin creates a task and assigns it to coder and runner.
2. Coder creates a plan to resolve the task.
3. Coder writes the code to implement the plan.
4. Coder sends the code to runner.
5. Runner runs the code and sends the output back to coder.
6. Coder uses the output to improve the code.
7. Repeat step 4-6 until the task is resolved.

## Limitations
GPT 3.5 is not as good as writing dotnet code as it is writing python code. Therefore, the coder agent may not be able to fix the code even with the error message from the runner agent.
One way to improve the result is to use GPT 4 series model on coder agent. However, it usually brings more cost and longer latency.
Another way is to introduce an examplar agent to the group chat. The examplar agent can provide correct code examples to the coder agent, which can help the coder agent to fix the code. The code example can be retrieved by constructing a knowledge base from stackoverflow or github.