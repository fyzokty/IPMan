namespace IPMan.Domain.Networking;

/// <summary>
/// Stable application identifier for a Windows network adapter.
/// The underlying value is expected to map to the Windows adapter identity
/// returned by the infrastructure layer.
/// </summary>
public readonly record struct NetworkAdapterId
{
    public NetworkAdapterId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = Guid.TryParse(value, out Guid parsed)
            ? parsed.ToString("B").ToUpperInvariant()
            : value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
