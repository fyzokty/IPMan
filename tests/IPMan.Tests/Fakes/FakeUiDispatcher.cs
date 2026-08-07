using IPMan.App.Presentation;

namespace IPMan.Tests.Fakes;

/// <summary>
/// Runs posted work immediately on the calling thread, so ViewModel behaviour
/// can be asserted without a WPF dispatcher.
/// </summary>
public sealed class FakeUiDispatcher : IUiDispatcher
{
    public int PostCount { get; private set; }

    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        PostCount++;
        action();
    }
}
