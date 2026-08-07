namespace IPMan.Domain.Networking;

/// <summary>One structured, localizable validation failure.</summary>
public sealed record StaticIpv4ValidationError(
    StaticIpv4ConfigurationField Field,
    StaticIpv4ValidationErrorCode Code);
