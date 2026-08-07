using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public sealed record StaticIpv4ApplyRequest(
    NetworkAdapterId AdapterId,
    StaticIpv4Configuration DesiredConfiguration,
    bool ConfirmPotentialConflict = false,
    bool ContinueAfterIndeterminateProbe = false);
