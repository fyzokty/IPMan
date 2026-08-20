using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

/// <summary>Requests restoration of the latest snapshot for an adapter.</summary>
/// <param name="AdapterId">The stable identity of the adapter to restore.</param>
/// <param name="ConfirmPotentialConflict">Whether the caller confirmed a potential address conflict.</param>
/// <param name="ContinueAfterIndeterminateProbe">Whether the caller accepted an indeterminate conflict probe.</param>
public sealed record RecoveryRestoreRequest(
    NetworkAdapterId AdapterId,
    bool ConfirmPotentialConflict = false,
    bool ContinueAfterIndeterminateProbe = false);
