using AgentChat.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentChat.CoderRunnerExamplar
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
        /// <param name="existingClass">Existing class definition from previous context.</param>
        /// <param name="existingFunctions">Existing function definitions from previous context.</param>
        /// <param name="existingVariables">Existing variables with type from previous context.</param>
        /// <param name="context">current context.</param>
        [FunctionAttribution]
        public async Task<string> SaveContext(string task, string[] completedSteps, string currentStep, string[] existingFunctions, string[] existingVariables, string[] existingClass, string context)
        {
            var result = $@"
-Task-
{task}

-Completed Steps-
{string.Join("\r\n", completedSteps)}

-Current Step-
{currentStep}

-Existing Functions-
```csharp
{string.Join("\r\n", existingFunctions)}
```

-Existing Variables-
```csharp
{string.Join("\r\n", existingVariables)}
```

-Existing Class-
```csharp
{string.Join("\r\n", existingClass)}
```

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
