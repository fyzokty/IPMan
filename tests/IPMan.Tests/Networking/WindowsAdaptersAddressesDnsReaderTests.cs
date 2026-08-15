using System.Runtime.InteropServices;
using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class WindowsAdaptersAddressesDnsReaderTests
{
    private static readonly Guid InterfaceGuid =
        new("11111111-2222-3333-4444-555555555555");
    private static readonly WindowsInterfaceIdentity Identity =
        new(InterfaceGuid, 1234, 9);

    [Fact]
    public void Read_WithExactTargetAndForeignAdapter_UsesOnlyFullExactIdentity()
    {
        WindowsAdapterDnsRecord foreign = new(
            new Guid("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE"),
            9876,
            10,
            new[] { Address(2, "9.9.9.9") });
        WindowsAdapterDnsRecord target = new(
            InterfaceGuid,
            1234,
            9,
            new[]
            {
                Address(23, "2001:4860:4860::8888"),
                Address(2, "8.8.8.8"),
                Address(2, "1.1.1.1"),
                Address(23, "2606:4700:4700::1111")
            });
        WindowsAdaptersAddressesDnsReader reader = new(new FakeProvider(Success(foreign, target)));

        AdaptersAddressesDnsReadResult result = reader.Read(Identity);

        Assert.True(result.Ipv4.ReadSuccess);
        Assert.True(result.Ipv6.ReadSuccess);
        Assert.True(result.Ipv4.ExactIdentityMatches);
        Assert.Equal(["8.8.8.8", "1.1.1.1"], result.Ipv4.Servers);
        Assert.Equal(["2001:4860:4860::8888", "2606:4700:4700::1111"], result.Ipv6.Servers);
    }

    [Fact]
    public void Read_WithExactTargetAndNoDnsRows_ReturnsEmptySuccess()
    {
        WindowsAdaptersAddressesDnsReader reader = new(new FakeProvider(Success(
            new WindowsAdapterDnsRecord(InterfaceGuid, 1234, 9, Array.Empty<WindowsNativeDnsAddress>()))));

        AdaptersAddressesDnsReadResult result = reader.Read(Identity);

        Assert.True(result.Ipv4.ReadSuccess);
        Assert.True(result.Ipv6.ReadSuccess);
        Assert.Empty(result.Ipv4.Servers);
        Assert.Empty(result.Ipv6.Servers);
    }

    [Fact]
    public void Read_WhenNativeProviderFails_PropagatesFailureAndCode()
    {
        WindowsAdaptersAddressesDnsReader reader = new(new FakeProvider(
            WindowsAdaptersAddressesProviderReadResult.Failure(
                DnsTruthSourceReadStatus.NativeCallFailed,
                87)));

        AdaptersAddressesDnsReadResult result = reader.Read(Identity);

        Assert.Equal(DnsTruthSourceReadStatus.NativeCallFailed, result.Ipv4.Status);
        Assert.Equal((uint)87, result.Ipv4.NativeResult);
        Assert.False(result.Ipv4.ExactIdentityMatches);
    }

    [Fact]
    public void Read_WhenTargetAdapterIsMissing_DoesNotAcceptForeignAdapter()
    {
        WindowsAdaptersAddressesDnsReader reader = new(new FakeProvider(Success(
            new WindowsAdapterDnsRecord(
                new Guid("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE"),
                9876,
                10,
                new[] { Address(2, "1.1.1.1") }))));

        AdaptersAddressesDnsReadResult result = reader.Read(Identity);

        Assert.Equal(DnsTruthSourceReadStatus.TargetMissing, result.Ipv4.Status);
        Assert.False(result.Ipv4.ExactIdentityMatches);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void Read_WhenAnyIdentityDimensionMatchesButFullIdentityDoesNot_FailsClosed(
        bool guidMatches,
        bool luidMatches,
        bool indexMatches)
    {
        WindowsAdapterDnsRecord drifted = new(
            guidMatches ? InterfaceGuid : Guid.NewGuid(),
            luidMatches ? 1234UL : 9876UL,
            indexMatches ? 9u : 10u,
            Array.Empty<WindowsNativeDnsAddress>());
        WindowsAdaptersAddressesDnsReader reader = new(new FakeProvider(Success(drifted)));

        AdaptersAddressesDnsReadResult result = reader.Read(Identity);

        Assert.Equal(DnsTruthSourceReadStatus.IdentityMismatch, result.Ipv4.Status);
        Assert.False(result.Ipv4.ExactIdentityMatches);
    }

    [Fact]
    public void Read_WhenTargetContainsMalformedSockaddr_DoesNotReportEmptySuccess()
    {
        WindowsAdapterDnsRecord target = new(
            InterfaceGuid,
            1234,
            9,
            new[]
            {
                new WindowsNativeDnsAddress(
                    DnsTruthSourceReadStatus.InvalidData,
                    AddressFamily: null,
                    Address: null)
            });
        WindowsAdaptersAddressesDnsReader reader = new(new FakeProvider(Success(target)));

        AdaptersAddressesDnsReadResult result = reader.Read(Identity);

        Assert.Equal(DnsTruthSourceReadStatus.InvalidData, result.Ipv4.Status);
        Assert.False(result.Ipv4.ReadSuccess);
        Assert.False(result.Ipv6.ReadSuccess);
    }

    [Fact]
    public void ParseSockaddr_ParsesCanonicalIpv4AndIpv6()
    {
        byte[] ipv4 = new byte[16];
        BitConverter.GetBytes((ushort)2).CopyTo(ipv4, 0);
        new byte[] { 1, 1, 1, 1 }.CopyTo(ipv4, 4);
        byte[] ipv6 = new byte[28];
        BitConverter.GetBytes((ushort)23).CopyTo(ipv6, 0);
        new byte[] { 0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }
            .CopyTo(ipv6, 8);

        WindowsNativeDnsAddress parsedIpv4 =
            SystemWindowsAdaptersAddressesProvider.ParseSockaddr(ipv4);
        WindowsNativeDnsAddress parsedIpv6 =
            SystemWindowsAdaptersAddressesProvider.ParseSockaddr(ipv6);

        Assert.Equal("1.1.1.1", parsedIpv4.Address);
        Assert.Equal("2001:db8::1", parsedIpv6.Address);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    public void ParseSockaddr_WithMalformedLength_FailsClosed(int length)
    {
        byte[] sockaddr = new byte[length];

        if (length >= sizeof(ushort))
        {
            BitConverter.GetBytes((ushort)2).CopyTo(sockaddr, 0);
        }

        WindowsNativeDnsAddress result =
            SystemWindowsAdaptersAddressesProvider.ParseSockaddr(sockaddr);

        Assert.Equal(DnsTruthSourceReadStatus.InvalidData, result.Status);
        Assert.Null(result.Address);
    }

    [Fact]
    public void Enumerate_WhenInitialNativeReadFails_PropagatesNativeCode()
    {
        SystemWindowsAdaptersAddressesProvider provider = new(new FailingNativeApi(5));

        WindowsAdaptersAddressesProviderReadResult result = provider.Enumerate();

        Assert.Equal(DnsTruthSourceReadStatus.NativeCallFailed, result.Status);
        Assert.Equal((uint)5, result.NativeResult);
    }

    [Fact]
    public void X64InteropLayout_MatchesRequiredDnsAndIdentityOffsets()
    {
        Assert.True(SystemWindowsAdaptersAddressesProvider.IsSupportedArchitecture);
        Assert.Equal(232, Marshal.SizeOf<SystemWindowsAdaptersAddressesProvider.IpAdapterAddressesHeader>());
        Assert.Equal(4, OffsetOf<SystemWindowsAdaptersAddressesProvider.IpAdapterAddressesHeader>("InterfaceIndex"));
        Assert.Equal(8, OffsetOf<SystemWindowsAdaptersAddressesProvider.IpAdapterAddressesHeader>("Next"));
        Assert.Equal(16, OffsetOf<SystemWindowsAdaptersAddressesProvider.IpAdapterAddressesHeader>("AdapterName"));
        Assert.Equal(48, OffsetOf<SystemWindowsAdaptersAddressesProvider.IpAdapterAddressesHeader>("FirstDnsServerAddress"));
        Assert.Equal(224, OffsetOf<SystemWindowsAdaptersAddressesProvider.IpAdapterAddressesHeader>("Luid"));
        Assert.Equal(32, Marshal.SizeOf<SystemWindowsAdaptersAddressesProvider.IpAdapterDnsServerAddress>());
        Assert.Equal(16, OffsetOf<SystemWindowsAdaptersAddressesProvider.IpAdapterDnsServerAddress>("Sockaddr"));
        Assert.Equal(24, OffsetOf<SystemWindowsAdaptersAddressesProvider.IpAdapterDnsServerAddress>("SockaddrLength"));
    }

    private static WindowsNativeDnsAddress Address(ushort family, string value) =>
        new(DnsTruthSourceReadStatus.Success, family, value);

    private static WindowsAdaptersAddressesProviderReadResult Success(
        params WindowsAdapterDnsRecord[] adapters) =>
        WindowsAdaptersAddressesProviderReadResult.Success(adapters);

    private static int OffsetOf<T>(string fieldName) =>
        Marshal.OffsetOf<T>(fieldName).ToInt32();

    private sealed class FakeProvider : IWindowsAdaptersAddressesProvider
    {
        private readonly WindowsAdaptersAddressesProviderReadResult _result;

        public FakeProvider(WindowsAdaptersAddressesProviderReadResult result) => _result = result;

        public WindowsAdaptersAddressesProviderReadResult Enumerate() => _result;
    }

    private sealed class FailingNativeApi : IGetAdaptersAddressesNativeApi
    {
        private readonly uint _result;

        public FailingNativeApi(uint result) => _result = result;

        public uint Get(uint family, uint flags, IntPtr addresses, ref uint bufferSize) => _result;
    }
}
