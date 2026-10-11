using System.Globalization;

namespace IPMan.Application.Networking;

/// <summary>Best-effort /24 suggestions; verification is probe evidence, not a reservation.</summary>
public sealed class Ipv4AddressSuggestionService(IIpv4ConflictProbe probe)
{
    private readonly IIpv4ConflictProbe _probe = probe ?? throw new ArgumentNullException(nameof(probe));

    public static IReadOnlyList<string> GenerateCandidates(string deviceAddress, string? currentGateway)
    {
        if (!Ipv4Value.TryParse(deviceAddress, out Ipv4Value device) ||
            device.IsUnspecified || device.IsLoopback || device.IsMulticast || device.IsLimitedBroadcast)
        {
            return Array.Empty<string>();
        }

        uint network = device.Bits & 0xffffff00;
        uint? gateway = Ipv4Value.TryParse(currentGateway, out Ipv4Value parsed) ? parsed.Bits : null;
        List<string> candidates = new(254);
        for (int offset = 0; offset < 254; offset++)
        {
            uint host = (uint)((49 + offset) % 254 + 1);
            uint bits = network | host;
            if (bits == device.Bits || bits == gateway)
            {
                continue;
            }

            candidates.Add(string.Create(CultureInfo.InvariantCulture,
                $"{bits >> 24}.{(bits >> 16) & 255}.{(bits >> 8) & 255}.{host}"));
        }

        return candidates;
    }

    public async Task<Ipv4AddressSuggestion?> SuggestAsync(
        string deviceAddress, string? currentGateway, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<string> candidates = GenerateCandidates(deviceAddress, currentGateway);
        if (candidates.Count == 0)
        {
            return null;
        }

        foreach (string candidate in candidates.Take(5))
        {
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

public enum Ipv4AddressSuggestionStatus
{
    Unverified,
    VerifiedFree
}

public sealed record Ipv4AddressSuggestion(string Address, Ipv4AddressSuggestionStatus Status);
