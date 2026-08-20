using System.IO;
using IPMan.Application.Profiles;
using IPMan.Domain.Profiles;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Profiles;

public sealed class ProfileCatalogTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task InitializeAsync_WhenRepositoryHasProfiles_LoadsSortedResults()
    {
        FakeProfileRepository repository = new()
        {
            LoadResult = new ProfileLoadResult(
                [
                    TestData.Profile(profileId: "other", name: "Alpha"),
                    TestData.Profile(profileId: "favorite", name: "Zulu", isFavorite: true)
                ],
                [])
        };
        using ProfileCatalog catalog = new(repository, new FakeProfileDirectoryWatcher());

        await catalog.InitializeAsync(CancellationToken.None);

        Assert.Equal(
            ["favorite", "other"],
            catalog.Profiles.Select(profile => profile.ProfileId));
    }

    [Fact]
    public async Task WatcherChanged_WhenCatalogIsWatching_ReloadsAndRaisesChanged()
    {
        FakeProfileRepository repository = new();
        FakeProfileDirectoryWatcher watcher = new();
        using ProfileCatalog catalog = new(repository, watcher);
        await catalog.InitializeAsync(CancellationToken.None);
        repository.LoadResult = new ProfileLoadResult(
            [TestData.Profile(profileId: "reloaded", name: "Reloaded")],
            []);
        TaskCompletionSource changed = NewCompletionSource();
        catalog.Changed += (_, _) => changed.TrySetResult();

        catalog.StartWatching();
        watcher.RaiseChanged();
        await changed.Task.WaitAsync(Timeout);

        Assert.Equal(2, repository.LoadCalls.Count);
        Assert.Equal("reloaded", Assert.Single(catalog.Profiles).ProfileId);
    }

    [Fact]
    public async Task WatcherChanged_WhenReloadFails_PreservesPreviousProfilesAndRaisesChanged()
    {
        NetworkProfile previous = TestData.Profile(profileId: "previous", name: "Previous");
        FakeProfileRepository repository = new()
        {
            LoadResult = new ProfileLoadResult([previous], [])
        };
        FakeProfileDirectoryWatcher watcher = new();
        using ProfileCatalog catalog = new(repository, watcher);
        await catalog.InitializeAsync(CancellationToken.None);
        repository.LoadAsyncOverride = _ =>
            Task.FromException<ProfileLoadResult>(new IOException("Read failed."));
        TaskCompletionSource changed = NewCompletionSource();
        catalog.Changed += (_, _) => changed.TrySetResult();

        catalog.StartWatching();
        watcher.RaiseChanged();
        await changed.Task.WaitAsync(Timeout);

        Assert.Same(previous, Assert.Single(catalog.Profiles));
    }

    [Fact]
    public async Task InitializeAsync_WhenRepositoryReportsProblems_ExposesProblems()
    {
        NetworkProfileProblem problem = new(
            "broken.json",
            ProfileLoadFailureKind.MalformedJson);
        FakeProfileRepository repository = new()
        {
            LoadResult = new ProfileLoadResult([], [problem])
        };
        using ProfileCatalog catalog = new(repository, new FakeProfileDirectoryWatcher());

        await catalog.InitializeAsync(CancellationToken.None);

        Assert.Same(problem, Assert.Single(catalog.Problems));
    }

    [Fact]
    public async Task SaveAsync_WhenRepositoryCompletes_DelegatesAndRefreshes()
    {
        NetworkProfile saved = TestData.Profile(profileId: "saved", name: "Saved");
        FakeProfileRepository repository = new()
        {
            SaveResult = ProfileSaveResult.Success(saved),
            LoadResult = new ProfileLoadResult([saved], [])
        };
        using ProfileCatalog catalog = new(repository, new FakeProfileDirectoryWatcher());

        ProfileSaveResult result = await catalog.SaveAsync(saved, CancellationToken.None);

        Assert.Same(saved, Assert.Single(repository.SavedProfiles));
        Assert.Same(repository.SaveResult, result);
        Assert.Same(saved, Assert.Single(catalog.Profiles));
        Assert.Single(repository.LoadCalls);
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryCompletes_DelegatesAndRefreshes()
    {
        FakeProfileRepository repository = new()
        {
            DeleteResult = ProfileDeleteResult.Success(),
            LoadResult = new ProfileLoadResult([], [])
        };
        using ProfileCatalog catalog = new(repository, new FakeProfileDirectoryWatcher());

        ProfileDeleteResult result = await catalog.DeleteAsync(
            "profile-to-delete",
            CancellationToken.None);

        Assert.Equal("profile-to-delete", Assert.Single(repository.DeletedProfileIds));
        Assert.Same(repository.DeleteResult, result);
        Assert.Empty(catalog.Profiles);
        Assert.Single(repository.LoadCalls);
    }

    [Fact]
    public void WatcherChanged_AfterStopWatching_DoesNotReload()
    {
        FakeProfileRepository repository = new();
        FakeProfileDirectoryWatcher watcher = new();
        using ProfileCatalog catalog = new(repository, watcher);
        catalog.StartWatching();
        catalog.StopWatching();

        watcher.RaiseChanged();

        Assert.Empty(repository.LoadCalls);
        Assert.Equal(1, watcher.StopCount);
    }

    [Fact]
    public void WatcherChanged_AfterDispose_DoesNotReload()
    {
        FakeProfileRepository repository = new();
        FakeProfileDirectoryWatcher watcher = new();
        ProfileCatalog catalog = new(repository, watcher);
        catalog.StartWatching();
        catalog.Dispose();

        watcher.RaiseChanged();

        Assert.Empty(repository.LoadCalls);
        Assert.Equal(1, watcher.DisposeCount);
    }

    [Fact]
    public async Task WatcherChanged_WhenTwoReloadsAreQueued_DoesNotLoadConcurrently()
    {
        FakeProfileRepository repository = new();
        FakeProfileDirectoryWatcher watcher = new();
        using ProfileCatalog catalog = new(repository, watcher);
        TaskCompletionSource firstLoadStarted = NewCompletionSource();
        TaskCompletionSource releaseFirstLoad = NewCompletionSource();
        TaskCompletionSource secondLoadCompleted = NewCompletionSource();
        int activeLoads = 0;
        int maximumActiveLoads = 0;
        int loadNumber = 0;
        repository.LoadAsyncOverride = async cancellationToken =>
        {
            int active = Interlocked.Increment(ref activeLoads);
            UpdateMaximum(ref maximumActiveLoads, active);
            int currentLoad = Interlocked.Increment(ref loadNumber);

            try
            {
                if (currentLoad == 1)
                {
                    firstLoadStarted.TrySetResult();
                    await releaseFirstLoad.Task.WaitAsync(cancellationToken);
                }

                return new ProfileLoadResult([], []);
            }
            finally
            {
                Interlocked.Decrement(ref activeLoads);

                if (currentLoad == 2)
                {
                    secondLoadCompleted.TrySetResult();
                }
            }
        };

        catalog.StartWatching();
        watcher.RaiseChanged();
        await firstLoadStarted.Task.WaitAsync(Timeout);
        watcher.RaiseChanged();
        releaseFirstLoad.TrySetResult();
        await secondLoadCompleted.Task.WaitAsync(Timeout);

        Assert.Equal(2, repository.LoadCalls.Count);
        Assert.Equal(1, maximumActiveLoads);
    }

    private static TaskCompletionSource NewCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        int observed;

        do
        {
            observed = Volatile.Read(ref maximum);

            if (candidate <= observed)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref maximum, candidate, observed) != observed);
    }
}
