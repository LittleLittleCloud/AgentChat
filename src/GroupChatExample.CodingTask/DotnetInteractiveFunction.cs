using GroupChatExample.DotnetInteractiveService;
using GroupChatExample.Helper;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using System.Text;

namespace GroupChatExample.CodingTask
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
        /// Run dotnet code block from given chat message.
        /// chat message will look like this:
        /// ## Example 1 ##
        /// run the code below and verify it.
        /// ```csharp
        /// // code to run
        /// ```
        /// ## end ##
        /// </summary>
        /// <param name="chatMessage">original chat message</param>
        [FunctionAttribution]
        public async Task<string> RunCode(string chatMessage)
        {
            if (this._interactiveService == null)
            {
                throw new Exception("InteractiveService is not initialized.");
            }

            // retrieve code from message between ```csharp and ```
            string? code = null;

            try
            {
                code = chatMessage.Substring(chatMessage.IndexOf("```csharp") + 9);
                code = code.Substring(0, code.IndexOf("```"));
            }
            catch
            {
                code = chatMessage;
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
