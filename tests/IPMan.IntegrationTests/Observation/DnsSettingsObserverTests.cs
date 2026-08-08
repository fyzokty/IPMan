using Xunit;

namespace IPMan.IntegrationTests.Observation;

public sealed class DnsSettingsObserverTests
{
    [Fact]
    public void ReadForPlatform_WhenV3CapableAndV3Fails_FailsClosedWithoutDowngrade()
    {
        List<int> calls = new();

        DnsSettingsObservation result = DnsSettingsObserver.ReadForPlatform(
            new Version(10, 0, 22621),
            version =>
            {
                calls.Add(version);
                return Observation(
                    version,
                    nativeError: 5,
                    DnsRicherPropertiesObservationStatus.Incomplete);
            });

        Assert.Equal(3, Assert.Single(calls));
        Assert.True(result.Supported);
        Assert.Equal((uint)5, result.NativeError);
        Assert.Equal(3, result.Version);
        Assert.Equal(DnsRicherPropertiesObservationStatus.Incomplete, result.RicherPropertiesStatus);
    }

    [Fact]
    public void ReadForPlatform_WhenBelowV3Capability_SelectsVersion2AsNotApplicableForRicherState()
    {
        List<int> calls = new();

        DnsSettingsObservation result = DnsSettingsObserver.ReadForPlatform(
            new Version(10, 0, 19045),
            version =>
            {
                calls.Add(version);
                return Observation(
                    version,
                    nativeError: null,
                    DnsRicherPropertiesObservationStatus.NotApplicableByPlatform);
            });

        Assert.Equal(2, Assert.Single(calls));
        Assert.Equal(2, result.Version);
        Assert.Equal(
            DnsRicherPropertiesObservationStatus.NotApplicableByPlatform,
            result.RicherPropertiesStatus);
    }

    [Fact]
    public void ReadForPlatform_WhenV3Succeeds_RetainsRicherProperties()
    {
        DnsServerPropertyObservation property = new(1, 0, 1, true, 1, true, "HASH");

        DnsSettingsObservation result = DnsSettingsObserver.ReadForPlatform(
            new Version(10, 0, 26100),
            version => Observation(
                version,
                nativeError: null,
                DnsRicherPropertiesObservationStatus.Complete,
                new[] { property }));

        Assert.Equal(3, result.Version);
        Assert.Equal(DnsRicherPropertiesObservationStatus.Complete, result.RicherPropertiesStatus);
        Assert.Equal(property, Assert.Single(result.ServerProperties));
    }

    [Fact]
    public void ReadForPlatform_WhenApiIsNotDocumentedForPlatform_DoesNotCallNativeReader()
    {
        int calls = 0;

        DnsSettingsObservation result = DnsSettingsObserver.ReadForPlatform(
            new Version(10, 0, 18362),
            _ =>
            {
                calls++;
                return Observation(1, null, DnsRicherPropertiesObservationStatus.NotApplicableByPlatform);
            });

        Assert.Equal(0, calls);
        Assert.False(result.Supported);
        Assert.Null(result.Version);
        Assert.Equal(
            DnsRicherPropertiesObservationStatus.NotApplicableByPlatform,
            result.RicherPropertiesStatus);
    }

    private static DnsSettingsObservation Observation(
        int version,
        uint? nativeError,
        DnsRicherPropertiesObservationStatus richerStatus,
        IReadOnlyList<DnsServerPropertyObservation>? serverProperties = null) =>
        new(
            Supported: true,
            nativeError,
            version,
            Flags: 0,
            NameServer: null,
            ProfileNameServer: null,
            SupplementalSearchListPresent: false,
            SupplementalSearchListHash: null,
            richerStatus,
            Array.Empty<uint>(),
            serverProperties ?? Array.Empty<DnsServerPropertyObservation>(),
            Array.Empty<DnsServerPropertyObservation>());
}
