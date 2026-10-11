using IPMan.App.Resources;
using IPMan.Application.Networking;

namespace IPMan.App.Presentation;

public sealed record DetailsFailureMessage(string Title, string Explanation, string NetworkState, string TechnicalDetails);

/// <summary>Only outcomes stopped before mutation may promise an unchanged network.</summary>
public static class DetailsFailureMessageFormatter
{
    public static DetailsFailureMessage? Describe(StaticIpv4ApplyResult result, string settingName, string previousName) =>
        result.Status is StaticIpv4ApplyStatus.VerifiedSuccess or StaticIpv4ApplyStatus.NoChange ? null :
        Create(ApplyResultMessageFormatter.Describe(result), settingName, previousName, result.ToString());

    public static DetailsFailureMessage? Describe(DhcpApplyResult result, string settingName, string previousName) =>
        result.Status is DhcpApplyStatus.VerifiedSuccess or DhcpApplyStatus.NoChange ? null :
        Create(ApplyResultMessageFormatter.Describe(result), settingName, previousName, result.ToString());

    // The explanation is the "what happened" sentence alone; the network state is shown on its own line,
    // so the assurance comes from the same classification the inline status message uses.
    private static DetailsFailureMessage Create(ApplyStatusMessage message, string setting, string previous, string technical) => new(
        Strings.FormatDetailsFailureTitle(setting),
        message.Reason ?? message.Text,
        message.Network switch
        {
            ApplyNetworkEffect.Unchanged => Strings.FormatDetailsUnchanged(previous),
            ApplyNetworkEffect.Partial => Strings.DetailsPartial,
            _ => Strings.DetailsUncertain
        },
        technical);
}
