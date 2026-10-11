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
    [InlineData(AdapterMedium.Wired, "Kablolu bağlantı (Ethernet)", "Kablo takılı değil")]
    [InlineData(AdapterMedium.Wireless, "Kablosuz bağlantı (Ethernet)", "Bağlantı yok")]
    [InlineData(AdapterMedium.Bluetooth, "Bluetooth bağlantısı (Ethernet)", "Bağlantı yok")]
    [InlineData(AdapterMedium.Cellular, "Mobil bağlantı (Ethernet)", "Bağlantı yok")]
    public void Format_UsesPhysicalMediumForFriendlyNameAndDisconnectedStatus(
        AdapterMedium medium, string expectedName, string expectedStatus)
    {
        NetworkAdapterSnapshot snapshot = TestData.Snapshot(isConnected: false) with { Medium = medium };

        Assert.Equal(expectedName, AdapterDisplayFormatter.FormatFriendlyName(snapshot));
        Assert.Equal(expectedStatus, AdapterDisplayFormatter.FormatConnectionStatus(snapshot));
    }

    [Theory]
    [InlineData(AdapterMedium.Bluetooth, InternetConnectivity.Unknown)]
    [InlineData(AdapterMedium.Bluetooth, InternetConnectivity.None)]
    [InlineData(AdapterMedium.Bluetooth, InternetConnectivity.LocalNetwork)]
    [InlineData(AdapterMedium.Bluetooth, InternetConnectivity.Internet)]
    [InlineData(AdapterMedium.Cellular, InternetConnectivity.Unknown)]
    [InlineData(AdapterMedium.Cellular, InternetConnectivity.None)]
    [InlineData(AdapterMedium.Cellular, InternetConnectivity.LocalNetwork)]
    [InlineData(AdapterMedium.Cellular, InternetConnectivity.Internet)]
    public void FormatConnectionStatus_WhenBluetoothOrCellularIsDisconnected_UsesNoConnection(
        AdapterMedium medium, InternetConnectivity connectivity)
    {
        NetworkAdapterSnapshot snapshot = TestData.Snapshot(isConnected: false) with
        {
            Medium = medium,
            InternetConnectivity = connectivity
        };

        Assert.Equal("Bağlantı yok", AdapterDisplayFormatter.FormatConnectionStatus(snapshot));
    }

    [Theory]
    [InlineData("Ethernet", true, InternetConnectivity.Unknown, "Bağlı")]
    [InlineData("Ethernet", true, InternetConnectivity.None, "Yerel ağ — internet yok")]
    [InlineData("Ethernet", true, InternetConnectivity.LocalNetwork, "Yerel ağ — internet yok")]
    [InlineData("Ethernet", true, InternetConnectivity.Internet, "İnternet çalışıyor")]
    [InlineData("Ethernet", false, InternetConnectivity.Unknown, "Kablo takılı değil")]
    [InlineData("Ethernet", false, InternetConnectivity.None, "Kablo takılı değil")]
    [InlineData("Ethernet", false, InternetConnectivity.LocalNetwork, "Kablo takılı değil")]
    [InlineData("Ethernet", false, InternetConnectivity.Internet, "Kablo takılı değil")]
    [InlineData("Wi-Fi", true, InternetConnectivity.Unknown, "Bağlı")]
    [InlineData("Wi-Fi", true, InternetConnectivity.None, "Yerel ağ — internet yok")]
    [InlineData("Wi-Fi", true, InternetConnectivity.LocalNetwork, "Yerel ağ — internet yok")]
    [InlineData("Wi-Fi", true, InternetConnectivity.Internet, "İnternet çalışıyor")]
    [InlineData("Wi-Fi", false, InternetConnectivity.Unknown, "Bağlantı yok")]
    [InlineData("Wi-Fi", false, InternetConnectivity.None, "Bağlantı yok")]
    [InlineData("Wi-Fi", false, InternetConnectivity.LocalNetwork, "Bağlantı yok")]
    [InlineData("Wi-Fi", false, InternetConnectivity.Internet, "Bağlantı yok")]
    public void FormatConnectionStatus_UsesPlainTurkishForEveryState(
        string name, bool connected, InternetConnectivity connectivity, string expected)
    {
        NetworkAdapterSnapshot snapshot = TestData.Snapshot(name: name, isConnected: connected) with
        {
            InternetConnectivity = connectivity
        };

        Assert.Equal(expected, AdapterDisplayFormatter.FormatConnectionStatus(snapshot));
    }

    [Theory]
    [InlineData("Ethernet", "Contoso Adapter", "Kablolu bağlantı (Ethernet)")]
    [InlineData("Wi-Fi", "Contoso Wireless Adapter", "Kablosuz bağlantı (Wi-Fi)")]
    [InlineData("PAN", "Bluetooth Device (Personal Area Network)", "Bluetooth bağlantısı (PAN)")]
    [InlineData("Mobile", "Mobile Broadband Adapter", "Mobil bağlantı (Mobile)")]
    [InlineData("", "Bluetooth PAN", "Bluetooth bağlantısı (Bluetooth PAN)")]
    [InlineData("", "Cellular Adapter", "Mobil bağlantı (Cellular Adapter)")]
    [InlineData("vEthernet", "Hyper-V Virtual Ethernet Adapter", "Sanal bağlantı (vEthernet)")]
    [InlineData("Virtual Wi-Fi", "Virtual Wireless Adapter", "Sanal bağlantı (Virtual Wi-Fi)")]
    [InlineData("", "Contoso Adapter", "Kablolu bağlantı (Contoso Adapter)")]
    [InlineData("   ", "Contoso Wireless Adapter", "Kablosuz bağlantı (Contoso Wireless Adapter)")]
    public void FormatFriendlyName_UsesCategoryAndFallsBackToDescription(string name, string description, string expected)
    {
        Assert.Equal(expected, AdapterDisplayFormatter.FormatFriendlyName(TestData.Snapshot(name: name, description: description)));
    }

    [Fact]
    public void FormatFriendlyName_WhenNameAndDescriptionAreMissing_UsesPlaceholder()
    {
        Assert.Equal($"Kablolu bağlantı ({Strings.ValueUnavailable})",
            AdapterDisplayFormatter.FormatFriendlyName(TestData.Snapshot(name: "", description: "")));
    }

    [Fact]
    public void FormatCopySummary_KeepsWindowsNameAndIncludesConnectivityStatus()
    {
        NetworkAdapterSnapshot snapshot = TestData.Snapshot() with { InternetConnectivity = InternetConnectivity.Internet };

        string summary = AdapterDisplayFormatter.FormatCopySummary(snapshot);

        Assert.Contains("Bağlantı adı: Ethernet", summary, StringComparison.Ordinal);
        Assert.Contains($"{Strings.FieldConnectionState}: İnternet çalışıyor", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("Kablolu bağlantı", summary, StringComparison.Ordinal);
    }

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
        Assert.Equal(Strings.ConnectionStateConnected, AdapterDisplayFormatter.FormatConnectionState(true));
        Assert.Equal(Strings.ConnectionStateDisconnected, AdapterDisplayFormatter.FormatConnectionState(false));
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

        Assert.Contains($"{Strings.FieldAdapterName}: Ethernet", text, StringComparison.Ordinal);
        Assert.Contains("192.168.1.50 (255.255.255.0)", text, StringComparison.Ordinal);
        Assert.Contains($"{Strings.FieldDnsServers}: 192.168.1.1", text, StringComparison.Ordinal);
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
