using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class AdapterRefreshCoordinatorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>Used where the expected outcome is "nothing happens".</summary>
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(200);

    [Fact]
    public async Task StartCoordinating_StartsMonitoringAndPublishesInitialDiscovery()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();
        harness.Reader.EnqueueResult(TestData.Snapshot(id: "{A}"));

        harness.Coordinator.StartCoordinating();
        AdapterRefreshedEventArgs refresh = await harness.RunPassAsync();

        Assert.Equal(1, harness.Monitor.StartCount);
        Assert.Equal(NetworkChangeReason.InitialDiscovery, refresh.Reason);
        Assert.Equal("{A}", Assert.Single(refresh.Adapters).Id.Value);
        Assert.Single(refresh.Added);
    }

    [Fact]
    public async Task StartCoordinating_WhenCalledTwice_DoesNotStartMonitoringTwice()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.StartCoordinating();
        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        Assert.Equal(1, harness.Monitor.StartCount);
    }

    [Fact]
    public async Task NetworkEvents_ArrivingAsABurst_ProduceASingleDiscoveryPass()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.StartCoordinating();

        // The initial discovery is waiting inside the debounce window; a burst of
        // Windows events arriving now must be merged into that single pass.
        Assert.True(await harness.WaitForDebounceAsync());

        for (int i = 0; i < 5; i++)
        {
            harness.Monitor.RaiseChanged(NetworkChangeReason.NetworkAddressChanged);
        }

        harness.ReleaseDebounce();
        await harness.WaitForRefreshAsync();

        Assert.Equal(1, harness.Reader.ReadCount);
        Assert.Equal(1, harness.RefreshCount);
    }

    [Fact]
    public async Task NetworkEvent_IsPublishedWithTheReasonReportedByWindows()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        harness.Monitor.RaiseChanged(NetworkChangeReason.NetworkAvailabilityChanged);
        AdapterRefreshedEventArgs refresh = await harness.RunPassAsync();

        Assert.Equal(NetworkChangeReason.NetworkAvailabilityChanged, refresh.Reason);
    }

    [Fact]
    public async Task Refresh_WhenAdapterIsAdded_ReportsItAsAdded()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();
        harness.Reader.EnqueueResult(TestData.Snapshot(id: "{A}"));
        harness.Reader.EnqueueResult(TestData.Snapshot(id: "{A}"), TestData.Snapshot(id: "{USB}"));

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        harness.Monitor.RaiseChanged(NetworkChangeReason.NetworkAddressChanged);
        AdapterRefreshedEventArgs refresh = await harness.RunPassAsync();

        Assert.Equal("{USB}", Assert.Single(refresh.Added).Id.Value);
        Assert.Empty(refresh.Removed);
        Assert.Empty(refresh.Changed);
    }

    [Fact]
    public async Task Refresh_WhenAdapterIsRemoved_ReportsItAsRemoved()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();
        harness.Reader.EnqueueResult(TestData.Snapshot(id: "{A}"), TestData.Snapshot(id: "{USB}"));
        harness.Reader.EnqueueResult(TestData.Snapshot(id: "{A}"));

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        harness.Monitor.RaiseChanged(NetworkChangeReason.NetworkAddressChanged);
        AdapterRefreshedEventArgs refresh = await harness.RunPassAsync();

        Assert.Equal("{USB}", Assert.Single(refresh.Removed).Id.Value);
        Assert.DoesNotContain(refresh.Adapters, adapter => adapter.Id.Value == "{USB}");
    }

    [Fact]
    public async Task Refresh_WhenAdapterStateChanges_ReportsItAsChanged()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();
        harness.Reader.EnqueueResult(TestData.Snapshot(id: "{A}", isConnected: true));
        harness.Reader.EnqueueResult(TestData.Snapshot(id: "{A}", isConnected: false));

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        harness.Monitor.RaiseChanged(NetworkChangeReason.NetworkAvailabilityChanged);
        AdapterRefreshedEventArgs refresh = await harness.RunPassAsync();

        NetworkAdapterSnapshot changed = Assert.Single(refresh.Changed);
        Assert.False(changed.IsConnected);
        Assert.Empty(refresh.Added);
        Assert.Empty(refresh.Removed);
    }

    [Fact]
    public async Task Refresh_WhenNothingChanged_ReportsNoDifferences()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();
        harness.Reader.EnqueueResult(TestData.Snapshot(id: "{A}"));

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        harness.Monitor.RaiseChanged(NetworkChangeReason.NetworkAddressChanged);
        AdapterRefreshedEventArgs refresh = await harness.RunPassAsync();

        Assert.Empty(refresh.Added);
        Assert.Empty(refresh.Removed);
        Assert.Empty(refresh.Changed);
    }

    [Fact]
    public async Task Refresh_WhenNoAdaptersExist_PublishesEmptyResult()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.StartCoordinating();
        AdapterRefreshedEventArgs refresh = await harness.RunPassAsync();

        Assert.Empty(refresh.Adapters);
        Assert.Empty(refresh.Added);
    }

    [Fact]
    public async Task Refresh_WhenDiscoveryFails_ReportsFailureAndKeepsObserving()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();
        harness.Reader.FailWith(new InvalidOperationException("Windows read failed."));

        harness.Coordinator.StartCoordinating();
        Assert.True(await harness.WaitForDebounceAsync());
        harness.ReleaseDebounce();

        AdapterRefreshFailedEventArgs failure = await harness.WaitForFailureAsync();
        Assert.Equal(NetworkChangeReason.InitialDiscovery, failure.Reason);

        harness.Reader.StopFailing();
        harness.Reader.EnqueueResult(TestData.Snapshot(id: "{A}"));
        harness.Monitor.RaiseChanged(NetworkChangeReason.NetworkAddressChanged);

        AdapterRefreshedEventArgs refresh = await harness.RunPassAsync();
        Assert.Single(refresh.Adapters);
    }

    [Fact]
    public async Task StopCoordinating_StopsMonitoringAndIgnoresFurtherRequests()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        harness.Coordinator.StopCoordinating();
        harness.Coordinator.RequestRefresh(NetworkChangeReason.ManualRefresh);

        Assert.Equal(1, harness.Monitor.StopCount);
        Assert.False(harness.Monitor.IsMonitoring);
        Assert.False(await harness.WaitForDebounceAsync(ShortTimeout));
        Assert.Equal(1, harness.Reader.ReadCount);
    }

    [Fact]
    public void StopCoordinating_WhenNeverStarted_IsSafe()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.StopCoordinating();
        harness.Coordinator.StopCoordinating();

        Assert.Equal(0, harness.Monitor.StopCount);
    }

    [Fact]
    public async Task StartCoordinating_AfterStop_ResumesObservation()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();
        harness.Coordinator.StopCoordinating();

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        Assert.Equal(2, harness.Monitor.StartCount);
        Assert.Equal(2, harness.Reader.ReadCount);
    }

    [Fact]
    public async Task Dispose_StopsMonitoring()
    {
        CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        harness.Dispose();

        Assert.Equal(1, harness.Monitor.StopCount);
        Assert.False(harness.Monitor.IsMonitoring);
    }

    [Fact]
    public void RequestRefresh_BeforeStart_IsIgnored()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.RequestRefresh(NetworkChangeReason.ManualRefresh);

        Assert.Equal(0, harness.Delay.TotalDelayCount);
        Assert.Equal(0, harness.Reader.ReadCount);
    }

    [Fact]
    public async Task ReconciliationTimer_WhenNoWindowsEventArrives_TriggersADiscoveryPass()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        Assert.True(await harness.WaitForReconciliationTimerAsync());
        harness.FireReconciliationTimer();

        AdapterRefreshedEventArgs refresh = await harness.RunPassAsync();

        Assert.Equal(NetworkChangeReason.ScheduledReconciliation, refresh.Reason);
        Assert.Equal(2, harness.Reader.ReadCount);
    }

    [Fact]
    public async Task ReconciliationTimer_WhileAPassIsPending_IsCoalescedIntoIt()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.StartCoordinating();

        // Initial discovery is inside its debounce window.
        Assert.True(await harness.WaitForDebounceAsync());
        Assert.True(await harness.WaitForReconciliationTimerAsync());
        harness.FireReconciliationTimer();

        harness.ReleaseDebounce();
        await harness.WaitForRefreshAsync();

        // The reconciliation request merged into the pending pass instead of
        // starting a second, overlapping discovery.
        Assert.Equal(1, harness.Reader.ReadCount);
        Assert.Equal(1, harness.RefreshCount);
    }

    [Fact]
    public async Task ReconciliationTimer_AfterStop_DoesNotRequestDiscovery()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create();

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        Assert.True(await harness.WaitForReconciliationTimerAsync());
        harness.Coordinator.StopCoordinating();
        harness.FireReconciliationTimer();

        Assert.False(await harness.WaitForDebounceAsync(ShortTimeout));
        Assert.Equal(1, harness.Reader.ReadCount);
    }

    [Fact]
    public async Task Reconciliation_WhenIntervalIsNotPositive_IsDisabled()
    {
        using CoordinatorHarness harness = CoordinatorHarness.Create(TimeSpan.Zero);

        harness.Coordinator.StartCoordinating();
        await harness.RunPassAsync();

        Assert.False(await harness.WaitForReconciliationTimerAsync(ShortTimeout));
    }

    [Fact]
    public void Constructor_WhenReconciliationIntervalIsSubSecond_Throws()
    {
        using FakeDelayProvider delay = new();
        FakeNetworkChangeMonitor monitor = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => new AdapterRefreshCoordinator(
            new FakeNetworkAdapterReader(),
            monitor,
            delay,
            new AdapterRefreshCoordinatorOptions
            {
                ReconciliationInterval = TimeSpan.FromMilliseconds(500)
            }));
    }

    [Fact]
    public void DefaultOptions_UseEventDrivenRefreshWithALowFrequencyFallback()
    {
        AdapterRefreshCoordinatorOptions options = new();

        Assert.Equal(TimeSpan.FromMilliseconds(300), options.DebounceWindow);
        Assert.Equal(TimeSpan.FromSeconds(15), options.ReconciliationInterval);
        Assert.True(options.ReconciliationInterval >= AdapterRefreshCoordinator.MinimumReconciliationInterval);
    }

    private sealed class CoordinatorHarness : IDisposable
    {
        private readonly object _sync = new();
        private readonly SemaphoreSlim _refreshReceived = new(0);
        private readonly SemaphoreSlim _failureReceived = new(0);
        private readonly List<AdapterRefreshedEventArgs> _refreshes = new();
        private readonly List<AdapterRefreshFailedEventArgs> _failures = new();

        private CoordinatorHarness(
            FakeNetworkAdapterReader reader,
            FakeNetworkChangeMonitor monitor,
            FakeDelayProvider delay,
            AdapterRefreshCoordinator coordinator)
        {
            Reader = reader;
            Monitor = monitor;
            Delay = delay;
            Coordinator = coordinator;

            coordinator.Refreshed += OnRefreshed;
            coordinator.RefreshFailed += OnRefreshFailed;
        }

        public FakeNetworkAdapterReader Reader { get; }

        public FakeNetworkChangeMonitor Monitor { get; }

        public FakeDelayProvider Delay { get; }

        public AdapterRefreshCoordinator Coordinator { get; }

        public int RefreshCount
        {
            get
            {
                lock (_sync)
                {
                    return _refreshes.Count;
                }
            }
        }

        public static TimeSpan DebounceWindow => TimeSpan.FromMilliseconds(300);

        public static TimeSpan ReconciliationInterval => TimeSpan.FromSeconds(15);

        public static CoordinatorHarness Create(TimeSpan? reconciliationInterval = null)
        {
            FakeNetworkAdapterReader reader = new();
            FakeNetworkChangeMonitor monitor = new();
            FakeDelayProvider delay = new();

            AdapterRefreshCoordinator coordinator = new(
                reader,
                monitor,
                delay,
                new AdapterRefreshCoordinatorOptions
                {
                    DebounceWindow = DebounceWindow,
                    ReconciliationInterval = reconciliationInterval ?? ReconciliationInterval
                });

            return new CoordinatorHarness(reader, monitor, delay, coordinator);
        }

        /// <summary>Releases the pending debounce window and awaits the published result.</summary>
        public async Task<AdapterRefreshedEventArgs> RunPassAsync()
        {
            Assert.True(await WaitForDebounceAsync());
            ReleaseDebounce();
            return await WaitForRefreshAsync();
        }

        public Task<bool> WaitForDebounceAsync(TimeSpan? timeout = null) =>
            Delay.WaitForDelayStartedAsync(DebounceWindow, timeout ?? Timeout);

        public void ReleaseDebounce() => Delay.ReleaseDelays(DebounceWindow);

        public Task<bool> WaitForReconciliationTimerAsync(TimeSpan? timeout = null) =>
            Delay.WaitForDelayStartedAsync(ReconciliationInterval, timeout ?? Timeout);

        /// <summary>Simulates the reconciliation interval elapsing.</summary>
        public void FireReconciliationTimer() => Delay.ReleaseDelays(ReconciliationInterval);

        public async Task<AdapterRefreshedEventArgs> WaitForRefreshAsync()
        {
            Assert.True(await _refreshReceived.WaitAsync(Timeout));

            lock (_sync)
            {
                return _refreshes[^1];
            }
        }

        public async Task<AdapterRefreshFailedEventArgs> WaitForFailureAsync()
        {
            Assert.True(await _failureReceived.WaitAsync(Timeout));

            lock (_sync)
            {
                return _failures[^1];
            }
        }

        public void Dispose()
        {
            Coordinator.Refreshed -= OnRefreshed;
            Coordinator.RefreshFailed -= OnRefreshFailed;
            Coordinator.Dispose();
            Delay.Dispose();
            _refreshReceived.Dispose();
            _failureReceived.Dispose();
        }

        private void OnRefreshed(object? sender, AdapterRefreshedEventArgs e)
        {
            lock (_sync)
            {
                _refreshes.Add(e);
            }

            _refreshReceived.Release();
        }

        private void OnRefreshFailed(object? sender, AdapterRefreshFailedEventArgs e)
        {
            lock (_sync)
            {
                _failures.Add(e);
            }

            _failureReceived.Release();
        }
    }
}
