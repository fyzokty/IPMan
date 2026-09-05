using IPMan.App.Resources;
using IPMan.Domain.Networking;

namespace IPMan.App.Presentation;

/// <summary>Translates structured static IPv4 validation failures into localized text.</summary>
public static class ValidationMessageFormatter
{
    /// <summary>Returns the localized description of one validation error.</summary>
    public static string Describe(StaticIpv4ValidationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        string field = DescribeField(error.Field);

        return error.Code switch
        {
            StaticIpv4ValidationErrorCode.Required => Strings.FormatValidationRequired(field),
            StaticIpv4ValidationErrorCode.InvalidIpv4 => Strings.FormatValidationInvalidIpv4(field),
            StaticIpv4ValidationErrorCode.AddressNotAllowed => Strings.FormatValidationAddressNotAllowed(field),
            StaticIpv4ValidationErrorCode.NonContiguousSubnetMask => Strings.ValidationNonContiguousSubnetMask,
            StaticIpv4ValidationErrorCode.NetworkAddressNotAllowed => Strings.ValidationNetworkAddressNotAllowed,
            StaticIpv4ValidationErrorCode.BroadcastAddressNotAllowed => Strings.ValidationBroadcastAddressNotAllowed,
            StaticIpv4ValidationErrorCode.GatewayOutsideSubnet => Strings.ValidationGatewayOutsideSubnet,
            StaticIpv4ValidationErrorCode.PrimaryDnsRequired => Strings.ValidationPrimaryDnsRequired,
            StaticIpv4ValidationErrorCode.GatewayMatchesHostAddress => Strings.ValidationGatewayMatchesHostAddress,
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
}
