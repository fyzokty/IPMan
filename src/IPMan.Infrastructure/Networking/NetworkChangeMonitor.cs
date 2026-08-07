using IPMan.Application.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Event-driven network observation (ADR-004, ADR-010). There is no polling
/// loop: notifications originate from the operating system.
/// </summary>
public sealed class NetworkChangeMonitor : INetworkChangeMonitor
{
    private readonly INetworkChangeEventSource _eventSource;
    private readonly Action<string> _handler;
    private readonly object _sync = new();

    private bool _isMonitoring;
    private bool _isDisposed;

    public NetworkChangeMonitor()
        : this(new SystemNetworkChangeEventSource())
    {
    }

    internal NetworkChangeMonitor(INetworkChangeEventSource eventSource)
    {
        ArgumentNullException.ThrowIfNull(eventSource);

        _eventSource = eventSource;
        _handler = OnNetworkChanged;
    }

    public event EventHandler<NetworkEnvironmentChangedEventArgs>? Changed;

    public void StartMonitoring()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (_isMonitoring)
            {
                return;
            }

            _eventSource.Subscribe(_handler);
            _isMonitoring = true;
        }
    }

    public void StopMonitoring()
    {
        lock (_sync)
        {
            if (!_isMonitoring)
            {
                return;
            }

            _eventSource.Unsubscribe(_handler);
            _isMonitoring = false;
        }
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

            if (_isMonitoring)
            {
                _eventSource.Unsubscribe(_handler);
                _isMonitoring = false;
            }
        }
    }

    private void OnNetworkChanged(string reason) =>
        Changed?.Invoke(this, new NetworkEnvironmentChangedEventArgs(reason));
}
