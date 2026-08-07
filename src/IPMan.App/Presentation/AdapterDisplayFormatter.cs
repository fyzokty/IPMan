using System.Globalization;
using IPMan.App.Resources;
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

    private static string FormatScaled(long bitsPerSecond, long unit, IFormatProvider formatProvider)
    {
        decimal scaled = decimal.Divide(bitsPerSecond, unit);
        decimal rounded = Math.Round(scaled, 1, MidpointRounding.AwayFromZero);

        return rounded == Math.Truncate(rounded)
            ? ((long)rounded).ToString(formatProvider)
            : rounded.ToString("0.#", formatProvider);
    }
}
