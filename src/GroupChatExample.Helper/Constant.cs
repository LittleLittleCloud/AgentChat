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
        public static string AZURE_GPT_35_MODEL_ID { get; set; } = "gpt-35-turbo-16k";

        public static string AZURE_GPT_4_MODEL_ID { get; set; } = "gpt-4";

        public static string GPT_35_API_KEY { get; set; } = Environment.GetEnvironmentVariable("GPT_35_API_KEY");

        public static string GPT_4_API_KEY { get; set; } = Environment.GetEnvironmentVariable("GPT_4_API_KEY");

        public static string GPT_35_ENDPOINT { get; set; } = Environment.GetEnvironmentVariable("GPT_35_ENDPOINT");

        public static string GPT_4_ENDPOINT { get; set; } = Environment.GetEnvironmentVariable("GPT_4_ENDPOINT");

        public static OpenAIClient AzureGPT35 { get; set; } = new OpenAIClient(
            new Uri(GPT_35_ENDPOINT),
            new Azure.AzureKeyCredential(GPT_35_API_KEY));

        public static OpenAIClient AzureGPT4 { get; set; } = new OpenAIClient(
                       new Uri(GPT_4_ENDPOINT),
                                  new Azure.AzureKeyCredential(GPT_4_API_KEY));

    }
}
