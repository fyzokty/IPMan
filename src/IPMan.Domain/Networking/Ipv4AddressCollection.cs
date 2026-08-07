using System.Collections;

namespace IPMan.Domain.Networking;

/// <summary>
/// Immutable, order-preserving list of the IPv4 addresses configured on an
/// adapter.
/// <para>
/// Windows adapters can carry more than one IPv4 address. Release 1.0 edits a
/// single primary address, but discovery must never lose the others: later
/// mutation logic has to know they exist so it cannot silently delete or
/// overwrite them.
/// </para>
/// <para>
/// Equality is by sequence, so a snapshot comparison detects an address being
/// added to or removed from an adapter.
/// </para>
/// </summary>
public sealed class Ipv4AddressCollection
    : IReadOnlyList<Ipv4AddressAssignment>, IEquatable<Ipv4AddressCollection>
{
    public static readonly Ipv4AddressCollection Empty =
        new(Array.Empty<Ipv4AddressAssignment>());

    private readonly Ipv4AddressAssignment[] _addresses;

    public Ipv4AddressCollection(IEnumerable<Ipv4AddressAssignment> addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        _addresses = addresses.ToArray();
    }

    public int Count => _addresses.Length;

    public Ipv4AddressAssignment this[int index] => _addresses[index];

    public IEnumerator<Ipv4AddressAssignment> GetEnumerator() =>
        ((IEnumerable<Ipv4AddressAssignment>)_addresses).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _addresses.GetEnumerator();

    public bool Equals(Ipv4AddressCollection? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || _addresses.SequenceEqual(other._addresses);
    }

    public override bool Equals(object? obj) => Equals(obj as Ipv4AddressCollection);

    public override int GetHashCode()
    {
        HashCode hash = new();

        foreach (Ipv4AddressAssignment address in _addresses)
        {
            hash.Add(address);
        }

        return hash.ToHashCode();
    }

    public override string ToString() =>
        string.Join(", ", _addresses.Select(address => address.ToString()));

    public static bool operator ==(Ipv4AddressCollection? left, Ipv4AddressCollection? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Ipv4AddressCollection? left, Ipv4AddressCollection? right) =>
        !(left == right);
}
