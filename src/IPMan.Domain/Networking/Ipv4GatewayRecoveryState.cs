namespace IPMan.Domain.Networking;

/// <summary>A captured IPv4 default gateway and its corresponding Windows metric.</summary>
public sealed record Ipv4GatewayRecoveryState(
    string Address,
    ushort? Metric);
