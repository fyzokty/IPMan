using System.Globalization;

namespace IPMan.Application.Networking;

/// <summary>
/// Best-effort /24 suggestions; verification is probe evidence, not a reservation. A probe can
/// only answer for addresses on the adapter's current on-link subnet, so candidates outside it
/// are never probed and are at most <see cref="Ipv4AddressSuggestionStatus.Unverified"/>.
/// </summary>
public sealed class Ipv4AddressSuggestionService(IIpv4ConflictProbe probe)
{
    private readonly IIpv4ConflictProbe _probe = probe ?? throw new ArgumentNullException(nameof(probe));

    public static IReadOnlyList<string> GenerateCandidates(string deviceAddress, string? currentGateway) =>
        GenerateCandidates(deviceAddress, [currentGateway]);

    /// <summary>Skips the network and broadcast hosts, the device and every excluded address.</summary>
    public static IReadOnlyList<string> GenerateCandidates(string deviceAddress, IEnumerable<string?> excludedAddresses)
    {
        ArgumentNullException.ThrowIfNull(excludedAddresses);
        if (!Ipv4Value.TryParse(deviceAddress, out Ipv4Value device) ||
            device.IsUnspecified || device.IsLoopback || device.IsMulticast || device.IsLimitedBroadcast)
        {
            return Array.Empty<string>();
        }

        uint network = device.Bits & 0xffffff00;
        HashSet<uint> excluded = [device.Bits];
        foreach (string? address in excludedAddresses)
        {
            if (Ipv4Value.TryParse(address, out Ipv4Value parsed))
            {
                excluded.Add(parsed.Bits);
            }
        }
        List<string> candidates = new(254);
        for (int offset = 0; offset < 254; offset++)
        {
            uint host = (uint)((49 + offset) % 254 + 1);
            uint bits = network | host;
            if (excluded.Contains(bits))
            {
                continue;
            }

            candidates.Add(string.Create(CultureInfo.InvariantCulture,
                $"{bits >> 24}.{(bits >> 16) & 255}.{(bits >> 8) & 255}.{host}"));
        }

        return candidates;
    }

    /// <summary>
    /// True only when the adapter's current address and contiguous, non-zero mask place the
    /// candidate on the same link, i.e. where a probe could actually reach it.
    /// </summary>
    public static bool IsOnAdapterSubnet(string candidate, string? adapterAddress, string? adapterSubnetMask)
    {
        if (!Ipv4Value.TryParse(candidate, out Ipv4Value target) ||
            !Ipv4Value.TryParse(adapterAddress, out Ipv4Value address) ||
            !Ipv4Value.TryParse(adapterSubnetMask, out Ipv4Value mask) ||
            address.IsUnspecified || mask.Bits == 0)
        {
            return false;
        }

        uint hostBits = ~mask.Bits;
        if ((hostBits & (hostBits + 1)) != 0)
        {
            return false;
        }

        return (target.Bits & mask.Bits) == (address.Bits & mask.Bits);
    }

    public async Task<Ipv4AddressSuggestion?> SuggestAsync(
        Ipv4AddressSuggestionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<string> candidates = GenerateCandidates(request.DeviceAddress, request.ExcludedAddresses);
        if (candidates.Count == 0)
        {
            return null;
        }

        foreach (string candidate in candidates.Take(5))
        {
            if (!IsOnAdapterSubnet(candidate, request.AdapterAddress, request.AdapterSubnetMask))
            {
                // Probing another network proves nothing, so do not probe and do not claim it is free.
                return new(candidate, Ipv4AddressSuggestionStatus.Unverified);
            }

            Ipv4ConflictProbeResult result;
            try
            {
                result = await _probe.ProbeAsync(candidate, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new(candidate, Ipv4AddressSuggestionStatus.Unverified);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Status == Ipv4ConflictProbeStatus.NoResponse)
            {
                return new(candidate, Ipv4AddressSuggestionStatus.VerifiedFree);
            }

            if (result.Status != Ipv4ConflictProbeStatus.ResponseObserved)
            {
                return new(candidate, Ipv4AddressSuggestionStatus.Unverified);
            }
        }

        return candidates.Count > 5
            ? new(candidates[5], Ipv4AddressSuggestionStatus.Unverified)
            : null;
    }
}

/// <param name="DeviceAddress">The target device whose /24 the suggestion is taken from.</param>
/// <param name="ExcludedAddresses">Addresses that must never be suggested, such as gateways.</param>
/// <param name="AdapterAddress">The adapter's current IPv4 address, when known.</param>
/// <param name="AdapterSubnetMask">The adapter's current subnet mask, when known.</param>
public sealed record Ipv4AddressSuggestionRequest(
    string DeviceAddress,
    IReadOnlyList<string?> ExcludedAddresses,
    string? AdapterAddress,
    string? AdapterSubnetMask);

public enum Ipv4AddressSuggestionStatus
{
    Unverified,
    VerifiedFree
}

public sealed record Ipv4AddressSuggestion(string Address, Ipv4AddressSuggestionStatus Status);
