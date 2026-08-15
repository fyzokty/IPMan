using IPMan.IntegrationTests.Observation;
using Xunit;

namespace IPMan.IntegrationTests.Harness;

public sealed class DnsInterfaceSettingsTruthReadResultTests
{
    private static readonly uint[] UnsupportedTypes = { 99 };

    [Fact]
    public void FromObservation_WhenSupportedV3ReadIsComplete_ReturnsSuccess()
    {
        DnsInterfaceSettingsTruthReadResult result =
            DnsInterfaceSettingsTruthReadResult.FromObservation(
                Observation(
                    supported: true,
                    nativeError: null,
                    version: 3,
                    DnsRicherPropertiesObservationStatus.Complete));

        Assert.True(result.ReadSuccess);
        Assert.Equal(DnsInterfaceSettingsTruthReadStatus.Success, result.Status);
        Assert.Equal((uint)0, result.NativeResult);
    }

    [Fact]
    public void FromObservation_WhenNativeReadFails_ReturnsTypedFailure()
    {
        DnsInterfaceSettingsTruthReadResult result =
            DnsInterfaceSettingsTruthReadResult.FromObservation(
                Observation(
                    supported: true,
                    nativeError: 5,
                    version: 3,
                    DnsRicherPropertiesObservationStatus.Incomplete));

        Assert.False(result.ReadSuccess);
        Assert.Equal(DnsInterfaceSettingsTruthReadStatus.NativeFailure, result.Status);
        Assert.Equal((uint)5, result.NativeResult);
    }

    [Fact]
    public void FromObservation_WhenApiIsUnsupported_DoesNotTreatMissingNativeErrorAsSuccess()
    {
        DnsInterfaceSettingsTruthReadResult result =
            DnsInterfaceSettingsTruthReadResult.FromObservation(
                Observation(
                    supported: false,
                    nativeError: null,
                    version: null,
                    DnsRicherPropertiesObservationStatus.NotApplicableByPlatform));

        Assert.False(result.ReadSuccess);
        Assert.Equal(DnsInterfaceSettingsTruthReadStatus.ApiUnsupported, result.Status);
        Assert.Null(result.NativeResult);
    }

    [Fact]
    public void FromObservation_WhenRicherReadIsIncomplete_FailsClosed()
    {
        DnsInterfaceSettingsTruthReadResult result =
            DnsInterfaceSettingsTruthReadResult.FromObservation(
                Observation(
                    supported: true,
                    nativeError: null,
                    version: 3,
                    DnsRicherPropertiesObservationStatus.Incomplete));

        Assert.False(result.ReadSuccess);
        Assert.Equal(DnsInterfaceSettingsTruthReadStatus.IncompleteRicherState, result.Status);
        Assert.Equal(DnsRicherPropertiesObservationStatus.Incomplete, result.RicherPropertiesStatus);
    }

    [Fact]
    public void FromObservation_WhenResultShapeIsMalformed_FailsClosed()
    {
        DnsInterfaceSettingsTruthReadResult result =
            DnsInterfaceSettingsTruthReadResult.FromObservation(
                Observation(
                    supported: true,
                    nativeError: null,
                    version: 4,
                    DnsRicherPropertiesObservationStatus.Complete));

        Assert.False(result.ReadSuccess);
        Assert.Equal(DnsInterfaceSettingsTruthReadStatus.MalformedResult, result.Status);
    }

    [Fact]
    public void FromObservation_WhenRicherPropertyIsUnsupported_PreservesEvidenceAndFailsClosed()
    {
        DnsServerPropertyObservation unsupported = new(
            Version: 1,
            ServerIndex: 0,
            Type: 99,
            PayloadSupported: false,
            DohFlags: null,
            DohTemplatePresent: false,
            DohTemplateHash: null);
        DnsSettingsObservation observation = Observation(
            supported: true,
            nativeError: null,
            version: 3,
            DnsRicherPropertiesObservationStatus.Incomplete,
            UnsupportedTypes,
            [unsupported]);

        DnsInterfaceSettingsTruthReadResult result =
            DnsInterfaceSettingsTruthReadResult.FromObservation(observation);

        Assert.False(result.ReadSuccess);
        Assert.Equal(
            DnsInterfaceSettingsTruthReadStatus.UnsupportedRicherProperty,
            result.Status);
        Assert.Equal(UnsupportedTypes, result.UnsupportedPropertyTypes);
        Assert.Equal(unsupported, Assert.Single(result.ServerProperties));
    }

    [Fact]
    public void SharedDnsSettingsObservation_EvidenceShapeDoesNotContainDiagnosticReadSuccess()
    {
        Assert.DoesNotContain(
            typeof(DnsSettingsObservation).GetProperties(),
            property => string.Equals(
                property.Name,
                nameof(DnsInterfaceSettingsTruthReadResult.ReadSuccess),
                StringComparison.Ordinal));
    }

    private static DnsSettingsObservation Observation(
        bool supported,
        uint? nativeError,
        int? version,
        DnsRicherPropertiesObservationStatus richerStatus,
        IReadOnlyList<uint>? unsupportedTypes = null,
        IReadOnlyList<DnsServerPropertyObservation>? serverProperties = null) =>
        new(
            supported,
            nativeError,
            version,
            Flags: 2,
            NameServer: "1.1.1.1",
            ProfileNameServer: null,
            SupplementalSearchListPresent: false,
            SupplementalSearchListHash: null,
            richerStatus,
            unsupportedTypes ?? Array.Empty<uint>(),
            serverProperties ?? Array.Empty<DnsServerPropertyObservation>(),
            Array.Empty<DnsServerPropertyObservation>());
}
