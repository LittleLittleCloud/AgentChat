﻿using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Azure.AI.OpenAI;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace AgentChat.SourceGenerator.Tests
{
    public class FunctionExample
    {
        private readonly FunctionExamples functionExamples = new FunctionExamples();
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        [Fact]
        public void Add_Test()
        {
            var args = new
            {
                a = 1,
                b = 2,
            };

            this.VerifyFunction(functionExamples.AddWrapper, args, 3);
            this.VerifyFunctionDefinition(functionExamples.AddFunction);
        }

        [Fact]
        public void Sum_Test()
        {
            var args = new
            {
                args = new double[] { 1, 2, 3 },
            };

            this.VerifyFunction(functionExamples.SumWrapper, args, 6.0);
            this.VerifyFunctionDefinition(functionExamples.SumFunction);
        }

        [Fact]
        public async Task DictionaryToString_Test()
        {
            var args = new
            {
                xargs = new Dictionary<string, string>
                {
                    { "a", "1" },
                    { "b", "2" },
                },
            };

            await this.VerifyAsyncFunction(functionExamples.DictionaryToStringAsyncWrapper, args, JsonSerializer.Serialize(args.xargs, jsonSerializerOptions));
            this.VerifyFunctionDefinition(functionExamples.DictionaryToStringAsyncFunction);
        }

        [Fact]
        public async Task TopLevelFunctionExampleAddTestAsync()
        {
            var example = new TopLevelStatementFunctionExample();
            var args = new
            {
                a = 1,
                b = 2,
            };

            await this.VerifyAsyncFunction(example.AddWrapper, args, "3");
        }

        [Fact]
        public void Query_Test()
        {
            var args = new
            {
                query = "hello",
                k = 3,
            };

            this.VerifyFunction(functionExamples.QueryWrapper, args, new[] { "hello", "hello", "hello" });
            this.VerifyFunctionDefinition(functionExamples.QueryFunction);
        }

        [UseReporter(typeof(DiffReporter))]
        [UseApprovalSubdirectory("ApprovalTests")]
        private void VerifyFunctionDefinition(FunctionDefinition function)
        {
            var func = new
            {
                name = function.Name,
                description = function.Description.Replace(Environment.NewLine, ","),
                parameters = function.Parameters.ToObjectFromJson<object>(options: jsonSerializerOptions),
            };

            Approvals.Verify(JsonSerializer.Serialize(func, jsonSerializerOptions));
        }

        private void VerifyFunction<T, U>(Func<string, T> func, U args, T expected)
        {
            var str = JsonSerializer.Serialize(args, jsonSerializerOptions);
            var res = func(str);
            res.Should().BeEquivalentTo(expected);
        }

        private async Task VerifyAsyncFunction<T, U>(Func<string, Task<T>> func, U args, T expected)
        {
            var str = JsonSerializer.Serialize(args, jsonSerializerOptions);
            var res = await func(str);
            res.Should().BeEquivalentTo(expected);
        }
    }
}
