using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using System.Text;

namespace GroupChatExample.DotnetInteractiveService
{
    internal class UTF8KernelCommandAndEventTextStreamSender : IKernelCommandAndEventSender
    {
        private readonly StreamWriter _writer;

        public UTF8KernelCommandAndEventTextStreamSender(StreamWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public async Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var str = KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand));
            var bytes = Encoding.UTF8.GetBytes(str);
            await _writer.BaseStream.WriteAsync(bytes, 0, bytes.Length);

            await _writer.WriteAsync(Delimiter);

            await _writer.FlushAsync();
        }

        public async Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var str = KernelEventEnvelope.Serialize(KernelEventEnvelope.Create(kernelEvent));
            var bytes = Encoding.UTF8.GetBytes(str);
            await _writer.BaseStream.WriteAsync(bytes, 0, bytes.Length);

            await _writer.WriteAsync(Delimiter);

            await _writer.FlushAsync();
        }

        public static string Delimiter => "\r\n";

        public Uri RemoteHostUri => throw new NotImplementedException();
    }
}
