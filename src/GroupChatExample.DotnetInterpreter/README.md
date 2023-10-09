# Dotnet interpreter: run csharp code with GPT function_call

## TL;DR
This example shows how to use GPT to resolve task by writing and running csharp code.

## Pre-requisites
To run this example, you need to have access to a GPT-0613 model.

## How to run
1. Set up the environment variable `OPENAI_API_KEY` with your openai api key.
2. Run the example with `dotnet run` command.

The built-in task is to print out the date today and the output should be the current date. You can also try the following tasks:
- what's the 10th fibonacci number?
- create 10 random int numbers that no greater than 100.
- create 10 empty folder under xxx directory.
- shut down the computer.
