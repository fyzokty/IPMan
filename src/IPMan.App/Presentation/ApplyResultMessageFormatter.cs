using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;

namespace IPMan.App.Presentation;

/// <summary>A localized inline apply result and its visual importance.</summary>
/// <param name="Text">The localized text shown to the user.</param>
/// <param name="Severity">The visual importance of the result.</param>
public sealed record ApplyStatusMessage(string Text, ApplyStatusSeverity Severity);

/// <summary>Translates application-layer networking outcomes into localized inline messages.</summary>
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
            StaticIpv4ApplyStatus.ValidationFailed => Error(Strings.ApplyValidationFailed),
            StaticIpv4ApplyStatus.AdapterUnavailable => Error(Strings.ApplyAdapterUnavailable),
            StaticIpv4ApplyStatus.AdapterReadFailed => Error(Strings.ApplyAdapterReadFailed),
            StaticIpv4ApplyStatus.SafetyBlocked => DescribeSafetyBlock(result.SafetyBlock),
            StaticIpv4ApplyStatus.ConflictConfirmationRequired => Warning(Strings.ApplyConflictUnconfirmed),
            StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired =>
                Warning(Strings.ApplyProbeIndeterminateUnconfirmed),
            StaticIpv4ApplyStatus.RecoveryCaptureFailed => Error(Strings.ApplyRecoveryCaptureFailed),
            StaticIpv4ApplyStatus.MutationFailed => Error(Strings.ApplyMutationFailed),
            StaticIpv4ApplyStatus.PartialFailure => Error(Strings.ApplyPartialFailure),
            StaticIpv4ApplyStatus.VerificationFailed => Error(Strings.ApplyVerificationFailed),
            StaticIpv4ApplyStatus.AdapterUnavailableDuringVerification =>
                Error(Strings.ApplyAdapterUnavailableDuringVerification),
            StaticIpv4ApplyStatus.Cancelled => Information(Strings.ApplyCancelled),
            StaticIpv4ApplyStatus.RecoveryStateUnavailable => Error(Strings.ApplyRecoveryStateUnavailable),
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
            DhcpApplyStatus.AdapterUnavailable => Error(Strings.ApplyAdapterUnavailable),
            DhcpApplyStatus.AdapterReadFailed => Error(Strings.ApplyAdapterReadFailed),
            DhcpApplyStatus.SafetyBlocked => DescribeSafetyBlock(result.SafetyBlock),
            DhcpApplyStatus.RecoveryCaptureFailed => Error(Strings.ApplyRecoveryCaptureFailed),
            DhcpApplyStatus.MutationFailed => Error(Strings.ApplyMutationFailed),
            DhcpApplyStatus.PartialFailure => Error(Strings.ApplyPartialFailure),
            DhcpApplyStatus.VerificationFailed => Error(Strings.ApplyVerificationFailed),
            DhcpApplyStatus.AdapterUnavailableDuringVerification =>
                Error(Strings.ApplyAdapterUnavailableDuringVerification),
            DhcpApplyStatus.Cancelled => Information(Strings.ApplyCancelled),
            DhcpApplyStatus.RecoveryStateUnavailable => Error(Strings.ApplyRecoveryStateUnavailable),
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
            RecoveryRestoreStatus.NoSnapshotFound => Information(Strings.RestoreNoSnapshotFound),
            RecoveryRestoreStatus.SnapshotUnreadable => Error(Strings.RestoreSnapshotUnreadable),
            RecoveryRestoreStatus.UnsupportedSnapshot => Warning(Strings.RestoreUnsupportedSnapshot),
            RecoveryRestoreStatus.AdapterUnavailable => Error(Strings.ApplyAdapterUnavailable),
            RecoveryRestoreStatus.AdapterReadFailed => Error(Strings.ApplyAdapterReadFailed),
            RecoveryRestoreStatus.AdapterIdentityChanged => Warning(Strings.RestoreAdapterIdentityChanged),
            RecoveryRestoreStatus.SafetyBlocked => DescribeSafetyBlock(result.SafetyBlock),
            RecoveryRestoreStatus.ConflictConfirmationRequired => Warning(Strings.ApplyConflictUnconfirmed),
            RecoveryRestoreStatus.ProbeIndeterminateConfirmationRequired =>
                Warning(Strings.ApplyProbeIndeterminateUnconfirmed),
            RecoveryRestoreStatus.RecoveryCaptureFailed => Error(Strings.ApplyRecoveryCaptureFailed),
            RecoveryRestoreStatus.MutationFailed => Error(Strings.ApplyMutationFailed),
            RecoveryRestoreStatus.PartialFailure => Error(Strings.ApplyPartialFailure),
            RecoveryRestoreStatus.VerificationFailed => Error(Strings.ApplyVerificationFailed),
            RecoveryRestoreStatus.AdapterUnavailableDuringVerification =>
                Error(Strings.ApplyAdapterUnavailableDuringVerification),
            RecoveryRestoreStatus.Cancelled => Information(Strings.ApplyCancelled),
            RecoveryRestoreStatus.RecoveryStateUnavailable => Error(Strings.ApplyRecoveryStateUnavailable),
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, null)
        };
    }

    private static ApplyStatusMessage DescribeSafetyBlock(StaticIpv4SafetyBlock safetyBlock) =>
        safetyBlock switch
        {
            StaticIpv4SafetyBlock.None => throw new ArgumentOutOfRangeException(
                nameof(safetyBlock),
                safetyBlock,
                null),
            StaticIpv4SafetyBlock.MultipleIpv4Addresses => Warning(Strings.SafetyMultipleIpv4Addresses),
            StaticIpv4SafetyBlock.MultipleIpv4Gateways => Warning(Strings.SafetyMultipleIpv4Gateways),
            StaticIpv4SafetyBlock.TooManyIpv4DnsServers => Warning(Strings.SafetyTooManyIpv4DnsServers),
            StaticIpv4SafetyBlock.NotElevated => Warning(Strings.SafetyNotElevated),
            _ => throw new ArgumentOutOfRangeException(nameof(safetyBlock), safetyBlock, null)
        };

    private static ApplyStatusMessage Success(string text) =>
        new(text, ApplyStatusSeverity.Success);

    private static ApplyStatusMessage Information(string text) =>
        new(text, ApplyStatusSeverity.Information);

    private static ApplyStatusMessage Warning(string text) =>
        new(text, ApplyStatusSeverity.Warning);

    private static ApplyStatusMessage Error(string text) =>
        new(text, ApplyStatusSeverity.Error);
}
