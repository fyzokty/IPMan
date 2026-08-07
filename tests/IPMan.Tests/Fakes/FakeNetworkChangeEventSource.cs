using IPMan.Infrastructure.Networking;

namespace IPMan.Tests.Fakes;

/// <summary>
/// Stands in for the static .NET <c>NetworkChange</c> events so subscription
/// lifecycle can be asserted.
/// </summary>
internal sealed class FakeNetworkChangeEventSource : INetworkChangeEventSource
{
    private readonly List<Action<string>> _handlers = new();

    public int SubscribeCount { get; private set; }

    public int UnsubscribeCount { get; private set; }

    public int ActiveSubscriptions => _handlers.Count;

    public void Subscribe(Action<string> onNetworkChanged)
    {
        SubscribeCount++;
        _handlers.Add(onNetworkChanged);
    }

    public void Unsubscribe(Action<string> onNetworkChanged)
    {
        UnsubscribeCount++;
        _handlers.Remove(onNetworkChanged);
    }

    public void RaiseNetworkChanged(string reason)
    {
        foreach (Action<string> handler in _handlers.ToArray())
        {
            handler(reason);
        }
    }
}
