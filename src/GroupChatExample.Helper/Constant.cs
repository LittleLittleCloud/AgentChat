using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupChatExample.Helper
{
    public static class Constant
    {
        public static string MLNET101SEARCHTOEKN { get; set; } = Environment.GetEnvironmentVariable("MLNET101SEARCHTOEKN");
        public static string GPT_35_MODEL_ID { get; set; } = Environment.GetEnvironmentVariable("AZURE_GPT_35_MODEL_ID") ?? "gpt-35-turbo";

        public static string GPT_4_MODEL_ID { get; set; } = Environment.GetEnvironmentVariable("AZURE_GPT_4_MODEL_ID") ?? "gpt-4";

        public static string? OPENAI_API_KEY { get; set; } = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        public static string? AZURE_OPENAI_API_KEY { get; set; } = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        public static string? AZURE_OPENAI_ENDPOINT { get; set; } = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");

        public static OpenAIClient? AzureOpenAI { get; set; } = AZURE_OPENAI_API_KEY != null ? new OpenAIClient(
            new Uri(AZURE_OPENAI_ENDPOINT),
            new Azure.AzureKeyCredential(AZURE_OPENAI_API_KEY)) : null;

        public static OpenAIClient? OpenAIOpenAI { get; set; } = OPENAI_API_KEY != null ? new OpenAIClient(OPENAI_API_KEY) : null;

        public static OpenAIClient GPT { get; set; } = AzureOpenAI ?? OpenAIOpenAI ?? throw new Exception(@"No OpenAI client is available. Please choose one of the folloiwng option
- Set `OPENAI_API_KEY`
- Set `AZURE_OPENAI_API_KEY and AZURE_OPENAI_ENDPOINT`
in your environment to provide access to OpenAI GPT model");


    }
}
