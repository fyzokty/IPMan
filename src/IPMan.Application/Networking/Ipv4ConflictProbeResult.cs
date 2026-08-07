namespace IPMan.Application.Networking;

/// <summary>
/// Evidence from a best-effort probe. <see cref="Ipv4ConflictProbeStatus.NoResponse"/>
/// is deliberately inconclusive and never means that an address is free.
/// </summary>
public sealed record Ipv4ConflictProbeResult(Ipv4ConflictProbeStatus Status);
