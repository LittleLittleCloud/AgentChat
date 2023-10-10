using GroupChatExample.DotnetInteractiveService;
using GroupChatExample.Helper;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GroupChatExample.CoderRunner
{
    public partial class DotnetInteractiveFunction : IDisposable
    {
        private readonly InteractiveService? _interactiveService = null;
        private readonly Logger? _logger;


        public DotnetInteractiveFunction(InteractiveService interactiveService, Logger? logger = null)
        {
            this._interactiveService = interactiveService;
            this._logger = logger;
        }

        /// <summary>
        /// Process the previous message. Don't modify the previous message, pass it as is.
        /// </summary>
        /// <param name="previousMessage">previous message.</param>
        [FunctionAttribution]
        public async Task<string> ProcessMessage(string previousMessage)
        {
            // retrieve all blocks from message between ``` and ```
            var blocks = previousMessage.Split("```");
            if (blocks.Length == 1)
            {
                // if message contains no block, return an error message.
                return "Message contains no code block.";
            }
            else
            {
                // if message contains multiple blocks, return an error message.
                if (blocks.Length > 3)
                {
                    return "Message contains multiple code blocks.";
                }
                else
                {
                    // if message contains ```csharp and ```, Run code.
                    var csharpCodeRegex = Regex.Match(previousMessage, @"```csharp[\s\S]*```");
                    var csharpCode = csharpCodeRegex.Value;
                    if (csharpCode.Length > 0)
                    {
                        return await this.RunCode(csharpCode);
                    }

                    return "Message contains no csharp code block.";
                }
            }
        }

        /// <summary>
        /// Run dotnet code block.
        /// </summary>
        /// <param name="code">code to run.</param>
        [FunctionAttribution]
        public async Task<string> RunCode(string code)
        {
            if (this._interactiveService == null)
            {
                throw new Exception("InteractiveService is not initialized.");
            }

            await (_logger?.LogToFile($"{nameof(RunCode)}.cs", code) ?? Task.CompletedTask);

            var result = await this._interactiveService.SubmitCSharpCodeAsync(code, default);
            if (result != null)
            {
                // if result contains Error, return entire message
                if (result.Contains("Error"))
                {
                    return result;
                }
                // if result is over 100 characters, only return the first 100 characters.
                if (result.Length > 100)
                {
                    result = result.Substring(0, 100) + "(...)";

                    return result;
                }

                return result;
            }

            return "Code run successfully. no output is available.";
        }

        /// <summary>
        /// Install nuget packages.
        /// </summary>
        /// <param name="nugetPackages">nuget package to install.</param>
        [FunctionAttribution]
        public async Task<string> InstallNugetPackages(string[] nugetPackages)
        {
            if (this._interactiveService == null)
            {
                throw new Exception("InteractiveService is not initialized.");
            }

            foreach (var nuget in nugetPackages ?? Array.Empty<string>())
            {
                var nugetInstallCommand = $"#r \"nuget:{nuget}\"";
                await this._interactiveService.SubmitCSharpCodeAsync(nugetInstallCommand, default);
            }

            var sb = new StringBuilder();
            sb.AppendLine("Installed nuget packages:");
            foreach (var nuget in nugetPackages ?? Array.Empty<string>())
            {
                sb.AppendLine($"- {nuget}");
            }

            return sb.ToString();
        }


        public void Dispose()
        {
            this._interactiveService?.Dispose();
        }
    }
}
