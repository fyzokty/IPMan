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

    public static string StateLoading => Get(nameof(StateLoading));

    public static string StateReady => Get(nameof(StateReady));

    public static string StateError => Get(nameof(StateError));

    public static string LoadingAdapters => Get(nameof(LoadingAdapters));

    public static string EmptyStateTitle => Get(nameof(EmptyStateTitle));

    public static string EmptyStateDescription => Get(nameof(EmptyStateDescription));

    public static string RefreshFailedTitle => Get(nameof(RefreshFailedTitle));

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

    private static string Get(string name) =>
        Manager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    private static string Format(
        string format,
        IFormatProvider formatProvider,
        params object?[] arguments) =>
        string.Format(formatProvider, format, arguments);
}
