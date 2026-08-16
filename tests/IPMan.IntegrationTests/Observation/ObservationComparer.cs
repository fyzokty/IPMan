using IPMan.IntegrationTests.Harness;

namespace IPMan.IntegrationTests.Observation;

public static class ObservationComparer
{
    private const ulong NameServerFlag = 0x2;
    private static readonly string[] WindowsUnconfiguredIpv6DnsPlaceholders =
    {
        "fec0:0:0:ffff::1",
        "fec0:0:0:ffff::2",
        "fec0:0:0:ffff::3"
    };

    public static ObservationComparisonResult CompareNonInterference(
        NetworkObservation before,
        NetworkObservation after,
        IReadOnlyList<string> requestedDimensions)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(requestedDimensions);
        List<string> differences = new();

        AddIfDifferent(differences, "adapter identity", before.AdapterId, after.AdapterId);
        AddIfDifferent(differences, "adapter name", before.Name, after.Name);
        AddIfDifferent(differences, "adapter description", before.Description, after.Description);
        AddIfDifferent(differences, "adapter MAC address", before.MacAddress, after.MacAddress);
        AddIfDifferent(differences, "IPv6 enabled state", before.Ipv6Enabled, after.Ipv6Enabled);
        AddSequenceIfDifferent(differences, "IPv6 addresses", before.Ipv6Addresses, after.Ipv6Addresses);
        AddSequenceIfDifferent(differences, "IPv6 gateways", before.Ipv6Gateways, after.Ipv6Gateways);
        AddSequenceIfDifferent(
            differences,
            "IPv6 DNS servers",
            NormalizeIpv6DnsServers(before.Ipv6DnsServers),
            NormalizeIpv6DnsServers(after.Ipv6DnsServers));
        CompareIpv6Routes(before.Ipv6Routes, after.Ipv6Routes, differences);

        bool changesDns = requestedDimensions.Contains(
            ScenarioPreconditionValidator.DnsDimension,
            StringComparer.Ordinal);

        CompareDnsSettings(before.DnsSettings, after.DnsSettings, changesDns, differences);

        return new ObservationComparisonResult(differences.Count == 0, differences);
    }

    private static string[] NormalizeIpv6DnsServers(IReadOnlyList<string> servers)
    {
        // GetAdaptersAddresses/.NET can project this exact legacy FEC0 trio
        // when no IPv6 DNS server is configured. Treat only the complete trio
        // as the unconfigured sentinel; every configured IPv6 value stays strict.
        string[] normalized = servers
            .Select(server => server.Split('%', 2)[0])
            .OrderBy(server => server, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] placeholders = WindowsUnconfiguredIpv6DnsPlaceholders
            .OrderBy(server => server, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return normalized.SequenceEqual(placeholders, StringComparer.OrdinalIgnoreCase)
            ? Array.Empty<string>()
            : normalized;
    }

    private static void CompareIpv6Routes(
        Ipv6RouteTableObservation before,
        Ipv6RouteTableObservation after,
        List<string> differences)
    {
        if (!before.Supported || !before.Complete || !after.Supported || !after.Complete)
        {
            differences.Add("IPv6 route observation was incomplete; non-interference cannot pass.");
            return;
        }

        AddIfDifferent(
            differences,
            "IPv6 route interface index",
            before.InterfaceIndex,
            after.InterfaceIndex);
        AddSequenceIfDifferent(differences, "IPv6 routes", before.Routes, after.Routes);
    }

    private static void CompareDnsSettings(
        DnsSettingsObservation before,
        DnsSettingsObservation after,
        bool allowsNameServerChange,
        List<string> differences)
    {
        if (!before.Supported || !after.Supported ||
            before.RicherPropertiesStatus == DnsRicherPropertiesObservationStatus.Incomplete ||
            after.RicherPropertiesStatus == DnsRicherPropertiesObservationStatus.Incomplete)
        {
            differences.Add(
                "Richer DNS observation was incomplete or contained an unsupported property type; non-interference cannot pass.");
        }

        AddIfDifferent(differences, "DNS observer support", before.Supported, after.Supported);
        AddIfDifferent(differences, "DNS observer native error", before.NativeError, after.NativeError);
        AddIfDifferent(differences, "DNS settings version", before.Version, after.Version);
        AddIfDifferent(
            differences,
            "richer DNS platform observation status",
            before.RicherPropertiesStatus,
            after.RicherPropertiesStatus);
        AddIfDifferent(
            differences,
            allowsNameServerChange ? "unrelated DNS settings flags" : "DNS settings flags",
            allowsNameServerChange ? before.Flags & ~NameServerFlag : before.Flags,
            allowsNameServerChange ? after.Flags & ~NameServerFlag : after.Flags);

        if (!allowsNameServerChange)
        {
            AddIfDifferent(
                differences,
                "DNS name-server state",
                before.NameServer,
                after.NameServer);
        }
        AddIfDifferent(
            differences,
            "profile DNS name-server state",
            before.ProfileNameServer,
            after.ProfileNameServer);
        AddIfDifferent(
            differences,
            "supplemental DNS search-list presence",
            before.SupplementalSearchListPresent,
            after.SupplementalSearchListPresent);
        AddIfDifferent(
            differences,
            "supplemental DNS search-list hash",
            before.SupplementalSearchListHash,
            after.SupplementalSearchListHash);
        AddSequenceIfDifferent(
            differences,
            "unsupported richer DNS property types",
            before.UnsupportedPropertyTypes,
            after.UnsupportedPropertyTypes);
        AddSequenceIfDifferent(
            differences,
            "DNS server properties / DoH state",
            before.ServerProperties,
            after.ServerProperties);
        AddSequenceIfDifferent(
            differences,
            "profile DNS server properties / DoH state",
            before.ProfileServerProperties,
            after.ProfileServerProperties);
    }

    private static void AddSequenceIfDifferent<T>(
        List<string> differences,
        string name,
        IReadOnlyList<T> before,
        IReadOnlyList<T> after)
    {
        string[] normalizedBefore = before
            .Select(value => value?.ToString() ?? string.Empty)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        string[] normalizedAfter = after
            .Select(value => value?.ToString() ?? string.Empty)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        if (!normalizedBefore.SequenceEqual(normalizedAfter, StringComparer.Ordinal))
        {
            differences.Add($"Unexpected {name} change.");
        }
    }

    private static void AddIfDifferent<T>(
        List<string> differences,
        string name,
        T before,
        T after)
    {
        if (!EqualityComparer<T>.Default.Equals(before, after))
        {
            differences.Add($"Unexpected {name} change.");
        }
    }
}
