using IPMan.Infrastructure.Profiles;

namespace IPMan.Tests.Fakes;

internal sealed class FakeProfileDirectoryEventSource : IProfileDirectoryEventSource
{
    private readonly List<Action> _handlers = new();

    public int SubscribeCount { get; private set; }

    public int UnsubscribeCount { get; private set; }

    public int DisposeCount { get; private set; }

    public int ActiveSubscriptions => _handlers.Count;

    public void Subscribe(Action onDirectoryChanged)
    {
        SubscribeCount++;
        _handlers.Add(onDirectoryChanged);
    }

    public void Unsubscribe(Action onDirectoryChanged)
    {
        UnsubscribeCount++;
        _handlers.Remove(onDirectoryChanged);
    }

    public void Dispose()
    {
        DisposeCount++;
        _handlers.Clear();
    }

    public void RaiseChanged()
    {
        foreach (Action handler in _handlers.ToArray())
        {
            handler();
        }
    }
}
