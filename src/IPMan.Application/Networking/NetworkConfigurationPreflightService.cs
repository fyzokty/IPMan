using System.Net.NetworkInformation;
using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public sealed class NetworkConfigurationPreflightService : INetworkConfigurationPreflightService
{
    private readonly INetworkAdapterReader _adapterReader;
    private readonly IStaticIpv4ConfigurationValidator _validator;
    private readonly IStaticIpv4ConfigurationComparer _comparer;
    private readonly IIpv4ConflictProbe _conflictProbe;

    public NetworkConfigurationPreflightService(
        INetworkAdapterReader adapterReader,
        IStaticIpv4ConfigurationValidator validator,
        IStaticIpv4ConfigurationComparer comparer,
        IIpv4ConflictProbe conflictProbe)
    {
        ArgumentNullException.ThrowIfNull(adapterReader);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(conflictProbe);

        _adapterReader = adapterReader;
        _validator = validator;
        _comparer = comparer;
        _conflictProbe = conflictProbe;
    }

    public async Task<NetworkConfigurationPreflightResult> PreflightAsync(
        NetworkAdapterId adapterId,
        StaticIpv4Configuration desiredConfiguration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(desiredConfiguration);

        NetworkAdapterSnapshot? current;

        try
        {
            current = await _adapterReader
                .GetAdapterAsync(adapterId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new NetworkConfigurationPreflightResult(NetworkConfigurationPreflightStatus.Cancelled);
        }
        catch (NetworkInformationException)
        {
            return new NetworkConfigurationPreflightResult(NetworkConfigurationPreflightStatus.AdapterReadFailed);
        }
        catch (PlatformNotSupportedException)
        {
            return new NetworkConfigurationPreflightResult(NetworkConfigurationPreflightStatus.AdapterReadFailed);
        }

        if (current is null)
        {
            return new NetworkConfigurationPreflightResult(NetworkConfigurationPreflightStatus.AdapterUnavailable);
        }

        StaticIpv4ValidationResult validation = _validator.Validate(desiredConfiguration);

        if (!validation.IsValid)
        {
            return new NetworkConfigurationPreflightResult(
                NetworkConfigurationPreflightStatus.ValidationFailed,
                current,
                validation);
        }

        StaticIpv4Configuration normalized = validation.NormalizedConfiguration!;
        NetworkConfigurationComparisonResult comparison = _comparer.Compare(current, normalized);

        if (comparison.IsEquivalent)
        {
            return new NetworkConfigurationPreflightResult(
                NetworkConfigurationPreflightStatus.NoChange,
                current,
                validation,
                comparison);
        }

        if (current.HasAdditionalIpv4Addresses)
        {
            return new NetworkConfigurationPreflightResult(
                NetworkConfigurationPreflightStatus.MultipleIpv4RequiresSafetyDecision,
                current,
                validation,
                comparison);
        }

        if (Ipv4Value.TryParse(current.Ipv4Address, out Ipv4Value currentAddress) &&
            string.Equals(currentAddress.Text, normalized.Ipv4Address, StringComparison.Ordinal))
        {
            // Another field may still require a later mutation, but probing the
            // adapter's own current address would create a false conflict warning.
            return new NetworkConfigurationPreflightResult(
                NetworkConfigurationPreflightStatus.Ready,
                current,
                validation,
                comparison);
        }

        Ipv4ConflictProbeResult probeResult;

        try
        {
            probeResult = await _conflictProbe
                .ProbeAsync(normalized.Ipv4Address, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            probeResult = new Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus.Cancelled);
        }

        NetworkConfigurationPreflightStatus status = probeResult.Status switch
        {
            Ipv4ConflictProbeStatus.ResponseObserved => NetworkConfigurationPreflightStatus.PotentialAddressConflict,
            Ipv4ConflictProbeStatus.NoResponse => NetworkConfigurationPreflightStatus.Ready,
            Ipv4ConflictProbeStatus.Unavailable => NetworkConfigurationPreflightStatus.ProbeIndeterminate,
            Ipv4ConflictProbeStatus.Cancelled => NetworkConfigurationPreflightStatus.Cancelled,
            _ => NetworkConfigurationPreflightStatus.ProbeIndeterminate
        };

        return new NetworkConfigurationPreflightResult(
            status,
            current,
            validation,
            comparison,
            probeResult);
    }

}
