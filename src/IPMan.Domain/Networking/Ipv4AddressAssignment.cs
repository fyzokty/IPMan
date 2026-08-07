namespace IPMan.Domain.Networking;

/// <summary>
/// One IPv4 address configured on an adapter, exactly as Windows reports it.
/// </summary>
/// <param name="Address">Dotted-quad IPv4 address.</param>
/// <param name="SubnetMask">
/// Subnet mask reported for this address, or <c>null</c> when Windows reports none.
/// </param>
public sealed record Ipv4AddressAssignment(string Address, string? SubnetMask)
{
    public override string ToString() =>
        SubnetMask is null ? Address : $"{Address}/{SubnetMask}";
}
