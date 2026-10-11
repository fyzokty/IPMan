using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;

namespace IPMan.App.Presentation;

/// <summary>What a result message may truthfully say about the network after the operation.</summary>
public enum ApplyNetworkEffect
{
    /// <summary>The message makes no claim about the network (success, no-op or non-mutating result).</summary>
    NotApplicable = 0,

    /// <summary>The operation stopped before any change was sent to Windows.</summary>
    Unchanged = 1,

    /// <summary>Some of the change reached Windows and some did not.</summary>
    Partial = 2,

    /// <summary>A change may have been sent but the final state could not be confirmed.</summary>
    Unverified = 3
}

/// <summary>A localized inline apply result and its visual importance.</summary>
/// <param name="Text">The complete localized text shown to the user (what happened, network state, next step).</param>
/// <param name="Severity">The visual importance of the result.</param>
/// <param name="Reason">The "what happened" sentence alone, for views that present the network state separately.</param>
/// <param name="Network">What the message claims about the network.</param>
public sealed record ApplyStatusMessage(
    string Text,
    ApplyStatusSeverity Severity,
    string? Reason = null,
    ApplyNetworkEffect Network = ApplyNetworkEffect.NotApplicable);

/// <summary>Translates application-layer networking outcomes into localized inline messages.</summary>
/// <remarks>
/// Failure texts follow the three-step formula: what happened, did the network change, what to do.
/// The "network did not change" assurance is attached only to outcomes that stop before any mutation.
/// </remarks>
public static class ApplyResultMessageFormatter
{
    /// <summary>Describes a static IPv4 apply outcome.</summary>
    public static ApplyStatusMessage Describe(StaticIpv4ApplyResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            StaticIpv4ApplyStatus.VerifiedSuccess => Success(Strings.ApplyStaticSuccess),
            StaticIpv4ApplyStatus.NoChange => Information(Strings.ApplyStaticNoChange),
            StaticIpv4ApplyStatus.ValidationFailed => InvalidValues(),
            StaticIpv4ApplyStatus.AdapterUnavailable => AdapterUnavailable(),
            StaticIpv4ApplyStatus.AdapterReadFailed => AdapterReadFailed(),
            StaticIpv4ApplyStatus.SafetyBlocked => DescribeSafetyBlock(result.SafetyBlock),
            StaticIpv4ApplyStatus.ConflictConfirmationRequired => ConflictUnconfirmed(),
            StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired => ProbeUnconfirmed(),
            StaticIpv4ApplyStatus.RecoveryCaptureFailed => RecoveryCaptureFailed(),
            StaticIpv4ApplyStatus.MutationFailed => MutationFailed(result.Mutation?.WasMutationAttempted != false),
            StaticIpv4ApplyStatus.PartialFailure => PartialFailure(),
            StaticIpv4ApplyStatus.VerificationFailed => VerificationFailed(),
            StaticIpv4ApplyStatus.AdapterUnavailableDuringVerification => AdapterLostDuringVerification(),
            StaticIpv4ApplyStatus.Cancelled => Cancelled(),
            StaticIpv4ApplyStatus.RecoveryStateUnavailable => RecoveryStateUnavailable(),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, null)
        };
    }

    /// <summary>Describes a DHCP apply outcome.</summary>
    public static ApplyStatusMessage Describe(DhcpApplyResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            DhcpApplyStatus.VerifiedSuccess => Success(Strings.ApplyDhcpSuccess),
            DhcpApplyStatus.NoChange => Information(Strings.ApplyDhcpNoChange),
            DhcpApplyStatus.AdapterUnavailable => AdapterUnavailable(),
            DhcpApplyStatus.AdapterReadFailed => AdapterReadFailed(),
            DhcpApplyStatus.SafetyBlocked => DescribeSafetyBlock(result.SafetyBlock),
            DhcpApplyStatus.RecoveryCaptureFailed => RecoveryCaptureFailed(),
            DhcpApplyStatus.MutationFailed => MutationFailed(result.Mutation?.WasMutationAttempted != false),
            DhcpApplyStatus.PartialFailure => PartialFailure(),
            DhcpApplyStatus.VerificationFailed => VerificationFailed(),
            DhcpApplyStatus.AdapterUnavailableDuringVerification => AdapterLostDuringVerification(),
            DhcpApplyStatus.Cancelled => Cancelled(),
            DhcpApplyStatus.RecoveryStateUnavailable => RecoveryStateUnavailable(),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, null)
        };
    }

    /// <summary>Describes a recovery restore outcome without replacing it with a delegated outcome.</summary>
    public static ApplyStatusMessage Describe(RecoveryRestoreResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            RecoveryRestoreStatus.VerifiedSuccess => Success(Strings.RestoreSuccess),
            RecoveryRestoreStatus.NoChange => Information(Strings.RestoreNoChange),
            RecoveryRestoreStatus.NoSnapshotFound => Compose(
                ApplyStatusSeverity.Information, Strings.RestoreNoSnapshotFound, ApplyNetworkEffect.Unchanged, null),
            RecoveryRestoreStatus.SnapshotUnreadable => Compose(
                ApplyStatusSeverity.Error, Strings.RestoreSnapshotUnreadable, ApplyNetworkEffect.Unchanged, Strings.NextManualEntry),
            RecoveryRestoreStatus.UnsupportedSnapshot => Compose(
                ApplyStatusSeverity.Warning, Strings.RestoreUnsupportedSnapshot, ApplyNetworkEffect.Unchanged, Strings.NextManualEntry),
            RecoveryRestoreStatus.AdapterUnavailable => AdapterUnavailable(),
            RecoveryRestoreStatus.AdapterReadFailed => AdapterReadFailed(),
            RecoveryRestoreStatus.AdapterIdentityChanged => Compose(
                ApplyStatusSeverity.Warning, Strings.RestoreAdapterIdentityChanged, ApplyNetworkEffect.Unchanged, Strings.NextSelectConnection),
            RecoveryRestoreStatus.SafetyBlocked => DescribeSafetyBlock(result.SafetyBlock),
            RecoveryRestoreStatus.ConflictConfirmationRequired => ConflictUnconfirmed(),
            RecoveryRestoreStatus.ProbeIndeterminateConfirmationRequired => ProbeUnconfirmed(),
            RecoveryRestoreStatus.RecoveryCaptureFailed => RecoveryCaptureFailed(),
            RecoveryRestoreStatus.MutationFailed => MutationFailed(
                (result.StaticOutcome?.Mutation ?? result.DhcpOutcome?.Mutation)?.WasMutationAttempted != false),
            RecoveryRestoreStatus.PartialFailure => PartialFailure(),
            RecoveryRestoreStatus.VerificationFailed => VerificationFailed(),
            RecoveryRestoreStatus.AdapterUnavailableDuringVerification => AdapterLostDuringVerification(),
            RecoveryRestoreStatus.Cancelled => Cancelled(),
            RecoveryRestoreStatus.RecoveryStateUnavailable => RecoveryStateUnavailable(),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, null)
        };
    }

    /// <summary>
    /// Describes input that was rejected locally before anything was sent to Windows and lists the fields to fix.
    /// </summary>
    /// <param name="fieldLabels">The localized labels of the invalid fields, one per line.</param>
    public static ApplyStatusMessage DescribeInvalidInput(string fieldLabels)
    {
        ArgumentNullException.ThrowIfNull(fieldLabels);

        ApplyStatusMessage message = InvalidValues();
        return message with { Text = string.Concat(message.Text, Environment.NewLine, fieldLabels) };
    }

    private static ApplyStatusMessage DescribeSafetyBlock(StaticIpv4SafetyBlock safetyBlock) =>
        safetyBlock switch
        {
            StaticIpv4SafetyBlock.None => throw new ArgumentOutOfRangeException(
                nameof(safetyBlock),
                safetyBlock,
                null),
            StaticIpv4SafetyBlock.MultipleIpv4Addresses => Compose(
                ApplyStatusSeverity.Warning, Strings.SafetyMultipleIpv4Addresses, ApplyNetworkEffect.Unchanged,
                Strings.SafetyMultipleIpv4AddressesAction),
            StaticIpv4SafetyBlock.MultipleIpv4Gateways => Compose(
                ApplyStatusSeverity.Warning, Strings.SafetyMultipleIpv4Gateways, ApplyNetworkEffect.Unchanged,
                Strings.SafetyMultipleIpv4GatewaysAction),
            StaticIpv4SafetyBlock.TooManyIpv4DnsServers => Compose(
                ApplyStatusSeverity.Warning, Strings.SafetyTooManyIpv4DnsServers, ApplyNetworkEffect.Unchanged,
                Strings.SafetyTooManyIpv4DnsServersAction),
            StaticIpv4SafetyBlock.NotElevated => Compose(
                ApplyStatusSeverity.Warning, Strings.SafetyNotElevated, ApplyNetworkEffect.Unchanged,
                Strings.SafetyNotElevatedAction),
            _ => throw new ArgumentOutOfRangeException(nameof(safetyBlock), safetyBlock, null)
        };

    private static ApplyStatusMessage InvalidValues() => Compose(
        ApplyStatusSeverity.Error, Strings.ApplyValidationFailed, ApplyNetworkEffect.Unchanged, Strings.ApplyValidationAction);

    private static ApplyStatusMessage AdapterUnavailable() => Compose(
        ApplyStatusSeverity.Error, Strings.ApplyAdapterUnavailable, ApplyNetworkEffect.Unchanged, Strings.NextRefreshAndRetry);

    private static ApplyStatusMessage AdapterReadFailed() => Compose(
        ApplyStatusSeverity.Error, Strings.ApplyAdapterReadFailed, ApplyNetworkEffect.Unchanged, Strings.NextRetry);

    private static ApplyStatusMessage ConflictUnconfirmed() => Compose(
        ApplyStatusSeverity.Warning, Strings.ApplyConflictUnconfirmed, ApplyNetworkEffect.Unchanged,
        Strings.ApplyConflictUnconfirmedAction);

    private static ApplyStatusMessage ProbeUnconfirmed() => Compose(
        ApplyStatusSeverity.Warning, Strings.ApplyProbeIndeterminateUnconfirmed, ApplyNetworkEffect.Unchanged,
        Strings.ApplyProbeIndeterminateUnconfirmedAction);

    private static ApplyStatusMessage RecoveryCaptureFailed() => Compose(
        ApplyStatusSeverity.Error, Strings.ApplyRecoveryCaptureFailed, ApplyNetworkEffect.Unchanged, Strings.NextFixDisk);

    private static ApplyStatusMessage RecoveryStateUnavailable() => Compose(
        ApplyStatusSeverity.Error, Strings.ApplyRecoveryStateUnavailable, ApplyNetworkEffect.Unchanged, Strings.NextWaitReady);

    private static ApplyStatusMessage Cancelled() => Compose(
        ApplyStatusSeverity.Information, Strings.ApplyCancelled, ApplyNetworkEffect.Unchanged, Strings.NextRetryIfWanted);

    // A mutation that never reached Windows leaves the network untouched; once it was attempted the
    // outcome of a rejected, non-partial change cannot be promised without a verified read.
    private static ApplyStatusMessage MutationFailed(bool mutationAttempted) => mutationAttempted
        ? Compose(ApplyStatusSeverity.Error, Strings.ApplyMutationFailed, ApplyNetworkEffect.Unverified, Strings.NextCheckDetails)
        : Compose(ApplyStatusSeverity.Error, Strings.ApplyMutationFailed, ApplyNetworkEffect.Unchanged, Strings.ApplyMutationFailedAction);

    private static ApplyStatusMessage PartialFailure() => Compose(
        ApplyStatusSeverity.Error, Strings.ApplyPartialFailure, ApplyNetworkEffect.Partial, Strings.ApplyPartialFailureAction);

    private static ApplyStatusMessage VerificationFailed() => Compose(
        ApplyStatusSeverity.Error, Strings.ApplyVerificationFailed, ApplyNetworkEffect.Unverified, Strings.NextCheckDetails);

    private static ApplyStatusMessage AdapterLostDuringVerification() => Compose(
        ApplyStatusSeverity.Error, Strings.ApplyAdapterUnavailableDuringVerification, ApplyNetworkEffect.Unverified,
        Strings.ApplyAdapterUnavailableDuringVerificationAction);

    private static ApplyStatusMessage Compose(
        ApplyStatusSeverity severity, string reason, ApplyNetworkEffect network, string? next)
    {
        string? state = network switch
        {
            ApplyNetworkEffect.Unchanged => Strings.NetworkUnchanged,
            ApplyNetworkEffect.Partial => Strings.NetworkPartial,
            ApplyNetworkEffect.Unverified => Strings.NetworkUnverified,
            _ => null
        };
        string text = string.Join(' ', new[] { reason, state, next }.Where(part => !string.IsNullOrEmpty(part)));
        return new ApplyStatusMessage(text, severity, reason, network);
    }

    private static ApplyStatusMessage Success(string text) =>
        new(text, ApplyStatusSeverity.Success);

    private static ApplyStatusMessage Information(string text) =>
        new(text, ApplyStatusSeverity.Information);
}
