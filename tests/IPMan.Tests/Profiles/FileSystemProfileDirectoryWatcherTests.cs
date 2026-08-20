using IPMan.Infrastructure.Profiles;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Profiles;

public sealed class FileSystemProfileDirectoryWatcherTests
{
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task RawChanges_WhenBurstOccurs_RaisesOneDebouncedChangedEvent()
    {
        FakeProfileDirectoryEventSource source = new();
        using FakeDelayProvider delayProvider = new();
        using FileSystemProfileDirectoryWatcher watcher = new(
            source,
            delayProvider,
            new ProfileWatcherOptions { DebounceWindow = DebounceWindow });
        int changedCount = 0;
        TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.Changed += (_, _) =>
        {
            Interlocked.Increment(ref changedCount);
            changed.TrySetResult();
        };

        watcher.StartWatching();

        for (int index = 0; index < 10; index++)
        {
            source.RaiseChanged();
        }

        Assert.True(await delayProvider.WaitForDelayStartedAsync(DebounceWindow, Timeout));
        delayProvider.ReleaseDelays(DebounceWindow);
        await changed.Task.WaitAsync(Timeout);

        Assert.Equal(1, changedCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenDebounceWindowIsNotPositive_Throws(int milliseconds)
    {
        FakeProfileDirectoryEventSource source = new();
        using FakeDelayProvider delayProvider = new();
        ProfileWatcherOptions options = new()
        {
            DebounceWindow = TimeSpan.FromMilliseconds(milliseconds)
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FileSystemProfileDirectoryWatcher(source, delayProvider, options));
    }

    [Fact]
    public void Constructor_WhenDebounceWindowExceedsMaximum_Throws()
    {
        FakeProfileDirectoryEventSource source = new();
        using FakeDelayProvider delayProvider = new();
        ProfileWatcherOptions options = new()
        {
            DebounceWindow = FileSystemProfileDirectoryWatcher.MaximumDebounceWindow +
                TimeSpan.FromMilliseconds(1)
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new FileSystemProfileDirectoryWatcher(source, delayProvider, options));
    }
}
