namespace IPMan.Infrastructure.Networking;

/// <summary>
/// Smallest abstraction over the static .NET <c>NetworkChange</c> events so the
/// monitor's subscribe/unsubscribe lifecycle can be tested deterministically.
/// Intentionally internal: it is not part of the application architecture.
/// </summary>
internal interface INetworkChangeEventSource
{
    /// <summary>Attaches the handler. Attaching twice must not subscribe twice.</summary>
    void Subscribe(Action<string> onNetworkChanged);

    /// <summary>Detaches the handler. Safe when nothing is attached.</summary>
    void Unsubscribe(Action<string> onNetworkChanged);
}
