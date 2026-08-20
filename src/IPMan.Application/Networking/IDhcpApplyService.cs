namespace IPMan.Application.Networking;

/// <summary>Coordinates a safe, recoverable transition to DHCP.</summary>
public interface IDhcpApplyService
{
    /// <summary>Applies DHCP to the requested adapter and verifies fresh Windows state.</summary>
    Task<DhcpApplyResult> ApplyAsync(
        DhcpApplyRequest request,
        CancellationToken cancellationToken);
}
