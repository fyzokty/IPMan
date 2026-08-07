using System.Net.NetworkInformation;
using IPMan.Domain.Networking;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class NetworkAdapterMapperTests
{
    private static readonly string[] UnspecifiedGateway = { "0.0.0.0" };

    private static readonly string[] Ipv6ThenIpv4Gateways = { "fe80::1", "192.168.9.254" };

    private static readonly string[] ThreeDnsServers = { "1.1.1.1", "9.9.9.9", "8.8.4.4" };

    private static readonly string[] Ipv6ThenIpv4DnsServers = { "2606:4700:4700::1111", "1.1.1.1" };

    private static readonly string[] SingleDnsServer = { "1.1.1.1" };

    private static readonly string[] SingleMappedGateway = { "192.168.9.254" };

    private static readonly byte[] SamplePhysicalAddress = { 0x0A, 0x1B, 0x2C, 0x3D, 0x4E, 0x5F };

    [Fact]
    public void Map_WhenAdapterIsUp_ReportsConnected()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(operationalStatus: OperationalStatus.Up));

        Assert.True(snapshot.IsConnected);
    }

    [Theory]
    [InlineData(OperationalStatus.Down)]
    [InlineData(OperationalStatus.NotPresent)]
    [InlineData(OperationalStatus.LowerLayerDown)]
    [InlineData(OperationalStatus.Unknown)]
    public void Map_WhenAdapterIsNotUp_ReportsDisconnected(OperationalStatus status)
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(operationalStatus: status));

        Assert.False(snapshot.IsConnected);
    }

    [Fact]
    public void Map_UsesWindowsIdentityRatherThanDisplayName()
    {
        const string WindowsId = "{ABCDEF01-2345-6789-ABCD-EF0123456789}";

        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(id: WindowsId, name: "Renamed by user"));

        Assert.Equal(WindowsId, snapshot.Id.Value);
        Assert.NotEqual(snapshot.Name, snapshot.Id.Value);
    }

    [Fact]
    public void Map_WhenAdapterHasNoIdentity_Throws()
    {
        Assert.Throws<ArgumentException>(() => NetworkAdapterMapper.Map(TestData.Adapter(id: " ")));
    }

    [Fact]
    public void Map_WhenSingleIpv4Exists_SelectsItWithItsMask()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: new[] { TestData.Ipv4("10.0.0.7", "255.255.0.0") }));

        Assert.Equal("10.0.0.7", snapshot.Ipv4Address);
        Assert.Equal("255.255.0.0", snapshot.SubnetMask);
    }

    [Fact]
    public void Map_WhenMultipleIpv4AddressesExist_SelectsFirstRoutableAddress()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: new[]
            {
                TestData.Ipv4("169.254.10.11", "255.255.0.0"),
                TestData.Ipv4("192.168.5.5", "255.255.255.0"),
                TestData.Ipv4("192.168.5.6", "255.255.255.0")
            }));

        Assert.Equal("192.168.5.5", snapshot.Ipv4Address);
        Assert.Equal("255.255.255.0", snapshot.SubnetMask);
    }

    [Fact]
    public void Map_WhenMultipleIpv4AddressesExist_PreservesAllOfThemInWindowsOrder()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: new[]
            {
                TestData.Ipv4("169.254.10.11", "255.255.0.0"),
                TestData.Ipv4("192.168.5.5", "255.255.255.0"),
                TestData.Ipv4("192.168.5.6", "255.255.255.128")
            }));

        Assert.Collection(
            snapshot.Ipv4Addresses,
            address => Assert.Equal(new Ipv4AddressAssignment("169.254.10.11", "255.255.0.0"), address),
            address => Assert.Equal(new Ipv4AddressAssignment("192.168.5.5", "255.255.255.0"), address),
            address => Assert.Equal(new Ipv4AddressAssignment("192.168.5.6", "255.255.255.128"), address));
    }

    [Fact]
    public void Map_WhenMultipleIpv4AddressesExist_ReportsTheNonPrimaryOnesAsAdditional()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: new[]
            {
                TestData.Ipv4("192.168.5.5", "255.255.255.0"),
                TestData.Ipv4("192.168.5.6", "255.255.255.128")
            }));

        Assert.Equal("192.168.5.5", snapshot.Ipv4Address);
        Assert.True(snapshot.HasAdditionalIpv4Addresses);
        Assert.Equal(
            "192.168.5.6",
            Assert.Single(snapshot.AdditionalIpv4Addresses).Address);
    }

    [Fact]
    public void Map_WhenSingleIpv4Exists_ReportsNoAdditionalAddresses()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: new[] { TestData.Ipv4("10.0.0.7", "255.255.0.0") }));

        Assert.Single(snapshot.Ipv4Addresses);
        Assert.False(snapshot.HasAdditionalIpv4Addresses);
        Assert.Empty(snapshot.AdditionalIpv4Addresses);
    }

    [Fact]
    public void Map_WhenAddressListContainsIpv6_KeepsItOutOfTheIpv4Addresses()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: new[]
            {
                TestData.Ipv6("fe80::1"),
                TestData.Ipv4("192.168.5.5", "255.255.255.0")
            }));

        Assert.Equal("192.168.5.5", Assert.Single(snapshot.Ipv4Addresses).Address);
    }

    [Fact]
    public void Map_WhenNoIpv4AddressExists_ReturnsEmptyAddressCollection()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: Array.Empty<AdapterUnicastAddressReadModel>()));

        Assert.Empty(snapshot.Ipv4Addresses);
        Assert.False(snapshot.HasAdditionalIpv4Addresses);
    }

    [Fact]
    public void Map_WhenAnAdditionalAddressHasNoMask_KeepsTheAddressWithoutMask()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: new[]
            {
                TestData.Ipv4("192.168.5.5", "255.255.255.0"),
                TestData.Ipv4("192.168.5.6", "0.0.0.0")
            }));

        Assert.Equal(
            new Ipv4AddressAssignment("192.168.5.6", null),
            Assert.Single(snapshot.AdditionalIpv4Addresses));
    }

    [Fact]
    public void Map_WhenOnlyApipaAddressExists_ReportsApipaAddressTruthfully()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: new[] { TestData.Ipv4("169.254.10.11", "255.255.0.0") }));

        Assert.Equal("169.254.10.11", snapshot.Ipv4Address);
    }

    [Fact]
    public void Map_WhenOnlyIpv6AddressesExist_ReportsNoIpv4()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: new[] { TestData.Ipv6("fe80::1") }));

        Assert.Null(snapshot.Ipv4Address);
        Assert.Null(snapshot.SubnetMask);
    }

    [Fact]
    public void Map_WhenAddressIsMissing_ReturnsSnapshotWithoutAddress()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: Array.Empty<AdapterUnicastAddressReadModel>()));

        Assert.Null(snapshot.Ipv4Address);
        Assert.Null(snapshot.SubnetMask);
    }

    [Fact]
    public void Map_WhenMaskIsUnspecified_ReturnsSnapshotWithoutMask()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(unicastAddresses: new[] { TestData.Ipv4("192.168.1.50", "0.0.0.0") }));

        Assert.Equal("192.168.1.50", snapshot.Ipv4Address);
        Assert.Null(snapshot.SubnetMask);
    }

    [Fact]
    public void Map_WhenGatewayIsMissing_ReturnsSnapshotWithoutGateway()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(gatewayAddresses: Array.Empty<string>()));

        Assert.Null(snapshot.Gateway);
    }

    [Fact]
    public void Map_WhenGatewayIsUnspecified_ReturnsSnapshotWithoutGateway()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(gatewayAddresses: UnspecifiedGateway));

        Assert.Null(snapshot.Gateway);
    }

    [Fact]
    public void Map_WhenGatewayListStartsWithIpv6_SelectsFirstIpv4Gateway()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(gatewayAddresses: Ipv6ThenIpv4Gateways));

        Assert.Equal("192.168.9.254", snapshot.Gateway);
        Assert.Equal(SingleMappedGateway, snapshot.Ipv4Gateways);
    }

    [Fact]
    public void Map_WhenMultipleIpv4GatewaysExist_PreservesAllInWindowsOrder()
    {
        string[] gateways = { "192.168.1.1", "10.0.0.1" };

        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(gatewayAddresses: gateways));

        Assert.Equal(gateways, snapshot.Ipv4Gateways);
        Assert.Equal("192.168.1.1", snapshot.Gateway);
    }

    [Fact]
    public void Map_WhenDnsServersExist_PreservesWindowsOrder()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(dnsAddresses: ThreeDnsServers));

        Assert.Equal("1.1.1.1", snapshot.PrimaryDns);
        Assert.Equal("9.9.9.9", snapshot.SecondaryDns);
        Assert.Equal(ThreeDnsServers, snapshot.Ipv4DnsServers);
    }

    [Fact]
    public void Map_WhenDnsListContainsIpv6_UsesIpv4ServersOnly()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(dnsAddresses: Ipv6ThenIpv4DnsServers));

        Assert.Equal("1.1.1.1", snapshot.PrimaryDns);
        Assert.Null(snapshot.SecondaryDns);
        Assert.Equal(SingleDnsServer, snapshot.Ipv4DnsServers);
    }

    [Fact]
    public void Map_WhenDnsIsMissing_ReturnsSnapshotWithoutDns()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(dnsAddresses: Array.Empty<string>()));

        Assert.Null(snapshot.PrimaryDns);
        Assert.Null(snapshot.SecondaryDns);
        Assert.Empty(snapshot.Ipv4DnsServers);
    }

    [Fact]
    public void Map_WhenOnlyOneDnsExists_LeavesSecondaryEmpty()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(dnsAddresses: SingleDnsServer));

        Assert.Equal("1.1.1.1", snapshot.PrimaryDns);
        Assert.Null(snapshot.SecondaryDns);
        Assert.Equal(SingleDnsServer, snapshot.Ipv4DnsServers);
    }

    [Fact]
    public void Map_WhenPhysicalAddressExists_FormatsMacAsHyphenatedUpperCase()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(physicalAddress: SamplePhysicalAddress));

        Assert.Equal("0A-1B-2C-3D-4E-5F", snapshot.MacAddress);
    }

    [Fact]
    public void Map_WhenPhysicalAddressIsMissing_ReturnsEmptyMac()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(physicalAddress: Array.Empty<byte>()));

        Assert.Equal(string.Empty, snapshot.MacAddress);
    }

    [Theory]
    [InlineData(true, NetworkConfigurationMode.Dhcp)]
    [InlineData(false, NetworkConfigurationMode.Static)]
    public void Map_WhenDhcpStateIsKnown_MapsConfigurationMode(
        bool isDhcpEnabled,
        NetworkConfigurationMode expected)
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(isDhcpEnabled: isDhcpEnabled));

        Assert.Equal(expected, snapshot.Mode);
    }

    [Fact]
    public void Map_WhenDhcpStateIsUnavailable_MapsUnknownConfigurationMode()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(isDhcpEnabled: null));

        Assert.Equal(NetworkConfigurationMode.Unknown, snapshot.Mode);
    }

    [Fact]
    public void Map_WhenLinkSpeedIsReported_KeepsBitsPerSecond()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(speedBitsPerSecond: 100_000_000));

        Assert.Equal(100_000_000, snapshot.LinkSpeedBitsPerSecond);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(0L)]
    public void Map_WhenLinkSpeedIsUnavailable_ReturnsSnapshotWithoutLinkSpeed(long reportedSpeed)
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(speedBitsPerSecond: reportedSpeed));

        Assert.Null(snapshot.LinkSpeedBitsPerSecond);
    }

    [Fact]
    public void Map_KeepsNameAndDescriptionAsMetadata()
    {
        NetworkAdapterSnapshot snapshot = NetworkAdapterMapper.Map(
            TestData.Adapter(name: "Wi-Fi", description: "Contoso Wireless"));

        Assert.Equal("Wi-Fi", snapshot.Name);
        Assert.Equal("Contoso Wireless", snapshot.Description);
    }
}
