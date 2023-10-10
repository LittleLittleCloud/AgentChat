using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GroupChatExample.Helper
{
    public interface IAgent
    {
        public string Name { get; }

        public Task<ChatMessage> CallAsync(IEnumerable<ChatMessage> conversation, CancellationToken ct = default);
    }
}
