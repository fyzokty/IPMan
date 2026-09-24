using System.Globalization;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed class AdapterDisplayFormatterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OrUnavailable_ForMissingValue_ReturnsPlaceholder(string? value)
    {
        Assert.Equal(Strings.ValueUnavailable, AdapterDisplayFormatter.OrUnavailable(value));
    }

    [Fact]
    public void OrUnavailable_ForPresentValue_ReturnsValueUnchanged()
    {
        Assert.Equal("192.168.1.50", AdapterDisplayFormatter.OrUnavailable("192.168.1.50"));
    }

    [Fact]
    public void OrUnavailable_NeverFabricatesAnAddress()
    {
        // A missing gateway must not become 0.0.0.0 just to fill the UI.
        Assert.NotEqual("0.0.0.0", AdapterDisplayFormatter.OrUnavailable(null));
    }

    [Fact]
    public void FormatConnectionState_UsesTurkishText_NotColourAlone()
    {
        Assert.Equal("Bağlı", AdapterDisplayFormatter.FormatConnectionState(true));
        Assert.Equal("Bağlı Değil", AdapterDisplayFormatter.FormatConnectionState(false));
    }

    [Theory]
    [InlineData(NetworkConfigurationMode.Dhcp)]
    [InlineData(NetworkConfigurationMode.Static)]
    [InlineData(NetworkConfigurationMode.Unknown)]
    public void FormatConfigurationMode_ReturnsLocalizedTextForEveryMode(NetworkConfigurationMode mode)
    {
        string text = AdapterDisplayFormatter.FormatConfigurationMode(mode);

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.NotEqual(mode.ToString(), text);
    }

    [Fact]
    public void FormatConfigurationMode_DistinguishesTheThreeModes()
    {
        string dhcp = AdapterDisplayFormatter.FormatConfigurationMode(NetworkConfigurationMode.Dhcp);
        string manual = AdapterDisplayFormatter.FormatConfigurationMode(NetworkConfigurationMode.Static);
        string unknown = AdapterDisplayFormatter.FormatConfigurationMode(NetworkConfigurationMode.Unknown);

        Assert.Equal(3, new HashSet<string>(StringComparer.Ordinal) { dhcp, manual, unknown }.Count);
    }

    [Theory]
    [InlineData(1_000_000_000L, "1 Gbps")]
    [InlineData(2_500_000_000L, "2.5 Gbps")]
    [InlineData(10_000_000_000L, "10 Gbps")]
    [InlineData(100_000_000L, "100 Mbps")]
    [InlineData(54_000_000L, "54 Mbps")]
    [InlineData(121_500_000L, "121.5 Mbps")]
    [InlineData(56_000L, "56 Kbps")]
    public void FormatLinkSpeed_ForReportedSpeed_ReturnsHumanReadableValue(
        long bitsPerSecond,
        string expected)
    {
        Assert.Equal(expected, AdapterDisplayFormatter.FormatLinkSpeed(bitsPerSecond, Culture));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void FormatLinkSpeed_WhenUnavailable_ReturnsPlaceholder(long? bitsPerSecond)
    {
        Assert.Equal(
            Strings.ValueUnavailable,
            AdapterDisplayFormatter.FormatLinkSpeed(bitsPerSecond, Culture));
    }

    [Fact]
    public void FormatAdditionalIpv4Addresses_WhenNoneExist_ReturnsPlaceholder()
    {
        Assert.Equal(
            Strings.ValueUnavailable,
            AdapterDisplayFormatter.FormatAdditionalIpv4Addresses(Array.Empty<Ipv4AddressAssignment>()));
    }

    [Fact]
    public void FormatAdditionalIpv4Addresses_ListsEveryAdditionalAddress()
    {
        string text = AdapterDisplayFormatter.FormatAdditionalIpv4Addresses(new[]
        {
            new Ipv4AddressAssignment("192.168.1.60", "255.255.255.0"),
            new Ipv4AddressAssignment("192.168.1.61", null)
        });

        Assert.Equal("192.168.1.60, 192.168.1.61", text);
    }

    [Fact]
    public void FormatRefreshTime_WhenNeverRefreshed_ReturnsNeverText()
    {
        Assert.Equal(
            Strings.StatusLastRefreshNever,
            AdapterDisplayFormatter.FormatRefreshTime(null, Culture));
    }

    [Fact]
    public void FormatRefreshTime_RendersTheTimestampInLocalTime()
    {
        DateTimeOffset refreshedAtUtc = new(2026, 8, 7, 21, 5, 9, TimeSpan.Zero);

        string expectedTime = refreshedAtUtc.ToLocalTime().ToString("HH:mm:ss", Culture);

        Assert.Contains(
            expectedTime,
            AdapterDisplayFormatter.FormatRefreshTime(refreshedAtUtc, Culture),
            StringComparison.Ordinal);
    }

    [Fact]
    public void FormatCopySummary_ListsIpv4FieldsAndExcludesIpv6()
    {
        NetworkAdapterSnapshot snapshot = TestData.Snapshot();

        string text = AdapterDisplayFormatter.FormatCopySummary(snapshot);

        Assert.Contains("Bağdaştırıcı adı: Ethernet", text, StringComparison.Ordinal);
        Assert.Contains("192.168.1.50 (255.255.255.0)", text, StringComparison.Ordinal);
        Assert.Contains("DNS sunucuları: 192.168.1.1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IPv6", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(4, 4, 25, ApplyStatusSeverity.Success)]
    [InlineData(4, 3, 25, ApplyStatusSeverity.Warning)]
    [InlineData(4, 4, 101, ApplyStatusSeverity.Warning)]
    [InlineData(4, 0, 0, ApplyStatusSeverity.Error)]
    public void FormatGatewayPingResult_ClassifiesLossAndLatency(
        int sent,
        int received,
        long average,
        ApplyStatusSeverity expectedSeverity)
    {
        GatewayPingResult result = new(sent, received, average, average, average, 0);

        ApplyStatusMessage message = AdapterDisplayFormatter.FormatGatewayPingResult("192.168.1.1", result);

        Assert.Equal(expectedSeverity, message.Severity);
        Assert.Contains("Hata kodu: 0", message.Text, StringComparison.Ordinal);
    }
}
