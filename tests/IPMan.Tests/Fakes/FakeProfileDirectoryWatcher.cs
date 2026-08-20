using IPMan.Application.Profiles;

namespace IPMan.Tests.Fakes;

public sealed class FakeProfileDirectoryWatcher : IProfileDirectoryWatcher
{
    public event EventHandler? Changed;

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public int DisposeCount { get; private set; }

    public bool IsWatching => StartCount > StopCount;

    public void StartWatching() => StartCount++;

    public void StopWatching() => StopCount++;

    public void Dispose() => DisposeCount++;

    public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
