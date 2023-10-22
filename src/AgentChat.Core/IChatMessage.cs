using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgentChat
{
    public interface IChatMessage
    {
        ChatRole Role { get; }

        // TODO
        // add image

        string? Content { get; }

        string? Name { get; }

        string? From { get; }
    }
}
