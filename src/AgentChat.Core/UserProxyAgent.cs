using Azure.AI.OpenAI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentChat
{
    /// <summary>
    /// An agent that takes user input and send it to the chat model.
    /// </summary>
    public class UserProxyAgent : IAgent
    {
        public UserProxyAgent(string name)
        {
            Name = name;
        }

        public string Name { get; }

        /// <summary>
        /// Take user input and send it to the chat model.
        /// </summary>
        public async Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> conversation, CancellationToken ct = default)
        {
            var consolePrompt = $"waiting for user input for {Name}...";
            Console.WriteLine(consolePrompt);
            var hintPrompt = "hit enter to start with newline, hit ctrl+enter to send message";
            Console.WriteLine(hintPrompt);

            var userInput = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    if (key.Modifiers == ConsoleModifiers.Control)
                    {
                        break;
                    }
                    else
                    {
                        userInput.AppendLine();
                    }
                }
                else
                {
                    userInput.Append(key.KeyChar);
                }
            }


            var input = userInput.ToString();

            return new Message(ChatRole.Assistant, input, Name);
        }
    }
}
