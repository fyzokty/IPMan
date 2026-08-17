using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace IPMan.Infrastructure.Common;

/// <summary>Receives the minimal activation signal over an administrator-only pipe.</summary>
internal sealed class NamedPipeActivationChannelServer : IActivationChannelServer
{
    private readonly object _sync = new();

    private CancellationTokenSource? _cancellation;
    private bool _isDisposed;

    public event EventHandler? ActivationRequested;

    public void StartListening()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (_cancellation is not null)
            {
                return;
            }

            CancellationTokenSource cancellation = new();
            _cancellation = cancellation;
            _ = Task.Run(
                () => RunAsync(cancellation.Token),
                CancellationToken.None);
        }
    }

    public void StopListening()
    {
        CancellationTokenSource? cancellation;

        lock (_sync)
        {
            cancellation = _cancellation;
            _cancellation = null;
        }

        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
        }

        StopListening();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using NamedPipeServerStream pipe = CreatePipe();

                // Cancellation disposes the active generation's pending pipe,
                // which reliably breaks both accept and read during teardown.
                using CancellationTokenRegistration registration = cancellationToken.Register(
                    static state => ((NamedPipeServerStream)state!).Dispose(),
                    pipe);

                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);

                byte[] message = new byte[1];
                int bytesRead = await pipe
                    .ReadAsync(message, cancellationToken)
                    .ConfigureAwait(false);

                if (bytesRead == 1 &&
                    message[0] == ActivationChannelProtocol.ActivationSignal)
                {
                    RaiseActivationRequested();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (IOException)
            {
                // A client may disconnect midway through the one-byte message.
                // The next loop iteration accepts a fresh connection.
            }
        }
    }

    private void RaiseActivationRequested()
    {
        try
        {
            ActivationRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            // A faulty subscriber must not permanently terminate IPC listening.
        }
    }

    private static NamedPipeServerStream CreatePipe()
    {
        SecurityIdentifier administrators = new(
            WellKnownSidType.BuiltinAdministratorsSid,
            domainSid: null);
        PipeSecurity security = new();

        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(
            new PipeAccessRule(
                administrators,
                PipeAccessRights.ReadWrite,
                AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            ActivationChannelProtocol.PipeName,
            PipeDirection.In,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous,
            inBufferSize: 0,
            outBufferSize: 0,
            security);
    }
}
