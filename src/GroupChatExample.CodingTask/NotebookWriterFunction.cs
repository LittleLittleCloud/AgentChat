using GroupChatExample.Helper;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GroupChatExample.DotnetInteractiveService;
using Microsoft.DotNet.Interactive.Commands;

namespace GroupChatExample.CodingTask
{
    public partial class NotebookWriterFunction : IDisposable
    {
        private readonly Logger? _logger;
        private readonly string _notebookPath;
        private readonly string _workingFolder;
        private readonly InteractiveService? _interactiveService = null;
        private readonly DotnetInteractiveFunction dotnetInteractiveFunction;

        public NotebookWriterFunction(string notebookPath, Logger? logger = null)
        {
            this._logger = logger;
            this._notebookPath = notebookPath;
            this._workingFolder = System.IO.Path.GetDirectoryName(notebookPath) ?? string.Empty;
            this._interactiveService = new InteractiveService(_workingFolder);
            this._interactiveService.StartAsync(this._workingFolder, default).Wait();
            this.dotnetInteractiveFunction = new DotnetInteractiveFunction(this._interactiveService, logger: this._logger);
        }
        /// <summary>
        /// Create an empty notebook.
        /// </summary>
        /// <param name="fullName">full path to notebook.</param>
        /// <param name="overwrite">overwrite existing notebook if true.</param>

        [FunctionAttribution]
        public async Task<string> CreateNotebook(string fullName, bool overwrite = false)
        {
            if (File.Exists(fullName))
            {
                if (!overwrite)
                {
                    return $"Notebook {fullName} already exists. No change will be made. If you want to overwrite existing notebook, please set overwrite to true and try again";
                }

                File.Delete(fullName);
            }

            var document = new InteractiveDocument();

            using var stream = File.OpenWrite(fullName);
            Notebook.Write(document, stream);
            stream.Dispose();

            return $"A new Notebook: {fullName} is created.";
        }

        /// <summary>
        /// Add markdown cell to a notebook.
        /// </summary>
        /// <param name="cellContent">cell content</param>
        /// <param name="index">cell index</param>
        [FunctionAttribution]
        public async Task<string> AddMarkdownCell(string cellContent, int index)
        {
            return await AddCell(cellContent, "markdown", index);
        }

        /// <summary>
        /// Add csharp code cell to a notebook.
        /// </summary>
        /// <param name="cellContent">cell content</param>
        /// <param name="index">cell index</param>
        [FunctionAttribution]
        public async Task<string> AddCSharpCodeCell(string cellContent, int index)
        {
            // check if code is valid
            var result = await this._interactiveService!.SubmitCSharpCodeAsync(cellContent, default);
            if (result?.StartsWith("Error") is true)
            {
                return result + "\r Please fix the error and try again.";
            }

            var addedMessage = await AddCell(cellContent, "csharp", index);

            if(result != null)
            {
                // shorten result if it is too long
                if (result.Length > 100)
                {
                    result = result.Substring(0, 100) + "..." + "(remaining output)";
                }
            }
            return $@"{addedMessage}
The output is {result ?? "empty"}";
        }

        /// <summary>
        /// Add nuget installation cell to an existing notebook.
        /// </summary>
        /// <param name="nugetPackageNames">nuget package names.</param>
        [FunctionAttribution]
        public async Task<string> AddNugetInstallationCell(string[] nugetPackageNames)
        {
            if (!File.Exists(this._notebookPath))
            {
                using var stream = File.OpenWrite(this._notebookPath);
                Notebook.Write(new InteractiveDocument(), stream);
                stream.Dispose();
            }
            await this.dotnetInteractiveFunction.InstallNugetPackages(nugetPackageNames);
            using var readStream = File.OpenRead(this._notebookPath);
            var document = await Notebook.ReadAsync(readStream);
            readStream.Dispose();

            var sb = new StringBuilder();
            foreach (var nuget in nugetPackageNames ?? Array.Empty<string>())
            {
                sb.AppendLine($"#r \"nuget:{nuget}\"");
            }

            var cell = new InteractiveDocumentElement(sb.ToString(), "csharp");

            document.Add(cell);

            using var writeStream = File.OpenWrite(this._notebookPath);
            Notebook.Write(document, writeStream);
            writeStream.Dispose();

            return $"Nuget packages are added to {this._notebookPath}. The following nugets are added: {string.Join(";", nugetPackageNames ?? Array.Empty<string>())}";
        }   

        /// <summary>
        /// Delete an existing notebook.
        /// </summary>
        /// <param name="notebookPath">notebook path.</param>
        [FunctionAttribution]
        public async Task<string> DeleteNotebook(string notebookPath, string dummy)
        {
            if (!File.Exists(notebookPath))
            {
                return $"Notebook {notebookPath} does not exist. No change will be made.";
            }

            File.Delete(notebookPath);

            return $"Notebook {notebookPath} is deleted.";
        }

        private async Task<string> AddCell(string cellContent, string kernelName, int cellIndex)
        {
            if (!File.Exists(this._notebookPath))
            {
                using var stream = File.OpenWrite(this._notebookPath);
                Notebook.Write(new InteractiveDocument(), stream);
                stream.Dispose();
            }

            using var readStream = File.OpenRead(this._notebookPath);
            var document = await Notebook.ReadAsync(readStream);
            readStream.Dispose();

            var cell = new InteractiveDocumentElement(cellContent, kernelName);

            document.Add(cell);

            using var writeStream = File.OpenWrite(this._notebookPath);
            Notebook.Write(document, writeStream);
            // sleep 3 seconds
            await Task.Delay(3000);
            writeStream.Flush();
            writeStream.Dispose();

            return $"cell {cellIndex} is added to {this._notebookPath}.";
        }

        public void Dispose()
        {
            this._logger?.Log("disposing interactive service");
            this._interactiveService?.Dispose();
        }
    }
}
