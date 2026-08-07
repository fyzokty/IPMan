using System.Collections;

namespace IPMan.Domain.Networking;

/// <summary>Immutable, order-preserving collection of canonical IPv4 text values.</summary>
public sealed class Ipv4AddressValueCollection
    : IReadOnlyList<string>, IEquatable<Ipv4AddressValueCollection>
{
    public static readonly Ipv4AddressValueCollection Empty = new(Array.Empty<string>());

    private readonly string[] _values;

    public Ipv4AddressValueCollection(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values.ToArray();
    }

    public int Count => _values.Length;

    public string this[int index] => _values[index];

    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)_values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

    public bool Equals(Ipv4AddressValueCollection? other) =>
        other is not null &&
        (ReferenceEquals(this, other) || _values.SequenceEqual(other._values, StringComparer.Ordinal));

    public override bool Equals(object? obj) => Equals(obj as Ipv4AddressValueCollection);

    public override int GetHashCode()
    {
        HashCode hash = new();

        foreach (string value in _values)
        {
            hash.Add(value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    public override string ToString() => string.Join(", ", _values);

    public static bool operator ==(Ipv4AddressValueCollection? left, Ipv4AddressValueCollection? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Ipv4AddressValueCollection? left, Ipv4AddressValueCollection? right) =>
        !(left == right);
}
