using System.IO;
using IPMan.Application.Logging;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class FirstRunBackupServiceTests
{
    private static readonly string[] DnsServers = { "1.1.1.1", "8.8.8.8" };
    private static readonly string[] Gateways = { "10.0.0.1", "10.0.0.254" };
    private readonly FakeNetworkAdapterReader _reader = new();
    private readonly FakeNetworkAdapterRecoveryReader _recoveryReader = new();
    private readonly FakeFirstRunBackupStore _store = new();
    private readonly FakeCriticalLogger _logger = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task CaptureAsync_WhenFileExists_DoesNotReadOrWrite()
    {
        _store.HasBackup = true;

        await CaptureAsync();

        Assert.Equal(0, _reader.ReadCount);
        Assert.Equal(0, _recoveryReader.ReadCount);
        Assert.Equal(0, _store.SaveCount);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task CaptureAsync_WhenFirstRun_PreservesEveryVisibleAdaptersRecoveryState()
    {
        NetworkAdapterRecoverySnapshot physical = Recovery("{A}");
        NetworkAdapterRecoverySnapshot disconnected = Recovery("{B}", "Virtual Ethernet") with
        {
            Adapter = Recovery("{B}", "Virtual Ethernet").Adapter with { IsConnected = false }
        };
        _reader.EnqueueResult(physical.Adapter, disconnected.Adapter);
        _recoveryReader.Enqueue(physical);
        _recoveryReader.Enqueue(disconnected);

        await CaptureAsync();
        await CaptureAsync();

        FirstRunBackup backup = Assert.IsType<FirstRunBackup>(_store.SavedBackup);
        Assert.Equal(1, _store.SaveCount);
        Assert.Equal(1, _reader.ReadCount);
        Assert.Equal(2, _recoveryReader.ReadCount);
        Assert.Equal(FirstRunBackup.CurrentSchemaVersion, backup.SchemaVersion);
        Assert.Equal(_clock.UtcNow, backup.CapturedAtUtc);
        Assert.Equal(new[] { physical.Adapter.Id, disconnected.Adapter.Id }, backup.Adapters.Select(item => item.AdapterId));
        AssertRecoveryEqual(physical, backup.Adapters[0]);
        AssertRecoveryEqual(disconnected, backup.Adapters[1]);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task CaptureAsync_WhenVirtualAdaptersHidden_UsesSharedWindowFilter()
    {
        NetworkAdapterRecoverySnapshot physical = Recovery("{A}");
        NetworkAdapterRecoverySnapshot virtualAdapter = Recovery("{B}", "Hyper-V Virtual Ethernet");
        _reader.EnqueueResult(physical.Adapter, virtualAdapter.Adapter);
        _recoveryReader.Enqueue(physical);

        await CaptureAsync(showVirtualAdapters: false);

        RecoverySnapshot saved = Assert.Single(_store.SavedBackup!.Adapters);
        Assert.Equal(physical.Adapter.Id, saved.AdapterId);
        Assert.Equal(1, _recoveryReader.ReadCount);
        Assert.Equal(
            AdapterVisibilityFilter.SelectVisible(new[] { physical.Adapter, virtualAdapter.Adapter }, false).Select(item => item.Id),
            _store.SavedBackup.Adapters.Select(item => item.AdapterId));
    }

    [Fact]
    public void SelectVisible_WhenSelectedVirtualAdapterBecomesHidden_RetainsSelectedWindowTab()
    {
        NetworkAdapterSnapshot selected = Recovery("{B}", "Virtual Ethernet").Adapter;

        Assert.Equal(selected, Assert.Single(AdapterVisibilityFilter.SelectVisible(new[] { selected }, false, selected.Id)));
    }

    [Fact]
    public async Task CaptureAsync_WhenOneAdapterCannotBeRead_WritesPartialAndLogsFailure()
    {
        NetworkAdapterRecoverySnapshot healthy = Recovery("{B}");
        _reader.EnqueueResult(Recovery("{A}").Adapter, healthy.Adapter);
        _recoveryReader.Enqueue(new NetworkAdapterRecoveryReadResult(NetworkAdapterRecoveryReadStatus.AdapterUnavailable));
        _recoveryReader.Enqueue(healthy);

        await CaptureAsync();

        Assert.Equal(healthy.Adapter.Id, Assert.Single(_store.SavedBackup!.Adapters).AdapterId);
        Assert.Equal(2, _recoveryReader.ReadCount);
        Assert.Equal(CriticalLogCategory.FirstRunBackup, Assert.Single(_logger.Entries).Category);
        Assert.Contains("{A}", _logger.Entries[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CaptureAsync_WhenNoAdaptersExist_DoesNotWriteAndRetriesNextStartup()
    {
        await CaptureAsync();
        await CaptureAsync();

        Assert.Equal(0, _store.SaveCount);
        Assert.Equal(0, _recoveryReader.ReadCount);
        Assert.Equal(2, _reader.ReadCount);
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task CaptureAsync_WhenOneRecoveryReaderThrows_ContinuesWithOtherAdapters()
    {
        NetworkAdapterRecoverySnapshot healthy = Recovery("{B}");
        _reader.EnqueueResult(Recovery("{A}").Adapter, healthy.Adapter);
        IOException failure = new("Read failed.");
        _recoveryReader.Failures.Add(new NetworkAdapterId("{A}"), failure);
        _recoveryReader.Enqueue(healthy);

        await CaptureAsync();

        Assert.Equal(healthy.Adapter.Id, Assert.Single(_store.SavedBackup!.Adapters).AdapterId);
        Assert.Same(failure, Assert.Single(_logger.Entries).Exception);
    }

    [Fact]
    public async Task CaptureAsync_WhenNoAdaptersCanBeRead_DoesNotWriteAndRetriesNextStartup()
    {
        _reader.EnqueueResult(Recovery("{A}").Adapter);

        await CaptureAsync();
        await CaptureAsync();

        Assert.Equal(0, _store.SaveCount);
        Assert.Equal(2, _recoveryReader.ReadCount);
        Assert.Equal(2, _logger.Entries.Count);
    }

    [Fact]
    public async Task CaptureAsync_WhenReaderReturnsDifferentIdentity_LogsAndDoesNotSaveWrongAdapter()
    {
        _reader.EnqueueResult(Recovery("{A}").Adapter);
        _recoveryReader.Enqueue(Recovery("{B}"));

        await CaptureAsync();

        Assert.Equal(0, _store.SaveCount);
        Assert.Single(_logger.Entries);
    }

    [Fact]
    public async Task CaptureAsync_WhenSaveFails_LogsWithoutThrowingAndRetriesNextStartup()
    {
        _reader.EnqueueResult(Recovery("{A}").Adapter);
        _recoveryReader.Enqueue(Recovery("{A}"));
        IOException failure = new("Disk full.");
        _store.SaveFailure = failure;

        await CaptureAsync();
        Assert.Same(failure, Assert.Single(_logger.Entries).Exception);
        Assert.False(_store.HasBackup);

        _store.SaveFailure = null;
        await CaptureAsync();

        Assert.Equal(2, _store.SaveCount);
        Assert.True(_store.HasBackup);
    }

    [Fact]
    public async Task CaptureAsync_WhenDiscoveryAndLoggerFail_DoesNotThrow()
    {
        _reader.FailWith(new InvalidOperationException("Discovery failed."));
        _logger.Failure = new IOException("Log unavailable.");

        await CaptureAsync();

        Assert.Equal(0, _store.SaveCount);
        Assert.Single(_logger.Entries);
    }

    [Fact]
    public async Task CaptureAsync_WhenExistenceCheckFails_LogsWithoutReadingOrThrowing()
    {
        _store.ExistsFailure = new UnauthorizedAccessException();

        await CaptureAsync();

        Assert.Equal(0, _reader.ReadCount);
        Assert.Equal(0, _store.SaveCount);
        Assert.Single(_logger.Entries);
    }

    private Task CaptureAsync(bool showVirtualAdapters = true) =>
        new FirstRunBackupService(_reader, _recoveryReader, _store, _clock, _logger)
            .CaptureAsync(showVirtualAdapters, CancellationToken.None);

    private static NetworkAdapterRecoverySnapshot Recovery(string id, string name = "Ethernet") =>
        new(
            new NetworkAdapterSnapshot(
                new NetworkAdapterId(id), name, "Contoso Adapter", "00:11:22:33:44:55", true, null,
                NetworkConfigurationMode.Static, "10.0.0.2", "255.255.255.0", "10.0.0.1", "1.1.1.1", "8.8.8.8",
                new Ipv4AddressCollection(new[]
                {
                    new Ipv4AddressAssignment("10.0.0.2", "255.255.255.0"),
                    new Ipv4AddressAssignment("10.0.0.3", "255.255.255.0")
                }),
                new Ipv4AddressValueCollection(Gateways),
                new Ipv4AddressValueCollection(DnsServers)),
            DnsConfigurationMode.Manual,
            DnsServers,
            new[] { new Ipv4GatewayRecoveryState("10.0.0.1", 25), new Ipv4GatewayRecoveryState("10.0.0.254", 50) });

    private void AssertRecoveryEqual(NetworkAdapterRecoverySnapshot expected, RecoverySnapshot actual)
    {
        Assert.Equal(RecoverySnapshotLoadResult.SupportedSchemaVersion, actual.SchemaVersion);
        Assert.True(Guid.TryParse(actual.SnapshotId, out _));
        Assert.Equal(_clock.UtcNow, actual.CapturedAtUtc);
        Assert.Equal(RecoverySnapshotState.Captured, actual.State);
        Assert.Equal(expected.Adapter.Id, actual.AdapterId);
        Assert.Equal(expected.Adapter.Name, actual.AdapterName);
        Assert.Equal(expected.Adapter.Description, actual.AdapterDescription);
        Assert.Equal(expected.Adapter.Mode, actual.Mode);
        Assert.Equal(expected.Adapter.Ipv4Addresses, actual.Ipv4Addresses);
        Assert.Equal(expected.Ipv4Gateways, actual.Ipv4Gateways);
        Assert.Equal(expected.DnsMode, actual.DnsMode);
        Assert.Equal(expected.ConfiguredIpv4DnsServers, actual.ConfiguredIpv4DnsServers);
        Assert.Equal(expected.Adapter.Ipv4DnsServers, actual.Ipv4DnsServers);
    }
}
