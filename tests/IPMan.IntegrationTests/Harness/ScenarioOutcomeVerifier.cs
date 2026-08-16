using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.IntegrationTests.Observation;

namespace IPMan.IntegrationTests.Harness;

public static class ScenarioOutcomeVerifier
{
    public static IReadOnlyList<string> Verify(
        DestructiveNetworkTestSettings settings,
        StaticIpv4ApplyResult? result,
        NetworkAdapterRecoverySnapshot beforeRecovery,
        NetworkAdapterRecoveryReadResult? afterRecovery,
        IReadOnlyList<string> requestedDimensions,
        ObservationComparisonResult? nonInterference,
        bool recoveryMatchesBefore)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(beforeRecovery);
        ArgumentNullException.ThrowIfNull(requestedDimensions);
        List<string> differences = new();

        if (result?.Status != StaticIpv4ApplyStatus.VerifiedSuccess)
        {
            differences.Add($"Product apply status was {result?.Status.ToString() ?? "unavailable"}.");
        }

        if (result?.Recovery is null)
        {
            differences.Add("No recovery snapshot reference was returned.");
        }

        if (!recoveryMatchesBefore)
        {
            differences.Add("Recovery snapshot did not match the immediate pre-mutation recovery state.");
        }

        if (afterRecovery?.Status != NetworkAdapterRecoveryReadStatus.Success ||
            afterRecovery.Snapshot is null)
        {
            differences.Add("Post-mutation recovery state could not be read.");
        }
        else
        {
            VerifyActualState(
                settings,
                beforeRecovery,
                afterRecovery.Snapshot,
                requestedDimensions,
                differences);
        }

        if (nonInterference is null)
        {
            differences.Add("Independent before/after observation was incomplete.");
        }
        else
        {
            differences.AddRange(nonInterference.Differences);
        }

        return differences;
    }

    private static void VerifyActualState(
        DestructiveNetworkTestSettings settings,
        NetworkAdapterRecoverySnapshot before,
        NetworkAdapterRecoverySnapshot actual,
        IReadOnlyList<string> requestedDimensions,
        List<string> differences)
    {
        StaticIpv4Configuration desired = settings.DesiredConfiguration;

        if (actual.Adapter.Id != settings.AdapterId)
        {
            differences.Add("Post-mutation recovery read returned a different adapter identity.");
        }

        if (actual.Adapter.Mode != NetworkConfigurationMode.Static)
        {
            differences.Add("Post-mutation IPv4 mode was not static.");
        }

        if (actual.Adapter.Ipv4Addresses.Count != 1 ||
            actual.Adapter.Ipv4Addresses[0].Address != desired.Ipv4Address ||
            actual.Adapter.Ipv4Addresses[0].SubnetMask != desired.SubnetMask)
        {
            differences.Add("Post-mutation IPv4 address/mask did not exactly match the request.");
        }

        bool gatewayRequested = requestedDimensions.Contains(
            ScenarioPreconditionValidator.GatewayDimension,
            StringComparer.Ordinal);

        if (!gatewayRequested)
        {
            if (!actual.Ipv4Gateways.SequenceEqual(before.Ipv4Gateways))
            {
                differences.Add(
                    "Post-mutation IPv4 gateway address/metric changed without a gateway request.");
            }
        }
        else if (desired.Gateway is null)
        {
            if (actual.Ipv4Gateways.Length != 0)
            {
                differences.Add("Post-mutation IPv4 gateway was not empty.");
            }
        }
        else if (actual.Ipv4Gateways.Length != 1 ||
            actual.Ipv4Gateways[0].Address != desired.Gateway ||
            actual.Ipv4Gateways[0].Metric != 1)
        {
            differences.Add("Post-mutation IPv4 gateway/metric did not exactly match the expected behavior.");
        }

        string[] desiredDns = new[] { desired.PrimaryDns, desired.SecondaryDns }
            .Where(value => value is not null)
            .Select(value => value!)
            .ToArray();

        if (desiredDns.Length == 0)
        {
            if (actual.DnsMode != DnsConfigurationMode.Automatic ||
                actual.ConfiguredIpv4DnsServers.Length != 0)
            {
                differences.Add("Post-mutation DNS source did not return to automatic semantics.");
            }
        }
        else if (actual.DnsMode != DnsConfigurationMode.Manual ||
            !actual.ConfiguredIpv4DnsServers.SequenceEqual(desiredDns, StringComparer.Ordinal) ||
            !actual.Adapter.Ipv4DnsServers.SequenceEqual(desiredDns, StringComparer.Ordinal))
        {
            differences.Add("Post-mutation manual DNS source/list/order did not exactly match the request.");
        }
    }
}
