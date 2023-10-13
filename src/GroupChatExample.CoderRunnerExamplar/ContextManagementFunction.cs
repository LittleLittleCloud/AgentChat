using GroupChatExample.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupChatExample.CoderRunnerExamplar
{
    public partial class ContextManagementFunction
    {
        private readonly string _contextFilePath;

        public ContextManagementFunction(string contextFilePath)
        {
            _contextFilePath = contextFilePath;
        }

        /// <summary>
        /// Summarize the current conversation.
        /// </summary>
        /// <param name="task">The global task to complete.</param>
        /// <param name="completedSteps">Completed steps.</param>
        /// <param name="currentStep">The current step.</param>
        /// <param name="existingCodeForEachStep">existing code solution for each step.</param>
        /// <param name="context">current context.</param>
        [FunctionAttribution]
        public async Task<string> SaveContext(string task, string[] completedSteps, string currentStep, string[] existingCodeForEachStep, string context)
        {
            var result = $@"
-Task-
{task}

-Completed Steps-
{string.Join("\r\n", completedSteps)}

-Current Step-
{currentStep}

-Existing Code-
{string.Join("\r\n", existingCodeForEachStep.Select(x => $@"
```csharp
{x}
```
"))}

{context}
";
            await File.WriteAllTextAsync(_contextFilePath, result);
            return result;
        }

        /// <summary>
        /// load previous context.
        /// </summary>
        [FunctionAttribution]
        public async Task<string> LoadContext(string contextName)
        {
            if (!File.Exists(_contextFilePath))
            {
                return "No previous context found. Please start from the first step";
            }

            return await File.ReadAllTextAsync(_contextFilePath);
        }
    }
}
