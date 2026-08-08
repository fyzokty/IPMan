namespace IPMan.IntegrationTests.Observation;

public sealed record NetworkObservation(
    DateTimeOffset CapturedAtUtc,
    string AdapterId,
    string Name,
    string Description,
    string MacAddress,
    bool Ipv6Enabled,
    IReadOnlyList<IpAddressObservation> Ipv4Addresses,
    IReadOnlyList<string> Ipv4Gateways,
    IReadOnlyList<string> Ipv4DnsServers,
    IReadOnlyList<IpAddressObservation> Ipv6Addresses,
    IReadOnlyList<string> Ipv6Gateways,
    IReadOnlyList<string> Ipv6DnsServers,
    Ipv6RouteTableObservation Ipv6Routes,
    DnsSettingsObservation DnsSettings);

public sealed record IpAddressObservation(
    string Address,
    int? PrefixLength,
    string? Ipv4Mask);

public sealed record Ipv6RouteTableObservation(
    bool Supported,
    bool Complete,
    uint? NativeError,
    uint? InterfaceIndex,
    IReadOnlyList<Ipv6RouteObservation> Routes);

public sealed record Ipv6RouteObservation(
    string DestinationPrefix,
    byte PrefixLength,
    string NextHop,
    uint InterfaceIndex,
    ulong InterfaceLuid,
    byte SitePrefixLength,
    uint Metric,
    int Protocol,
    bool Loopback,
    bool AutoconfigureAddress,
    bool Publish,
    bool Immortal,
    int Origin);

public sealed record DnsSettingsObservation(
    bool Supported,
    uint? NativeError,
    int? Version,
    ulong Flags,
    string? NameServer,
    string? ProfileNameServer,
    bool SupplementalSearchListPresent,
    string? SupplementalSearchListHash,
    DnsRicherPropertiesObservationStatus RicherPropertiesStatus,
    IReadOnlyList<uint> UnsupportedPropertyTypes,
    IReadOnlyList<DnsServerPropertyObservation> ServerProperties,
    IReadOnlyList<DnsServerPropertyObservation> ProfileServerProperties);

public enum DnsRicherPropertiesObservationStatus
{
    Complete = 0,
    NotApplicableByPlatform = 1,
    Incomplete = 2
}

public sealed record DnsServerPropertyObservation(
    uint Version,
    uint ServerIndex,
    uint Type,
    bool PayloadSupported,
    ulong? DohFlags,
    bool DohTemplatePresent,
    string? DohTemplateHash);
