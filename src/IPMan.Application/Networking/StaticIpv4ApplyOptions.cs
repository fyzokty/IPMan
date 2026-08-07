namespace IPMan.Application.Networking;

public sealed class StaticIpv4ApplyOptions
{
    public int VerificationAttempts { get; init; } = 4;

    public TimeSpan VerificationDelay { get; init; } = TimeSpan.FromMilliseconds(500);
}
