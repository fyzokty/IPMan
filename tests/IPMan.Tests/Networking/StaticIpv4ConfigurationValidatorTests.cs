using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Networking;

public sealed class StaticIpv4ConfigurationValidatorTests
{
    private readonly StaticIpv4ConfigurationValidator _validator = new();

    [Fact]
    public void Validate_WhenOrdinaryConfigurationIsValid_NormalizesEveryValue()
    {
        StaticIpv4Configuration input = new(
            " 192.168.001.050 ",
            " 255.255.255.000 ",
            " 192.168.001.001 ",
            " 008.008.008.008 ",
            " 001.001.001.001 ");

        StaticIpv4ValidationResult result = _validator.Validate(input);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(
            new StaticIpv4Configuration(
                "192.168.1.50",
                "255.255.255.0",
                "192.168.1.1",
                "8.8.8.8",
                "1.1.1.1"),
            result.NormalizedConfiguration);
    }

    [Theory]
    [InlineData("8.8.8.8", "255.255.255.0")]
    [InlineData("169.254.20.30", "255.255.0.0")]
    [InlineData("10.10.10.10", "255.255.255.252")]
    public void Validate_WhenMainAddressRangeIsAllowed_ReturnsValid(string address, string mask)
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(address, mask));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-address")]
    [InlineData("192.168.1")]
    [InlineData("2001:db8::1")]
    public void Validate_WhenMainAddressCannotBeParsed_ReturnsStructuredError(string address)
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(ipv4Address: address));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == StaticIpv4ConfigurationField.Ipv4Address);
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("127.0.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("239.255.255.255")]
    [InlineData("255.255.255.255")]
    public void Validate_WhenMainAddressIsNotAssignable_RejectsIt(string address)
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(ipv4Address: address));

        AssertError(
            result,
            StaticIpv4ConfigurationField.Ipv4Address,
            StaticIpv4ValidationErrorCode.AddressNotAllowed);
    }

    [Theory]
    [InlineData("255.255.255.0")]
    [InlineData("255.255.0.0")]
    [InlineData("255.255.255.252")]
    [InlineData("255.255.255.254")]
    [InlineData("255.255.255.255")]
    [InlineData("0.0.0.0")]
    public void Validate_WhenSubnetMaskIsContiguous_AcceptsIt(string mask)
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(subnetMask: mask));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("255.0.255.0")]
    [InlineData("255.255.128.255")]
    [InlineData("255.255.255.253")]
    public void Validate_WhenSubnetMaskIsNotContiguous_RejectsIt(string mask)
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(subnetMask: mask));

        AssertError(
            result,
            StaticIpv4ConfigurationField.SubnetMask,
            StaticIpv4ValidationErrorCode.NonContiguousSubnetMask);
    }

    [Fact]
    public void Validate_WhenGatewayIsEmpty_NormalizesItToNull()
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(gateway: "   "));

        Assert.True(result.IsValid);
        Assert.Null(result.NormalizedConfiguration!.Gateway);
    }

    [Fact]
    public void Validate_WhenGatewayIsInSubnet_AcceptsIt()
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(gateway: "192.168.1.254"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenGatewayIsOutsideSubnet_RejectsIt()
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(gateway: "192.168.2.1"));

        AssertError(
            result,
            StaticIpv4ConfigurationField.Gateway,
            StaticIpv4ValidationErrorCode.GatewayOutsideSubnet);
    }

    [Theory]
    [InlineData("192.168.1.50", "255.255.255.0", "192.168.1.50")]
    [InlineData("192.168.1.0", "255.255.255.254", "192.168.1.0")]
    [InlineData("192.168.1.1", "255.255.255.254", "192.168.1.1")]
    [InlineData("192.168.1.77", "255.255.255.255", "192.168.1.77")]
    public void Validate_WhenGatewayMatchesRequestedHost_RejectsWithSpecificError(
        string address,
        string mask,
        string gateway)
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(address, mask, gateway));

        AssertError(
            result,
            StaticIpv4ConfigurationField.Gateway,
            StaticIpv4ValidationErrorCode.GatewayMatchesHostAddress);
    }

    [Theory]
    [InlineData("192.168.1.0", "192.168.1.1")]
    [InlineData("192.168.1.1", "192.168.1.0")]
    public void Validate_WhenSlash31GatewayIsPeerEndpoint_AcceptsIt(string address, string gateway)
    {
        StaticIpv4ValidationResult result = _validator.Validate(
            Valid(address, "255.255.255.254", gateway));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenSlash32GatewayIsExternal_RejectsAsOutsideSubnet()
    {
        StaticIpv4ValidationResult result = _validator.Validate(
            Valid("192.168.1.77", "255.255.255.255", "192.168.1.1"));

        AssertError(
            result,
            StaticIpv4ConfigurationField.Gateway,
            StaticIpv4ValidationErrorCode.GatewayOutsideSubnet);
    }

    [Theory]
    [InlineData("192.168.1.0")]
    [InlineData("192.168.1.255")]
    [InlineData("127.0.0.1")]
    [InlineData("224.0.0.1")]
    public void Validate_WhenGatewayIsNotUsable_RejectsIt(string gateway)
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(gateway: gateway));

        AssertError(
            result,
            StaticIpv4ConfigurationField.Gateway,
            StaticIpv4ValidationErrorCode.AddressNotAllowed);
    }

    [Fact]
    public void Validate_WhenBothDnsFieldsAreEmpty_AcceptsThem()
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(primaryDns: null, secondaryDns: " "));

        Assert.True(result.IsValid);
        Assert.Null(result.NormalizedConfiguration!.PrimaryDns);
        Assert.Null(result.NormalizedConfiguration.SecondaryDns);
    }

    [Theory]
    [InlineData("8.8.8.8", null)]
    [InlineData("127.0.0.1", null)]
    [InlineData("8.8.8.8", "1.1.1.1")]
    public void Validate_WhenDnsCombinationIsValid_AcceptsIt(string primaryDns, string? secondaryDns)
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(primaryDns: primaryDns, secondaryDns: secondaryDns));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenOnlySecondaryDnsIsPresent_RejectsWithoutReordering()
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(primaryDns: null, secondaryDns: "1.1.1.1"));

        AssertError(
            result,
            StaticIpv4ConfigurationField.SecondaryDns,
            StaticIpv4ValidationErrorCode.PrimaryDnsRequired);
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    public void Validate_WhenDnsAddressIsNotAllowed_RejectsIt(string dns)
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(primaryDns: dns));

        AssertError(
            result,
            StaticIpv4ConfigurationField.PrimaryDns,
            StaticIpv4ValidationErrorCode.AddressNotAllowed);
    }

    [Fact]
    public void Validate_WhenAddressIsOrdinaryNetworkAddress_RejectsIt()
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid("192.168.1.0", "255.255.255.0"));

        AssertError(
            result,
            StaticIpv4ConfigurationField.Ipv4Address,
            StaticIpv4ValidationErrorCode.NetworkAddressNotAllowed);
    }

    [Fact]
    public void Validate_WhenAddressIsOrdinarySubnetBroadcast_RejectsIt()
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid("192.168.1.255", "255.255.255.0"));

        AssertError(
            result,
            StaticIpv4ConfigurationField.Ipv4Address,
            StaticIpv4ValidationErrorCode.BroadcastAddressNotAllowed);
    }

    [Theory]
    [InlineData("192.168.1.0", "255.255.255.254")]
    [InlineData("192.168.1.1", "255.255.255.254")]
    [InlineData("192.168.1.77", "255.255.255.255")]
    public void Validate_WhenMaskIsSlash31OrSlash32_AcceptsEndpoint(string address, string mask)
    {
        StaticIpv4ValidationResult result = _validator.Validate(Valid(address, mask));

        Assert.True(result.IsValid);
    }

    private static StaticIpv4Configuration Valid(
        string ipv4Address = "192.168.1.50",
        string subnetMask = "255.255.255.0",
        string? gateway = null,
        string? primaryDns = null,
        string? secondaryDns = null) =>
        new(ipv4Address, subnetMask, gateway, primaryDns, secondaryDns);

    private static void AssertError(
        StaticIpv4ValidationResult result,
        StaticIpv4ConfigurationField field,
        StaticIpv4ValidationErrorCode code)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Field == field && error.Code == code);
    }
}
