using System.Collections;
using System.Collections.Immutable;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;

namespace AgentChat.DotnetInteractiveService;

public static class ObservableExtensions
{
    public static SubscribedList<T> ToSubscribedList<T>(this IObservable<T> source) => new(source);
}

public static class KernelExtensions
{
    private static Exception GetException(this CommandFailed commandFailedEvent) => new(commandFailedEvent.Message);

    internal static async Task<KernelCommandResult> SendAndThrowOnCommandFailedAsync(
        this Kernel kernel,
        KernelCommand command,
        CancellationToken cancellationToken)
    {
        var result = await kernel.SendAsync(command, cancellationToken);
        result.ThrowOnCommandFailed();
        return result;
    }

    internal static void SetUpValueSharingIfSupported(this ProxyKernel proxyKernel)
    {
        var supportedCommands = proxyKernel.KernelInfo.SupportedKernelCommands;

        if (supportedCommands.Any(d => d.Name == nameof(RequestValue)) &&
            supportedCommands.Any(d => d.Name == nameof(SendValue)))
        {
            proxyKernel.UseValueSharing();
        }
    }

    private static void ThrowOnCommandFailed(this KernelCommandResult result)
    {
        var failedEvents = result.Events.OfType<CommandFailed>();

        if (!failedEvents.Any())
        {
            return;
        }

        if (failedEvents.Skip(1).Any())
        {
            var innerExceptions = failedEvents.Select(f => f.GetException());
            throw new AggregateException(innerExceptions);
        }

        throw failedEvents.Single().GetException();
    }
}

public class SubscribedList<T> : IReadOnlyList<T>, IDisposable
{
    private readonly IDisposable _subscription;

    private ImmutableArray<T> _list = ImmutableArray<T>.Empty;

    public SubscribedList(IObservable<T> source)
    {
        _subscription = source.Subscribe(x => _list = _list.Add(x));
    }

    public void Dispose() => _subscription.Dispose();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_list).GetEnumerator();

    public int Count => _list.Length;

    public T this[int index] => _list[index];
}