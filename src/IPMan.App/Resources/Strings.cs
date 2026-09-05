using System.Globalization;
using System.Resources;

namespace IPMan.App.Resources;

/// <summary>
/// Typed access to the localized UI strings in <c>Strings.resx</c>.
/// All user-facing text must be resolved through this class; no user-facing
/// literal belongs in ViewModels, views or services.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager Manager =
        new("IPMan.App.Resources.Strings", typeof(Strings).Assembly);

    public static string ApplicationTagline => Get(nameof(ApplicationTagline));

    public static string CurrentConfigurationHeader => Get(nameof(CurrentConfigurationHeader));

    public static string DraftConfigurationHeader => Get(nameof(DraftConfigurationHeader));

    public static string DraftConfigurationHint => Get(nameof(DraftConfigurationHint));

    public static string FieldConnectionState => Get(nameof(FieldConnectionState));

    public static string FieldAdapterName => Get(nameof(FieldAdapterName));

    public static string FieldDescription => Get(nameof(FieldDescription));

    public static string FieldMacAddress => Get(nameof(FieldMacAddress));

    public static string FieldIpv4Address => Get(nameof(FieldIpv4Address));

    public static string FieldSubnetMask => Get(nameof(FieldSubnetMask));

    public static string FieldGateway => Get(nameof(FieldGateway));

    public static string FieldPrimaryDns => Get(nameof(FieldPrimaryDns));

    public static string FieldSecondaryDns => Get(nameof(FieldSecondaryDns));

    public static string FieldConfigurationMode => Get(nameof(FieldConfigurationMode));

    public static string FieldLinkSpeed => Get(nameof(FieldLinkSpeed));

    public static string FieldAdditionalIpv4Addresses => Get(nameof(FieldAdditionalIpv4Addresses));

    public static string ConnectionStateConnected => Get(nameof(ConnectionStateConnected));

    public static string ConnectionStateDisconnected => Get(nameof(ConnectionStateDisconnected));

    public static string ModeDhcp => Get(nameof(ModeDhcp));

    public static string ModeStatic => Get(nameof(ModeStatic));

    public static string ModeUnknown => Get(nameof(ModeUnknown));

    public static string ValueUnavailable => Get(nameof(ValueUnavailable));

    public static string CommandGetCurrentValues => Get(nameof(CommandGetCurrentValues));

    public static string CommandCopy => Get(nameof(CommandCopy));

    /// <summary>Gets the static apply command label.</summary>
    public static string CommandApply => Get(nameof(CommandApply));

    /// <summary>Gets the DHCP command label.</summary>
    public static string CommandUseDhcp => Get(nameof(CommandUseDhcp));

    /// <summary>Gets the recovery restore command label.</summary>
    public static string CommandRestoreLast => Get(nameof(CommandRestoreLast));

    public static string StateLoading => Get(nameof(StateLoading));

    public static string StateReady => Get(nameof(StateReady));

    public static string StateError => Get(nameof(StateError));

    /// <summary>Gets the application state shown while a network action is running.</summary>
    public static string StateApplying => Get(nameof(StateApplying));

    /// <summary>Gets the message shown when draft validation fails.</summary>
    public static string ApplyValidationFailed => Get(nameof(ApplyValidationFailed));

    /// <summary>Gets the successful static apply message.</summary>
    public static string ApplyStaticSuccess => Get(nameof(ApplyStaticSuccess));

    /// <summary>Gets the unchanged static apply message.</summary>
    public static string ApplyStaticNoChange => Get(nameof(ApplyStaticNoChange));

    /// <summary>Gets the successful DHCP apply message.</summary>
    public static string ApplyDhcpSuccess => Get(nameof(ApplyDhcpSuccess));

    /// <summary>Gets the unchanged DHCP apply message.</summary>
    public static string ApplyDhcpNoChange => Get(nameof(ApplyDhcpNoChange));

    /// <summary>Gets the successful restore message.</summary>
    public static string RestoreSuccess => Get(nameof(RestoreSuccess));

    /// <summary>Gets the unchanged restore message.</summary>
    public static string RestoreNoChange => Get(nameof(RestoreNoChange));

    /// <summary>Gets the missing recovery snapshot message.</summary>
    public static string RestoreNoSnapshotFound => Get(nameof(RestoreNoSnapshotFound));

    /// <summary>Gets the unreadable recovery snapshot message.</summary>
    public static string RestoreSnapshotUnreadable => Get(nameof(RestoreSnapshotUnreadable));

    /// <summary>Gets the unsupported recovery snapshot message.</summary>
    public static string RestoreUnsupportedSnapshot => Get(nameof(RestoreUnsupportedSnapshot));

    /// <summary>Gets the changed adapter identity message.</summary>
    public static string RestoreAdapterIdentityChanged => Get(nameof(RestoreAdapterIdentityChanged));

    /// <summary>Gets the unavailable adapter message.</summary>
    public static string ApplyAdapterUnavailable => Get(nameof(ApplyAdapterUnavailable));

    /// <summary>Gets the unreliable adapter read message.</summary>
    public static string ApplyAdapterReadFailed => Get(nameof(ApplyAdapterReadFailed));

    /// <summary>Gets the cancellation message.</summary>
    public static string ApplyCancelled => Get(nameof(ApplyCancelled));

    /// <summary>Gets the recovery capture failure message.</summary>
    public static string ApplyRecoveryCaptureFailed => Get(nameof(ApplyRecoveryCaptureFailed));

    /// <summary>Gets the unavailable recovery state message.</summary>
    public static string ApplyRecoveryStateUnavailable => Get(nameof(ApplyRecoveryStateUnavailable));

    /// <summary>Gets the mutation failure message.</summary>
    public static string ApplyMutationFailed => Get(nameof(ApplyMutationFailed));

    /// <summary>Gets the partial failure message.</summary>
    public static string ApplyPartialFailure => Get(nameof(ApplyPartialFailure));

    /// <summary>Gets the verification failure message.</summary>
    public static string ApplyVerificationFailed => Get(nameof(ApplyVerificationFailed));

    /// <summary>Gets the adapter-disappeared verification message.</summary>
    public static string ApplyAdapterUnavailableDuringVerification =>
        Get(nameof(ApplyAdapterUnavailableDuringVerification));

    /// <summary>Gets the declined action message.</summary>
    public static string ActionDeclined => Get(nameof(ActionDeclined));

    /// <summary>Gets the message shown when conflict confirmation was not obtained.</summary>
    public static string ApplyConflictUnconfirmed => Get(nameof(ApplyConflictUnconfirmed));

    /// <summary>Gets the message shown when indeterminate-probe confirmation was not obtained.</summary>
    public static string ApplyProbeIndeterminateUnconfirmed => Get(nameof(ApplyProbeIndeterminateUnconfirmed));

    /// <summary>Gets the multiple-address safety message.</summary>
    public static string SafetyMultipleIpv4Addresses => Get(nameof(SafetyMultipleIpv4Addresses));

    /// <summary>Gets the multiple-gateway safety message.</summary>
    public static string SafetyMultipleIpv4Gateways => Get(nameof(SafetyMultipleIpv4Gateways));

    /// <summary>Gets the excessive-DNS safety message.</summary>
    public static string SafetyTooManyIpv4DnsServers => Get(nameof(SafetyTooManyIpv4DnsServers));

    /// <summary>Gets the elevation-required safety message.</summary>
    public static string SafetyNotElevated => Get(nameof(SafetyNotElevated));

    /// <summary>Gets the potential conflict prompt title.</summary>
    public static string ConfirmConflictTitle => Get(nameof(ConfirmConflictTitle));

    /// <summary>Gets the indeterminate probe prompt title.</summary>
    public static string ConfirmProbeIndeterminateTitle => Get(nameof(ConfirmProbeIndeterminateTitle));

    /// <summary>Gets the indeterminate probe prompt message.</summary>
    public static string ConfirmProbeIndeterminateMessage => Get(nameof(ConfirmProbeIndeterminateMessage));

    /// <summary>Gets the recovery restore prompt title.</summary>
    public static string ConfirmRestoreTitle => Get(nameof(ConfirmRestoreTitle));

    /// <summary>Gets the non-contiguous subnet mask validation message.</summary>
    public static string ValidationNonContiguousSubnetMask => Get(nameof(ValidationNonContiguousSubnetMask));

    /// <summary>Gets the network-address validation message.</summary>
    public static string ValidationNetworkAddressNotAllowed => Get(nameof(ValidationNetworkAddressNotAllowed));

    /// <summary>Gets the broadcast-address validation message.</summary>
    public static string ValidationBroadcastAddressNotAllowed => Get(nameof(ValidationBroadcastAddressNotAllowed));

    /// <summary>Gets the gateway-outside-subnet validation message.</summary>
    public static string ValidationGatewayOutsideSubnet => Get(nameof(ValidationGatewayOutsideSubnet));

    /// <summary>Gets the primary-DNS-required validation message.</summary>
    public static string ValidationPrimaryDnsRequired => Get(nameof(ValidationPrimaryDnsRequired));

    /// <summary>Gets the gateway-equals-host validation message.</summary>
    public static string ValidationGatewayMatchesHostAddress => Get(nameof(ValidationGatewayMatchesHostAddress));

    public static string LoadingAdapters => Get(nameof(LoadingAdapters));

    public static string EmptyStateTitle => Get(nameof(EmptyStateTitle));

    public static string EmptyStateDescription => Get(nameof(EmptyStateDescription));

    public static string RefreshFailedTitle => Get(nameof(RefreshFailedTitle));

    public static string ExistingInstanceNotResponding => Get(nameof(ExistingInstanceNotResponding));

    public static string StatusAdministratorYes => Get(nameof(StatusAdministratorYes));

    public static string StatusAdministratorNo => Get(nameof(StatusAdministratorNo));

    public static string StatusNoSelectedAdapter => Get(nameof(StatusNoSelectedAdapter));

    public static string StatusLastRefreshNever => Get(nameof(StatusLastRefreshNever));

    public static string FormatLinkSpeedGigabits(string value, IFormatProvider formatProvider) =>
        Format(Get("LinkSpeedGigabitsPerSecond"), formatProvider, value);

    public static string FormatLinkSpeedMegabits(string value, IFormatProvider formatProvider) =>
        Format(Get("LinkSpeedMegabitsPerSecond"), formatProvider, value);

    public static string FormatLinkSpeedKilobits(string value, IFormatProvider formatProvider) =>
        Format(Get("LinkSpeedKilobitsPerSecond"), formatProvider, value);

    public static string FormatCopyFieldTooltip(string fieldLabel) =>
        Format(Get("CommandCopyFieldTooltip"), CultureInfo.CurrentCulture, fieldLabel);

    public static string FormatSelectedAdapter(string adapterName, string connectionState) =>
        Format(Get("StatusSelectedAdapter"), CultureInfo.CurrentCulture, adapterName, connectionState);

    public static string FormatLastRefresh(string time) =>
        Format(Get("StatusLastRefresh"), CultureInfo.CurrentCulture, time);

    public static string FormatVersion(string version) =>
        Format(Get("StatusVersion"), CultureInfo.CurrentCulture, version);

    /// <summary>Formats the conflict confirmation message for an IPv4 address.</summary>
    public static string FormatConfirmConflictMessage(string address) =>
        Format(Get("ConfirmConflictMessage"), CultureInfo.CurrentCulture, address);

    /// <summary>Formats the restore confirmation message for an adapter.</summary>
    public static string FormatConfirmRestoreMessage(string adapterName) =>
        Format(Get("ConfirmRestoreMessage"), CultureInfo.CurrentCulture, adapterName);

    /// <summary>Formats a required-field validation message.</summary>
    public static string FormatValidationRequired(string fieldLabel) =>
        Format(Get("ValidationRequired"), CultureInfo.CurrentCulture, fieldLabel);

    /// <summary>Formats an invalid-IPv4 validation message.</summary>
    public static string FormatValidationInvalidIpv4(string fieldLabel) =>
        Format(Get("ValidationInvalidIpv4"), CultureInfo.CurrentCulture, fieldLabel);

    /// <summary>Formats a disallowed-address validation message.</summary>
    public static string FormatValidationAddressNotAllowed(string fieldLabel) =>
        Format(Get("ValidationAddressNotAllowed"), CultureInfo.CurrentCulture, fieldLabel);

    private static string Get(string name) =>
        Manager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    private static string Format(
        string format,
        IFormatProvider formatProvider,
        params object?[] arguments) =>
        string.Format(formatProvider, format, arguments);
}
