using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class Ipv4AddressCollectionTests
{
    [Fact]
    public void Empty_HasNoAddresses()
    {
        Assert.Empty(Ipv4AddressCollection.Empty);
    }

    [Fact]
    public void Collection_PreservesTheOrderItWasGiven()
    {
        Ipv4AddressCollection addresses = Create("10.0.0.2", "10.0.0.1");

        Assert.Equal("10.0.0.2", addresses[0].Address);
        Assert.Equal("10.0.0.1", addresses[1].Address);
    }

    [Fact]
    public void Equals_ForSameAddressesInSameOrder_ReturnsTrue()
    {
        Assert.Equal(Create("10.0.0.1", "10.0.0.2"), Create("10.0.0.1", "10.0.0.2"));
        Assert.True(Create("10.0.0.1") == Create("10.0.0.1"));
    }

    [Fact]
    public void Equals_ForDifferentOrder_ReturnsFalse()
    {
        Assert.NotEqual(Create("10.0.0.1", "10.0.0.2"), Create("10.0.0.2", "10.0.0.1"));
    }

    [Fact]
    public void Equals_WhenAnAddressIsAdded_ReturnsFalse()
    {
        Assert.NotEqual(Create("10.0.0.1"), Create("10.0.0.1", "10.0.0.2"));
        Assert.True(Create("10.0.0.1") != Create("10.0.0.1", "10.0.0.2"));
    }

    [Fact]
    public void GetHashCode_ForEqualCollections_Matches()
    {
        Assert.Equal(
            Create("10.0.0.1", "10.0.0.2").GetHashCode(),
            Create("10.0.0.1", "10.0.0.2").GetHashCode());
    }

    [Fact]
    public void Snapshot_WhenAnAdditionalAddressAppears_IsNoLongerEqual()
    {
        // The refresh coordinator relies on snapshot equality to report changes,
        // so an extra IPv4 address must make the snapshot differ.
        NetworkAdapterSnapshot before = TestData.Snapshot(ipv4Addresses: Create("192.168.1.50"));
        NetworkAdapterSnapshot after = TestData.Snapshot(
            ipv4Addresses: Create("192.168.1.50", "192.168.1.60"));

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Snapshot_WithIdenticalAddresses_RemainsEqual()
    {
        NetworkAdapterSnapshot before = TestData.Snapshot(ipv4Addresses: Create("192.168.1.50"));
        NetworkAdapterSnapshot after = TestData.Snapshot(ipv4Addresses: Create("192.168.1.50"));

        Assert.Equal(before, after);
    }

    private static Ipv4AddressCollection Create(params string[] addresses) =>
        new(addresses.Select(address => new Ipv4AddressAssignment(address, "255.255.255.0")));
}
