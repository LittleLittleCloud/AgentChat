using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;

namespace AgentChat.DotnetInteractiveService;

public class DotnetInteractiveFunction : IDisposable
{
    private readonly InteractiveService? _interactiveService;

    private readonly KernelInfoCollection _kernelInfoCollection = new();

    private string? _notebookPath;

    public FunctionDefinition RunCodeFunction =>
        new()
        {
            Name = @"RunCode",
            Description = """
                          Run existing dotnet code from message. Don't modify the code, run it as is.
                          """,
            Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        code = new
                        {
                            Type = @"string",
                            Description = @"code."
                        }
                    },
                    Required = new[]
                    {
                        "code"
                    }
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
        };

    public FunctionDefinition InstallNugetPackagesFunction =>
        new()
        {
            Name = @"InstallNugetPackages",
            Description = """
                          Install nuget packages.
                          """,
            Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        nugetPackages = new
                        {
                            Type = @"array",
                            Items = new
                            {
                                Type = @"string"
                            },
                            Description = @"nuget package to install."
                        }
                    },
                    Required = new[]
                    {
                        "nugetPackages"
                    }
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
        };

    public DotnetInteractiveFunction(InteractiveService interactiveService, string? notebookPath = null,
                                     bool continueFromExistingNotebook = false)
    {
        _interactiveService = interactiveService;
        _notebookPath = notebookPath;
        _kernelInfoCollection.Add(new KernelInfo("csharp"));
        _kernelInfoCollection.Add(new KernelInfo("markdown"));

        if (_notebookPath != null)
        {
            if (continueFromExistingNotebook == false)
            {
                // remove existing notebook
                if (File.Exists(_notebookPath))
                {
                    File.Delete(_notebookPath);
                }

                var document = new InteractiveDocument();

                using var stream = File.OpenWrite(_notebookPath);
                Notebook.Write(document, stream, _kernelInfoCollection);
                stream.Flush();
            }
            else if (continueFromExistingNotebook && File.Exists(_notebookPath))
            {
                // load existing notebook
                using var readStream = File.OpenRead(_notebookPath);
                var document = Notebook.Read(readStream, _kernelInfoCollection);

                foreach (var cell in document.Elements)
                {
                    if (cell.KernelName == "csharp")
                    {
                        var code = cell.Contents;
                        _interactiveService.SubmitCSharpCodeAsync(code, default).Wait();
                    }
                }
            }
            else
            {
                // create an empty notebook
                var document = new InteractiveDocument();

                using var stream = File.OpenWrite(_notebookPath);
                Notebook.Write(document, stream, _kernelInfoCollection);
                stream.Flush();
            }
        }
    }

    public void Dispose()
    {
        _interactiveService?.Dispose();
    }

    private async Task AddCellAsync(string cellContent, string kernelName)
    {
        if (_notebookPath == null)
        {
            throw new ArgumentException("_notebookPath is null", nameof(_notebookPath));
        }

        if (!File.Exists(_notebookPath))
        {
            using var stream = File.OpenWrite(_notebookPath);
            Notebook.Write(new InteractiveDocument(), stream, _kernelInfoCollection);
        }

        using var readStream = File.OpenRead(_notebookPath);
        var document = Notebook.Read(readStream, _kernelInfoCollection);

        var cell = new InteractiveDocumentElement(cellContent, kernelName);

        document.Add(cell);

        using var writeStream = File.OpenWrite(_notebookPath);
        Notebook.Write(document, writeStream, _kernelInfoCollection);

        // sleep 3 seconds
        await Task.Delay(3000);
        writeStream.Flush();
    }

    /// <summary>
    ///     Install nuget packages.
    /// </summary>
    /// <param name="nugetPackages">nuget package to install.</param>
    public async Task<string> InstallNugetPackages(string[] nugetPackages)
    {
        if (_interactiveService == null)
        {
            throw new Exception("InteractiveService is not initialized.");
        }

        var codeSB = new StringBuilder();

        foreach (var nuget in nugetPackages ?? Array.Empty<string>())
        {
            var nugetInstallCommand = $"#r \"nuget:{nuget}\"";
            codeSB.AppendLine(nugetInstallCommand);
            await _interactiveService.SubmitCSharpCodeAsync(nugetInstallCommand, default);
        }

        var code = codeSB.ToString();

        if (_notebookPath != null)
        {
            await AddCellAsync(code, "csharp");
        }

        var sb = new StringBuilder();
        sb.AppendLine("Installed nuget packages:");

        foreach (var nuget in nugetPackages ?? Array.Empty<string>())
        {
            sb.AppendLine($"- {nuget}");
        }

        return sb.ToString();
    }

    public Task<string> InstallNugetPackagesWrapper(string arguments)
    {
        var schema = JsonSerializer.Deserialize<InstallNugetPackagesSchema>(
            arguments,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        return InstallNugetPackages(schema!.nugetPackages);
    }

    /// <summary>
    ///     Run existing dotnet code from message. Don't modify the code, run it as is.
    /// </summary>
    /// <param name="code">code.</param>
    public async Task<string> RunCode(string code)
    {
        if (_interactiveService == null)
        {
            throw new Exception("InteractiveService is not initialized.");
        }

        var result = await _interactiveService.SubmitCSharpCodeAsync(code, default);

        if (result != null)
        {
            // if result contains Error, return entire message
            if (result.StartsWith("Error:"))
            {
                return result;
            }

            // add cell if _notebookPath is not null
            if (_notebookPath != null)
            {
                await AddCellAsync(code, "csharp");
            }

            // if result is over 100 characters, only return the first 100 characters.
            if (result.Length > 100)
            {
                result = result.Substring(0, 100) + " (...too long to present)";

                return result;
            }

            return result;
        }

        // add cell if _notebookPath is not null
        if (_notebookPath != null)
        {
            await AddCellAsync(code, "csharp");
        }

        return "Code run successfully. no output is available.";
    }

    public Task<string> RunCodeWrapper(string arguments)
    {
        var schema = JsonSerializer.Deserialize<RunCodeSchema>(
            arguments,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        return RunCode(schema!.code);
    }

    private class InstallNugetPackagesSchema
    {
        public string[] nugetPackages { get; set; } = Array.Empty<string>();
    }

    private class RunCodeSchema
    {
        public string code { get; set; } = string.Empty;
    }
}