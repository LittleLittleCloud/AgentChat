// <copyright file="InteractiveService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Reflection;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;

namespace AgentChat.DotnetInteractiveService;

public class InteractiveService : IDisposable
{
    private const string DotnetInteractiveToolNotInstallMessage =
        "Cannot find a tool in the manifest file that has a command named 'dotnet-interactive'.";

    private bool disposedValue;

    private Kernel? kernel;

    private readonly Process? process = null;

    //private readonly ProcessJobTracker jobTracker = new ProcessJobTracker();
    private readonly string workingDirectory;

    public InteractiveService(string workingDirectory)
    {
        this.workingDirectory = workingDirectory;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public event EventHandler<CommandFailed>? CommandFailed;

    private async Task<Kernel> CreateKernelAsync(string workingDirectory, CancellationToken ct = default)
    {
        try
        {
            var url = KernelHost.CreateHostUriForCurrentProcessId();
            var compositeKernel = new CompositeKernel("cbcomposite");

            var cmd = new[]
            {
                "dotnet",
                "tool",
                "run",
                "dotnet-interactive",
                $"[cb-{Process.GetCurrentProcess().Id}]",
                "stdio",

                //"--default-kernel",
                //"csharp",
                "--working-dir",
                $@"""{workingDirectory}"""
            };

            var connector = new StdIoKernelConnector(
                cmd,
                "root-proxy",
                url,
                new DirectoryInfo(workingDirectory));

            // Start the dotnet-interactive tool and get a proxy for the root composite kernel therein.
            using var rootProxyKernel = await connector.CreateRootProxyKernelAsync().ConfigureAwait(false);

            // Get proxies for each subkernel present inside the dotnet-interactive tool.
            var requestKernelInfoCommand = new RequestKernelInfo(rootProxyKernel.KernelInfo.RemoteUri);

            var result =
                await rootProxyKernel.SendAsync(
                    requestKernelInfoCommand,
                    ct).ConfigureAwait(false);

            var subKernels = result.Events.OfType<KernelInfoProduced>();

            foreach (var kernelInfoProduced in result.Events.OfType<KernelInfoProduced>())
            {
                var kernelInfo = kernelInfoProduced.KernelInfo;

                if (kernelInfo is not null && !kernelInfo.IsProxy && !kernelInfo.IsComposite)
                {
                    var proxyKernel = await connector.CreateProxyKernelAsync(kernelInfo).ConfigureAwait(false);
                    proxyKernel.SetUpValueSharingIfSupported();
                    compositeKernel.Add(proxyKernel);
                }
            }

            //compositeKernel.DefaultKernelName = "csharp";
            compositeKernel.Add(rootProxyKernel);

            compositeKernel.KernelEvents.Subscribe(OnKernelDiagnosticEventReceived);

            return compositeKernel;
        }
        catch (CommandLineInvocationException ex) when (ex.Message.Contains(
                                                            "Cannot find a tool in the manifest file that has a command named 'dotnet-interactive'"))
        {
            var success = RestoreDotnetInteractive();

            if (success)
            {
                return await CreateKernelAsync(workingDirectory, ct);
            }

            throw;
        }
    }

    public event EventHandler<DisplayEvent>? DisplayEvent;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                kernel?.Dispose();

                if (process != null)
                {
                    process.Kill();
                    process.Dispose();
                }
            }

            disposedValue = true;
        }
    }

    public event EventHandler<HoverTextProduced>? HoverTextProduced;

    public bool IsRunning() => kernel != null;

    private void OnKernelDiagnosticEventReceived(KernelEvent ke)
    {
        WriteLine("Receive data from kernel");
        WriteLine(KernelEventEnvelope.Serialize(ke));

        switch (ke)
        {
            case DisplayEvent de:
                DisplayEvent?.Invoke(this, de);
                break;
            case CommandFailed cf:
                CommandFailed?.Invoke(this, cf);
                break;
            case HoverTextProduced cf:
                HoverTextProduced?.Invoke(this, cf);
                break;
        }
    }

    public event EventHandler<string>? Output;

    private void PrintProcessOutput(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            WriteLine(e.Data);
        }
    }

    private bool RestoreDotnetInteractive()
    {
        WriteLine("Restore dotnet interactive tool");

        // write RestoreInteractive.config from embedded resource to this.workingDirectory
        var assembly = Assembly.GetAssembly(typeof(InteractiveService))!;
        var resourceName = "AgentChat.DotnetInteractiveFunction.RestoreInteractive.config";

        using (var stream = assembly.GetManifestResourceStream(resourceName)!)
        using (var fileStream = File.Create(Path.Combine(workingDirectory, "RestoreInteractive.config")))
        {
            stream.CopyTo(fileStream);
        }

        // write dotnet-tool.json from embedded resource to this.workingDirectory

        resourceName = "AgentChat.DotnetInteractiveFunction.dotnet-tools.json";

        using (var stream2 = assembly.GetManifestResourceStream(resourceName)!)
        using (var fileStream2 = File.Create(Path.Combine(workingDirectory, "dotnet-tools.json")))
        {
            stream2.CopyTo(fileStream2);
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "tool restore --configfile RestoreInteractive.config",
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.OutputDataReceived += PrintProcessOutput;
        process.ErrorDataReceived += PrintProcessOutput;
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        process.WaitForExit();

        return process.ExitCode == 0;
    }

    public async Task<bool> StartAsync(string workingDirectory, CancellationToken ct)
    {
        kernel = await CreateKernelAsync(workingDirectory, ct);
        return true;
    }

    public async Task<string?> SubmitCommandAsync(KernelCommand cmd, CancellationToken ct)
    {
        if (kernel == null)
        {
            throw new Exception("Kernel is not running");
        }

        try
        {
            var res = await kernel.SendAndThrowOnCommandFailedAsync(cmd, ct);
            var events = res.Events;

            var displayValues = events.Where(x =>
                    x is StandardErrorValueProduced || x is StandardOutputValueProduced || x is ReturnValueProduced)
                .SelectMany(x => (x as DisplayEvent)!.FormattedValues);

            if (displayValues is null || displayValues.Count() == 0)
            {
                return null;
            }

            return string.Join("\n", displayValues.Select(x => x.Value));
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<string?> SubmitCSharpCodeAsync(string code, CancellationToken ct)
    {
        var command = new SubmitCode(code, "csharp");
        return await SubmitCommandAsync(command, ct);
    }

    public async Task<string?> SubmitPowershellCodeAsync(string code, CancellationToken ct)
    {
        var command = new SubmitCode(code, "pwsh");
        return await SubmitCommandAsync(command, ct);
    }

    private void WriteLine(string data)
    {
        Output?.Invoke(this, data);
    }
}