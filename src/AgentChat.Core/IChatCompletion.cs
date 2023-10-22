using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentChat
{
    public interface IChatCompletion
    {
        IChatMessage? Message { get; }

        CompletionsUsage? Usage { get; }
    }
}
