using System;
using AgentChat.OpenAI;

namespace AgentChat.Example.Share;

public static class Constant
{
    public static string? MLNET101SEARCHTOEKN { get; set; } = Environment.GetEnvironmentVariable("MLNET101SEARCHTOEKN");

    public static string? GPT_35_MODEL_ID { get; set; } =
        Environment.GetEnvironmentVariable("AZURE_GPT_35_MODEL_ID") ?? "gpt-3.5-turbo";

    public static string? GPT_4_MODEL_ID { get; set; } = Environment.GetEnvironmentVariable("AZURE_GPT_4_MODEL_ID") ?? "gpt-4";

    public static string? OPENAI_API_KEY { get; set; } = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    public static string? AZURE_OPENAI_API_KEY { get; set; } = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

    public static string? AZURE_OPENAI_ENDPOINT { get; set; } = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

    public static GPT? AzureGPT4 { get; set; } = AZURE_OPENAI_API_KEY != null && AZURE_OPENAI_ENDPOINT != null ?
        GPT.CreateFromAzureOpenAI(GPT_4_MODEL_ID, AZURE_OPENAI_API_KEY, AZURE_OPENAI_ENDPOINT) : null;

    public static GPT? OpenAIGPT4 { get; set; } =
        OPENAI_API_KEY != null ? GPT.CreateFromOpenAI(OPENAI_API_KEY, GPT_4_MODEL_ID) : null;

    public static GPT? AzureGPT35 { get; set; } = AZURE_OPENAI_API_KEY != null && AZURE_OPENAI_ENDPOINT != null ?
        GPT.CreateFromAzureOpenAI(GPT_35_MODEL_ID, AZURE_OPENAI_API_KEY, AZURE_OPENAI_ENDPOINT) : null;

    public static GPT? OpenAIGPT35 { get; set; } =
        OPENAI_API_KEY != null ? GPT.CreateFromOpenAI(OPENAI_API_KEY, GPT_35_MODEL_ID) : null;

    public static GPT GPT4 { get; set; } = AzureGPT4 ?? OpenAIGPT4 ?? throw new Exception(
        @"No OpenAI client is available. Please choose one of the folloiwng option
- Set `OPENAI_API_KEY`
- Set `AZURE_OPENAI_API_KEY and AZURE_OPENAI_ENDPOINT`
in your environment to provide access to OpenAI GPT model");

    public static GPT GPT35 { get; set; } = AzureGPT35 ?? OpenAIGPT35 ?? throw new Exception(
        @"No OpenAI client is available. Please choose one of the folloiwng option
- Set `OPENAI_API_KEY`
- Set `AZURE_OPENAI_API_KEY and AZURE_OPENAI_ENDPOINT`
in your environment to provide access to OpenAI GPT model");
}