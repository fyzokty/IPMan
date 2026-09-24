using System.Globalization;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.App.Presentation;

/// <summary>
/// Deterministic presentation formatting for adapter values.
/// <para>
/// Kept out of XAML so every rule is unit-testable. Nothing here invents a
/// value: anything Windows did not report becomes the unavailable placeholder.
/// </para>
/// </summary>
public static class AdapterDisplayFormatter
{
    private const long BitsPerKilobit = 1_000L;
    private const long BitsPerMegabit = 1_000_000L;
    private const long BitsPerGigabit = 1_000_000_000L;

    /// <summary>Returns the value, or the unavailable placeholder when absent.</summary>
    public static string OrUnavailable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Strings.ValueUnavailable : value;

    public static string FormatConnectionState(bool isConnected) =>
        isConnected ? Strings.ConnectionStateConnected : Strings.ConnectionStateDisconnected;

    public static string FormatConfigurationMode(NetworkConfigurationMode mode) => mode switch
    {
        NetworkConfigurationMode.Dhcp => Strings.ModeDhcp,
        NetworkConfigurationMode.Static => Strings.ModeStatic,
        _ => Strings.ModeUnknown
    };

    /// <summary>
    /// Renders a link speed as Gbps/Mbps/Kbps, trimming a trailing ",0"/".0".
    /// An unreported or non-positive speed yields the unavailable placeholder.
    /// </summary>
    public static string FormatLinkSpeed(long? linkSpeedBitsPerSecond, IFormatProvider formatProvider)
    {
        ArgumentNullException.ThrowIfNull(formatProvider);

        if (linkSpeedBitsPerSecond is not > 0)
        {
            return Strings.ValueUnavailable;
        }

        long bitsPerSecond = linkSpeedBitsPerSecond.Value;

        if (bitsPerSecond >= BitsPerGigabit)
        {
            return Strings.FormatLinkSpeedGigabits(
                FormatScaled(bitsPerSecond, BitsPerGigabit, formatProvider),
                formatProvider);
        }

        if (bitsPerSecond >= BitsPerMegabit)
        {
            return Strings.FormatLinkSpeedMegabits(
                FormatScaled(bitsPerSecond, BitsPerMegabit, formatProvider),
                formatProvider);
        }

        if (bitsPerSecond >= BitsPerKilobit)
        {
            return Strings.FormatLinkSpeedKilobits(
                FormatScaled(bitsPerSecond, BitsPerKilobit, formatProvider),
                formatProvider);
        }

        return Strings.FormatLinkSpeedKilobits("0", formatProvider);
    }

    /// <summary>
    /// Joins the IPv4 addresses configured besides the primary one, or returns
    /// the unavailable placeholder when there are none.
    /// </summary>
    public static string FormatAdditionalIpv4Addresses(
        IReadOnlyList<Ipv4AddressAssignment> additionalAddresses)
    {
        ArgumentNullException.ThrowIfNull(additionalAddresses);

        return additionalAddresses.Count == 0
            ? Strings.ValueUnavailable
            : string.Join(", ", additionalAddresses.Select(address => address.Address));
    }

    /// <summary>Formats a refresh timestamp in the user's local time.</summary>
    public static string FormatRefreshTime(
        DateTimeOffset? refreshedAtUtc,
        IFormatProvider formatProvider)
    {
        ArgumentNullException.ThrowIfNull(formatProvider);

        return refreshedAtUtc is null
            ? Strings.StatusLastRefreshNever
            : Strings.FormatLastRefresh(
                refreshedAtUtc.Value.ToLocalTime().ToString("HH:mm:ss", formatProvider));
    }

    /// <summary>Formats a Turkish plain-text snapshot suitable for the clipboard.</summary>
    public static string FormatCopySummary(NetworkAdapterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        string addresses = snapshot.Ipv4Addresses.Count == 0
            ? Strings.ValueUnavailable
            : string.Join(
                ", ",
                snapshot.Ipv4Addresses.Select(address => string.IsNullOrWhiteSpace(address.SubnetMask)
                    ? address.Address
                    : $"{address.Address} ({address.SubnetMask})"));

        return string.Join(
            Environment.NewLine,
            $"{Strings.FieldAdapterName}: {OrUnavailable(snapshot.Name)}",
            $"{Strings.FieldDescription}: {OrUnavailable(snapshot.Description)}",
            $"{Strings.FieldConnectionState}: {FormatConnectionState(snapshot.IsConnected)}",
            $"{Strings.FieldMacAddress}: {OrUnavailable(snapshot.MacAddress)}",
            $"{Strings.FieldIpv4Address}: {addresses}",
            $"{Strings.FieldGateway}: {FormatValues(snapshot.Ipv4Gateways)}",
            $"{Strings.FieldDnsServers}: {FormatValues(snapshot.Ipv4DnsServers)}",
            $"{Strings.FieldDnsSuffix}: {Strings.ValueUnavailable}",
            $"{Strings.FieldConfigurationMode}: {FormatConfigurationMode(snapshot.Mode)}");
    }

    /// <summary>Classifies and formats a gateway ping result for the inline status area.</summary>
    public static ApplyStatusMessage FormatGatewayPingResult(
        string gateway,
        GatewayPingResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gateway);
        ArgumentNullException.ThrowIfNull(result);

        string message = Strings.FormatPingResult(
            gateway,
            result.Sent,
            result.Received,
            result.PacketLossPercent,
            result.MinimumRoundtripTimeMilliseconds?.ToString(CultureInfo.CurrentCulture) ?? Strings.ValueUnavailable,
            result.AverageRoundtripTimeMilliseconds?.ToString(CultureInfo.CurrentCulture) ?? Strings.ValueUnavailable,
            result.MaximumRoundtripTimeMilliseconds?.ToString(CultureInfo.CurrentCulture) ?? Strings.ValueUnavailable,
            result.ErrorCode?.ToString(CultureInfo.CurrentCulture) ?? "0");

        ApplyStatusSeverity severity = !result.IsSuccessful
            ? ApplyStatusSeverity.Error
            : result.PacketLossPercent > 0 || result.AverageRoundtripTimeMilliseconds > 100
                ? ApplyStatusSeverity.Warning
                : ApplyStatusSeverity.Success;
        return new ApplyStatusMessage(message, severity);
    }

    private static string FormatValues(IReadOnlyList<string> values) =>
        values.Count == 0 ? Strings.ValueUnavailable : string.Join(", ", values);

    private static string FormatScaled(long bitsPerSecond, long unit, IFormatProvider formatProvider)
    {
        decimal scaled = decimal.Divide(bitsPerSecond, unit);
        decimal rounded = Math.Round(scaled, 1, MidpointRounding.AwayFromZero);

        return rounded == Math.Truncate(rounded)
            ? ((long)rounded).ToString(formatProvider)
            : rounded.ToString("0.#", formatProvider);
    }
}
