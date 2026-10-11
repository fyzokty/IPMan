using IPMan.App.Resources;
using IPMan.Domain.Networking;

namespace IPMan.App.Presentation;

/// <summary>Translates structured static IPv4 validation failures into localized text with a concrete example.</summary>
public static class ValidationMessageFormatter
{
    /// <summary>Returns the localized description of one validation error.</summary>
    /// <param name="error">The validation error to describe.</param>
    /// <param name="fieldLabelOverride">A label to use instead of the field's standard label, when the value belongs elsewhere.</param>
    public static string Describe(StaticIpv4ValidationError error, string? fieldLabelOverride = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        string field = fieldLabelOverride ?? DescribeField(error.Field);
        string example = DescribeExample(error.Field);

        return error.Code switch
        {
            StaticIpv4ValidationErrorCode.Required => Strings.FormatValidationRequired(field, example),
            StaticIpv4ValidationErrorCode.InvalidIpv4 => Strings.FormatValidationInvalidIpv4(field, example),
            StaticIpv4ValidationErrorCode.AddressNotAllowed => Strings.FormatValidationAddressNotAllowed(field, example),
            StaticIpv4ValidationErrorCode.NonContiguousSubnetMask =>
                Strings.FormatValidationNonContiguousSubnetMask(Strings.ExampleMask),
            StaticIpv4ValidationErrorCode.NetworkAddressNotAllowed =>
                Strings.FormatValidationNetworkAddressNotAllowed(Strings.ExampleAddress),
            StaticIpv4ValidationErrorCode.BroadcastAddressNotAllowed =>
                Strings.FormatValidationBroadcastAddressNotAllowed(Strings.ExampleAddress),
            StaticIpv4ValidationErrorCode.GatewayOutsideSubnet =>
                Strings.FormatValidationGatewayOutsideSubnet(Strings.ExampleAddress, Strings.ExampleGateway),
            StaticIpv4ValidationErrorCode.PrimaryDnsRequired =>
                Strings.FormatValidationPrimaryDnsRequired(Strings.ExamplePrimaryDns),
            StaticIpv4ValidationErrorCode.GatewayMatchesHostAddress =>
                Strings.FormatValidationGatewayMatchesHostAddress(Strings.ExampleAddress, Strings.ExampleGateway),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Code, null)
        };
    }

    /// <summary>Returns the localized label for an editable static IPv4 field.</summary>
    public static string DescribeField(StaticIpv4ConfigurationField field) => field switch
    {
        StaticIpv4ConfigurationField.Ipv4Address => Strings.FieldIpv4Address,
        StaticIpv4ConfigurationField.SubnetMask => Strings.FieldSubnetMask,
        StaticIpv4ConfigurationField.Gateway => Strings.FieldGateway,
        StaticIpv4ConfigurationField.PrimaryDns => Strings.FieldPrimaryDns,
        StaticIpv4ConfigurationField.SecondaryDns => Strings.FieldSecondaryDns,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
    };

    /// <summary>Returns a valid sample value for an editable static IPv4 field.</summary>
    public static string DescribeExample(StaticIpv4ConfigurationField field) => field switch
    {
        StaticIpv4ConfigurationField.Ipv4Address => Strings.ExampleAddress,
        StaticIpv4ConfigurationField.SubnetMask => Strings.ExampleMask,
        StaticIpv4ConfigurationField.Gateway => Strings.ExampleGateway,
        StaticIpv4ConfigurationField.PrimaryDns => Strings.ExamplePrimaryDns,
        StaticIpv4ConfigurationField.SecondaryDns => Strings.ExampleSecondaryDns,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
    };
}
