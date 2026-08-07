using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class Ipv4AddressValueCollectionTests
{
    private static readonly string[] OrderedValues = { "192.168.1.1", "10.0.0.1" };

    private static readonly string[] ReversedValues = { "10.0.0.1", "192.168.1.1" };

    [Fact]
    public void Constructor_PreservesValuesAndOrder()
    {
        Ipv4AddressValueCollection collection = new(OrderedValues);

        Assert.Equal(OrderedValues, collection);
    }

    [Fact]
    public void Equals_WhenSequencesMatch_ReturnsTrue()
    {
        Assert.Equal(
            new Ipv4AddressValueCollection(OrderedValues),
            new Ipv4AddressValueCollection(OrderedValues));
    }

    [Fact]
    public void Equals_WhenOrderChanges_ReturnsFalse()
    {
        Assert.NotEqual(
            new Ipv4AddressValueCollection(OrderedValues),
            new Ipv4AddressValueCollection(ReversedValues));
    }
}
