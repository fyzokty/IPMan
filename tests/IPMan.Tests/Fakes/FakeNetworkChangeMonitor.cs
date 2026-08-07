using IPMan.Application.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeNetworkChangeMonitor : INetworkChangeMonitor
{
    public event EventHandler<NetworkEnvironmentChangedEventArgs>? Changed;

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public int DisposeCount { get; private set; }

    public bool IsMonitoring => StartCount > StopCount;

    public void StartMonitoring() => StartCount++;

    public void StopMonitoring() => StopCount++;

    public void Dispose() => DisposeCount++;

    public void RaiseChanged(string reason) =>
        Changed?.Invoke(this, new NetworkEnvironmentChangedEventArgs(reason));
}
