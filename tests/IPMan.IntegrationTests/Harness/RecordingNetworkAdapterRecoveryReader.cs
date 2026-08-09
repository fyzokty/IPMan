using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.IntegrationTests.Harness;

/// <summary>
/// Harness-only decorator that records the first exact recovery read performed
/// by one production Apply transaction. Later verification reads cannot replace it.
/// </summary>
public sealed class RecordingNetworkAdapterRecoveryReader : INetworkAdapterRecoveryReader
{
    private readonly INetworkAdapterRecoveryReader _inner;
    private readonly object _sync = new();
    private bool _captureActive;
    private NetworkAdapterRecoveryReadResult? _capturedRead;

    public RecordingNetworkAdapterRecoveryReader(INetworkAdapterRecoveryReader inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public void BeginApplyCapture()
    {
        lock (_sync)
        {
            if (_captureActive)
            {
                throw new InvalidOperationException("An Apply recovery capture is already active.");
            }

            _capturedRead = null;
            _captureActive = true;
        }
    }

    public NetworkAdapterRecoveryReadResult? EndApplyCapture()
    {
        lock (_sync)
        {
            if (!_captureActive)
            {
                throw new InvalidOperationException("No Apply recovery capture is active.");
            }

            _captureActive = false;
            return _capturedRead;
        }
    }

    public async Task<NetworkAdapterRecoveryReadResult> ReadAsync(
        NetworkAdapterId adapterId,
        CancellationToken cancellationToken)
    {
        NetworkAdapterRecoveryReadResult result = await _inner
            .ReadAsync(adapterId, cancellationToken)
            .ConfigureAwait(false);

        lock (_sync)
        {
            if (_captureActive && _capturedRead is null)
            {
                _capturedRead = result;
            }
        }

        return result;
    }
}
