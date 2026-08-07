namespace IPMan.Application.Networking;

public interface INetworkChangeMonitor : IDisposable
{
    event EventHandler<NetworkEnvironmentChangedEventArgs>? Changed;

    void StartMonitoring();

    void StopMonitoring();
}
