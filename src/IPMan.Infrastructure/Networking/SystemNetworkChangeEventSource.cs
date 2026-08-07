using System.Net.NetworkInformation;
using IPMan.Application.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Wraps the static <see cref="NetworkChange"/> events (ADR-010).
/// <para>
/// Handlers are attached to the static events only while a subscriber exists, so
/// no static subscription can outlive the monitor that created it.
/// </para>
/// </summary>
internal sealed class SystemNetworkChangeEventSource : INetworkChangeEventSource
{
    private readonly object _sync = new();

    private Action<string>? _handler;

    public void Subscribe(Action<string> onNetworkChanged)
    {
        ArgumentNullException.ThrowIfNull(onNetworkChanged);

        lock (_sync)
        {
            if (_handler is not null)
            {
                return;
            }

            _handler = onNetworkChanged;

            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }
    }

    public void Unsubscribe(Action<string> onNetworkChanged)
    {
        ArgumentNullException.ThrowIfNull(onNetworkChanged);

        lock (_sync)
        {
            if (_handler is null)
            {
                return;
            }

            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;

            _handler = null;
        }
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e) =>
        Notify(NetworkChangeReason.NetworkAddressChanged);

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) =>
        Notify(NetworkChangeReason.NetworkAvailabilityChanged);

    private void Notify(string reason)
    {
        Action<string>? handler;

        lock (_sync)
        {
            handler = _handler;
        }

        handler?.Invoke(reason);
    }
}
