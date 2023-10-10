using GroupChatExample.DotnetInteractiveService;
using GroupChatExample.Helper;
using System;
using System.Text;
using System.Threading.Tasks;

namespace GroupChatExample.Helper
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
        /// Run dotnet code. Don't modify the code, run it as is.
        /// </summary>
        /// <param name="code">code.</param>
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
                    result = result.Substring(0, 100) + " (...too long to present)";

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
