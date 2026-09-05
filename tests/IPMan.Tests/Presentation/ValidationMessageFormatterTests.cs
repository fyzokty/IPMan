using IPMan.App.Presentation;
using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed class ValidationMessageFormatterTests
{
    [Fact]
    public void Describe_ReturnsLocalizedTextForEveryErrorCode()
    {
        foreach (StaticIpv4ValidationErrorCode code in Enum.GetValues<StaticIpv4ValidationErrorCode>())
        {
            string message = ValidationMessageFormatter.Describe(
                new StaticIpv4ValidationError(StaticIpv4ConfigurationField.Ipv4Address, code));

            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.NotEqual(code.ToString(), message);
        }
    }

    [Theory]
    [InlineData(StaticIpv4ValidationErrorCode.Required)]
    [InlineData(StaticIpv4ValidationErrorCode.InvalidIpv4)]
    [InlineData(StaticIpv4ValidationErrorCode.AddressNotAllowed)]
    public void Describe_WhenCodeNeedsFieldLabel_IncludesLocalizedFieldLabel(
        StaticIpv4ValidationErrorCode code)
    {
        string field = ValidationMessageFormatter.DescribeField(
            StaticIpv4ConfigurationField.PrimaryDns);
        string message = ValidationMessageFormatter.Describe(
            new StaticIpv4ValidationError(StaticIpv4ConfigurationField.PrimaryDns, code));

        Assert.Contains(field, message, StringComparison.Ordinal);
    }
}
