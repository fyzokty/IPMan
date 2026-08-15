using IPMan.Infrastructure.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class WindowsDnsClientServerAddressReaderTests
{
    private static readonly string[] OrderedIpv4Servers = { "8.8.8.8", "1.1.1.1" };
    private static readonly string[] OrderedIpv6Servers =
        { "2001:4860:4860::8888", "2606:4700:4700::1111" };
    private static readonly string[] SingleIpv4Server = { "1.1.1.1" };

    private static readonly WindowsInterfaceIdentity Identity =
        new(new Guid("11111111-2222-3333-4444-555555555555"), 1234, 9);

    [Fact]
    public void Read_WithExactIpv4AndIpv6Rows_SeparatesFamiliesAndPreservesOrder()
    {
        FakeProvider provider = new(Success(
            Row(9u, "Ethernet", 2u, OrderedIpv4Servers),
            Row(9u, "Ethernet", 23u, OrderedIpv6Servers)));
        WindowsDnsClientServerAddressReader reader = new(provider);

        DnsClientServerAddressReadResult result = reader.Read(Identity);

        Assert.True(result.Ipv4.ReadSuccess);
        Assert.True(result.Ipv6.ReadSuccess);
        Assert.Equal(["8.8.8.8", "1.1.1.1"], result.Ipv4.Servers);
        Assert.Equal(["2001:4860:4860::8888", "2606:4700:4700::1111"], result.Ipv6.Servers);
        Assert.Equal((uint)9, provider.InterfaceIndex);
    }

    [Fact]
    public void Read_WithEmptyFamilyLists_ReturnsLegitimateSuccess()
    {
        WindowsDnsClientServerAddressReader reader = new(new FakeProvider(Success(
            Row(9u, "Ethernet", 2u, Array.Empty<string>()),
            Row(9u, "Ethernet", 23u, null))));

        DnsClientServerAddressReadResult result = reader.Read(Identity);

        Assert.True(result.Ipv4.ReadSuccess);
        Assert.True(result.Ipv6.ReadSuccess);
        Assert.Empty(result.Ipv4.Servers);
        Assert.Empty(result.Ipv6.Servers);
    }

    [Fact]
    public void Read_WhenProviderFails_PropagatesTypedFailureToBothFamilies()
    {
        WindowsDnsClientServerAddressReader reader = new(new FakeProvider(
            DnsClientServerAddressProviderReadResult.Failure(
                DnsTruthSourceReadStatus.ProviderReadFailed,
                5)));

        DnsClientServerAddressReadResult result = reader.Read(Identity);

        Assert.Equal(DnsTruthSourceReadStatus.ProviderReadFailed, result.Ipv4.Status);
        Assert.Equal(DnsTruthSourceReadStatus.ProviderReadFailed, result.Ipv6.Status);
        Assert.Equal((uint)5, result.Ipv4.TechnicalCode);
    }

    [Fact]
    public void Read_WhenReturnedIndexIsForeign_FailsClosed()
    {
        WindowsDnsClientServerAddressReader reader = new(new FakeProvider(Success(
            Row(10u, "Foreign", 2u, SingleIpv4Server))));

        DnsClientServerAddressReadResult result = reader.Read(Identity);

        Assert.Equal(DnsTruthSourceReadStatus.IdentityMismatch, result.Ipv4.Status);
        Assert.Equal(DnsTruthSourceReadStatus.IdentityMismatch, result.Ipv6.Status);
    }

    [Theory]
    [InlineData("invalid-family", null)]
    [InlineData(99u, null)]
    [InlineData(2u, "invalid-address")]
    [InlineData(2u, "2001:db8::1")]
    public void Read_WhenFamilyOrAddressIsMalformed_FailsClosed(
        object family,
        string? address)
    {
        object? servers = address is null ? Array.Empty<string>() : new[] { address };
        WindowsDnsClientServerAddressReader reader = new(new FakeProvider(Success(
            Row(9u, "Ethernet", family, servers))));

        DnsClientServerAddressReadResult result = reader.Read(Identity);

        Assert.False(result.Ipv4.ReadSuccess);
        Assert.False(result.Ipv6.ReadSuccess);
        Assert.Contains(
            result.Ipv4.Status,
            new[]
            {
                DnsTruthSourceReadStatus.InvalidData,
                DnsTruthSourceReadStatus.UnsupportedAddressFamily
            });
    }

    [Fact]
    public void BuildQuery_FiltersOnlyExactInterfaceIndex()
    {
        string query = SystemWindowsDnsClientServerAddressProvider.BuildQuery(9);

        Assert.Contains("MSFT_DNSClientServerAddress", query, StringComparison.Ordinal);
        Assert.Contains("WHERE InterfaceIndex = 9", query, StringComparison.Ordinal);
        Assert.DoesNotContain("InterfaceAlias", query[(query.IndexOf("WHERE", StringComparison.Ordinal))..], StringComparison.Ordinal);
    }

    private static DnsClientServerAddressProviderReadResult Success(
        params DnsClientServerAddressRawRecord[] records) =>
        DnsClientServerAddressProviderReadResult.Success(records);

    private static DnsClientServerAddressRawRecord Row(
        object? index,
        object? alias,
        object? family,
        object? servers) =>
        new(index, alias, family, servers);

    private sealed class FakeProvider : IWindowsDnsClientServerAddressProvider
    {
        private readonly DnsClientServerAddressProviderReadResult _result;

        public FakeProvider(DnsClientServerAddressProviderReadResult result) => _result = result;

        public uint? InterfaceIndex { get; private set; }

        public DnsClientServerAddressProviderReadResult Enumerate(uint interfaceIndex)
        {
            InterfaceIndex = interfaceIndex;
            return _result;
        }
    }
}
