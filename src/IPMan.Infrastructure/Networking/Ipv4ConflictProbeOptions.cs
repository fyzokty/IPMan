namespace IPMan.Infrastructure.Networking;

public sealed class Ipv4ConflictProbeOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMilliseconds(500);
}
