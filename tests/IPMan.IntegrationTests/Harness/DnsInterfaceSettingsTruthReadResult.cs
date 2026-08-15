using IPMan.IntegrationTests.Observation;

namespace IPMan.IntegrationTests.Harness;

internal enum DnsInterfaceSettingsTruthReadStatus
{
    Success = 0,
    ApiUnsupported = 1,
    NativeFailure = 2,
    MalformedResult = 3,
    IncompleteRicherState = 4,
    UnsupportedRicherProperty = 5
}

internal sealed record DnsInterfaceSettingsTruthReadResult(
    DnsInterfaceSettingsTruthReadStatus Status,
    bool Supported,
    uint? NativeResult,
    int? Version,
    ulong Flags,
    string? NameServer,
    string? ProfileNameServer,
    bool SupplementalSearchListPresent,
    string? SupplementalSearchListHash,
    DnsRicherPropertiesObservationStatus RicherPropertiesStatus,
    IReadOnlyList<uint> UnsupportedPropertyTypes,
    IReadOnlyList<DnsServerPropertyObservation> ServerProperties,
    IReadOnlyList<DnsServerPropertyObservation> ProfileServerProperties)
{
    public bool ReadSuccess => Status == DnsInterfaceSettingsTruthReadStatus.Success;

    public static DnsInterfaceSettingsTruthReadResult FromObservation(
        DnsSettingsObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        DnsInterfaceSettingsTruthReadStatus status = Classify(observation);

        return new DnsInterfaceSettingsTruthReadResult(
            status,
            observation.Supported,
            status == DnsInterfaceSettingsTruthReadStatus.Success
                ? 0
                : observation.NativeError,
            observation.Version,
            observation.Flags,
            observation.NameServer,
            observation.ProfileNameServer,
            observation.SupplementalSearchListPresent,
            observation.SupplementalSearchListHash,
            observation.RicherPropertiesStatus,
            observation.UnsupportedPropertyTypes,
            observation.ServerProperties,
            observation.ProfileServerProperties);
    }

    private static DnsInterfaceSettingsTruthReadStatus Classify(
        DnsSettingsObservation observation)
    {
        if (!observation.Supported)
        {
            return DnsInterfaceSettingsTruthReadStatus.ApiUnsupported;
        }

        if (observation.NativeError is not null)
        {
            return DnsInterfaceSettingsTruthReadStatus.NativeFailure;
        }

        if (observation.Version is not 1 and not 2 and not 3)
        {
            return DnsInterfaceSettingsTruthReadStatus.MalformedResult;
        }

        bool hasUnsupportedProperty = observation.UnsupportedPropertyTypes.Count != 0 ||
            observation.ServerProperties.Any(property => !property.PayloadSupported) ||
            observation.ProfileServerProperties.Any(property => !property.PayloadSupported);

        if (hasUnsupportedProperty)
        {
            return DnsInterfaceSettingsTruthReadStatus.UnsupportedRicherProperty;
        }

        if (observation.Version < 3)
        {
            return observation.RicherPropertiesStatus ==
                    DnsRicherPropertiesObservationStatus.NotApplicableByPlatform &&
                observation.ServerProperties.Count == 0 &&
                observation.ProfileServerProperties.Count == 0
                ? DnsInterfaceSettingsTruthReadStatus.Success
                : DnsInterfaceSettingsTruthReadStatus.MalformedResult;
        }

        return observation.RicherPropertiesStatus ==
                DnsRicherPropertiesObservationStatus.Complete
            ? DnsInterfaceSettingsTruthReadStatus.Success
            : DnsInterfaceSettingsTruthReadStatus.IncompleteRicherState;
    }
}
