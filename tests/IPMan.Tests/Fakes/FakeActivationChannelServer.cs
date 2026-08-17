using IPMan.Infrastructure.Common;

namespace IPMan.Tests.Fakes;

internal sealed class FakeActivationChannelServer : IActivationChannelServer
{
    private bool _isListening;

    public event EventHandler? ActivationRequested;

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public int ActivationCount { get; private set; }

    public void StartListening()
    {
        StartCount++;
        _isListening = true;
    }

    public void StopListening()
    {
        StopCount++;
        _isListening = false;
    }

    public void ReceiveActivationSignal()
    {
        if (!_isListening)
        {
            return;
        }

        ActivationCount++;
        ActivationRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => StopListening();
}
