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

    public static string DiagnosticViewHeader => Get(nameof(DiagnosticViewHeader));

    public static string ColumnName => Get(nameof(ColumnName));

    public static string ColumnDescription => Get(nameof(ColumnDescription));

    public static string ColumnConnectionState => Get(nameof(ColumnConnectionState));

    public static string ColumnMode => Get(nameof(ColumnMode));

    public static string ColumnIpv4Address => Get(nameof(ColumnIpv4Address));

    public static string ColumnSubnetMask => Get(nameof(ColumnSubnetMask));

    public static string ColumnGateway => Get(nameof(ColumnGateway));

    public static string ColumnPrimaryDns => Get(nameof(ColumnPrimaryDns));

    public static string ColumnSecondaryDns => Get(nameof(ColumnSecondaryDns));

    public static string ColumnAdditionalIpv4Addresses => Get(nameof(ColumnAdditionalIpv4Addresses));

    public static string ColumnMacAddress => Get(nameof(ColumnMacAddress));

    public static string ColumnLinkSpeed => Get(nameof(ColumnLinkSpeed));

    public static string ConnectionStateConnected => Get(nameof(ConnectionStateConnected));

    public static string ConnectionStateDisconnected => Get(nameof(ConnectionStateDisconnected));

    public static string ModeDhcp => Get(nameof(ModeDhcp));

    public static string ModeStatic => Get(nameof(ModeStatic));

    public static string ModeUnknown => Get(nameof(ModeUnknown));

    public static string ValueUnavailable => Get(nameof(ValueUnavailable));

    public static string StatusNotStarted => Get(nameof(StatusNotStarted));

    public static string FormatLinkSpeedMegabitsPerSecond(long megabitsPerSecond) =>
        Format(Get("LinkSpeedMegabitsPerSecond"), megabitsPerSecond);

    public static string FormatStatusRefreshed(int adapterCount, string reason) =>
        Format(Get("StatusRefreshed"), adapterCount, reason);

    public static string FormatStatusRefreshFailed(string reason) =>
        Format(Get("StatusRefreshFailed"), reason);

    private static string Get(string name) =>
        Manager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    private static string Format(string format, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, format, arguments);
}
