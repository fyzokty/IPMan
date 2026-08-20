using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Identifies the adapter that should transition to DHCP.</summary>
/// <param name="AdapterId">The stable Windows adapter identity.</param>
public sealed record DhcpApplyRequest(NetworkAdapterId AdapterId);
