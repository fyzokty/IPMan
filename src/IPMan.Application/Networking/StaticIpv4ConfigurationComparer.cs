using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public sealed class StaticIpv4ConfigurationComparer : IStaticIpv4ConfigurationComparer
{
    public NetworkConfigurationComparisonResult Compare(
        NetworkAdapterSnapshot current,
        StaticIpv4Configuration normalizedDesired)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(normalizedDesired);

        NetworkConfigurationDifference differences = NetworkConfigurationDifference.None;

        AddIfDifferent(ref differences, NetworkConfigurationDifference.Ipv4Address, current.Ipv4Address, normalizedDesired.Ipv4Address);
        AddIfDifferent(ref differences, NetworkConfigurationDifference.SubnetMask, current.SubnetMask, normalizedDesired.SubnetMask);
        AddIfDifferent(ref differences, NetworkConfigurationDifference.Gateway, current.Gateway, normalizedDesired.Gateway);
        AddIfDifferent(ref differences, NetworkConfigurationDifference.PrimaryDns, current.PrimaryDns, normalizedDesired.PrimaryDns);
        AddIfDifferent(ref differences, NetworkConfigurationDifference.SecondaryDns, current.SecondaryDns, normalizedDesired.SecondaryDns);

        if (current.Mode != NetworkConfigurationMode.Static)
        {
            differences |= NetworkConfigurationDifference.Mode;
        }

        return new NetworkConfigurationComparisonResult(differences);
    }

    private static void AddIfDifferent(
        ref NetworkConfigurationDifference differences,
        NetworkConfigurationDifference field,
        string? current,
        string? desired)
    {
        if (!string.Equals(Normalize(current), Normalize(desired), StringComparison.Ordinal))
        {
            differences |= field;
        }
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Ipv4Value.TryParse(value, out Ipv4Value parsed)
            ? parsed.Text
            : value.Trim();
    }
}
