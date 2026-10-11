using IPMan.App.Resources;
using IPMan.Application.Networking;

namespace IPMan.App.Presentation;

public sealed record DetailsFailureMessage(string Title, string Explanation, string NetworkState, string TechnicalDetails);

/// <summary>Only outcomes stopped before mutation may promise an unchanged network.</summary>
public static class DetailsFailureMessageFormatter
{
    public static DetailsFailureMessage? Describe(StaticIpv4ApplyResult result, string settingName, string previousName) =>
        result.Status is StaticIpv4ApplyStatus.VerifiedSuccess or StaticIpv4ApplyStatus.NoChange ? null :
        Create(ApplyResultMessageFormatter.Describe(result), settingName, previousName,
            (result.Status == StaticIpv4ApplyStatus.MutationFailed && result.Mutation?.WasMutationAttempted == false) ||
            result.Status is StaticIpv4ApplyStatus.ValidationFailed or StaticIpv4ApplyStatus.AdapterUnavailable
                or StaticIpv4ApplyStatus.AdapterReadFailed or StaticIpv4ApplyStatus.SafetyBlocked
                or StaticIpv4ApplyStatus.ConflictConfirmationRequired or StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired
                or StaticIpv4ApplyStatus.RecoveryCaptureFailed or StaticIpv4ApplyStatus.RecoveryStateUnavailable or StaticIpv4ApplyStatus.Cancelled,
            result.Status == StaticIpv4ApplyStatus.MutationFailed, result.Status == StaticIpv4ApplyStatus.PartialFailure, result.ToString());

    public static DetailsFailureMessage? Describe(DhcpApplyResult result, string settingName, string previousName) =>
        result.Status is DhcpApplyStatus.VerifiedSuccess or DhcpApplyStatus.NoChange ? null :
        Create(ApplyResultMessageFormatter.Describe(result), settingName, previousName,
            (result.Status == DhcpApplyStatus.MutationFailed && result.Mutation?.WasMutationAttempted == false) ||
            result.Status is DhcpApplyStatus.AdapterUnavailable or DhcpApplyStatus.AdapterReadFailed
                or DhcpApplyStatus.SafetyBlocked or DhcpApplyStatus.RecoveryCaptureFailed or DhcpApplyStatus.RecoveryStateUnavailable or DhcpApplyStatus.Cancelled,
            result.Status == DhcpApplyStatus.MutationFailed, result.Status == DhcpApplyStatus.PartialFailure, result.ToString());

    private static DetailsFailureMessage Create(ApplyStatusMessage message, string setting, string previous,
        bool unchanged, bool mutationFailed, bool partial, string technical) => new(
            Strings.FormatDetailsFailureTitle(setting),
            mutationFailed ? Strings.DetailsMutationRejected : partial ? Strings.DetailsPartialExplanation : message.Text,
            unchanged ? Strings.FormatDetailsUnchanged(previous)
                : partial ? Strings.DetailsPartial : Strings.DetailsUncertain, technical);
}
