using IPMan.Domain.Networking;

namespace IPMan.Application.Networking;

public sealed class StaticIpv4ConfigurationValidator : IStaticIpv4ConfigurationValidator
{
    public StaticIpv4ValidationResult Validate(StaticIpv4Configuration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        List<StaticIpv4ValidationError> errors = new();
        Ipv4Value? address = ParseRequired(
            configuration.Ipv4Address,
            StaticIpv4ConfigurationField.Ipv4Address,
            errors);
        Ipv4Value? mask = ParseRequired(
            configuration.SubnetMask,
            StaticIpv4ConfigurationField.SubnetMask,
            errors);

        if (address is { } parsedAddress && !IsNormalAdapterAddress(parsedAddress))
        {
            Add(errors, StaticIpv4ConfigurationField.Ipv4Address, StaticIpv4ValidationErrorCode.AddressNotAllowed);
            address = null;
        }

        if (mask is { } parsedMask && !IsContiguousMask(parsedMask.Bits))
        {
            Add(errors, StaticIpv4ConfigurationField.SubnetMask, StaticIpv4ValidationErrorCode.NonContiguousSubnetMask);
            mask = null;
        }

        Ipv4Value? gateway = ParseOptional(
            configuration.Gateway,
            StaticIpv4ConfigurationField.Gateway,
            errors);
        Ipv4Value? primaryDns = ParseOptional(
            configuration.PrimaryDns,
            StaticIpv4ConfigurationField.PrimaryDns,
            errors);
        Ipv4Value? secondaryDns = ParseOptional(
            configuration.SecondaryDns,
            StaticIpv4ConfigurationField.SecondaryDns,
            errors);

        gateway = ValidateGateway(gateway, address, mask, errors);
        primaryDns = ValidateDns(primaryDns, StaticIpv4ConfigurationField.PrimaryDns, errors);
        secondaryDns = ValidateDns(secondaryDns, StaticIpv4ConfigurationField.SecondaryDns, errors);

        if (primaryDns is null && secondaryDns is not null)
        {
            Add(errors, StaticIpv4ConfigurationField.SecondaryDns, StaticIpv4ValidationErrorCode.PrimaryDnsRequired);
        }

        if (address is { } hostAddress && mask is { } subnetMask)
        {
            ValidateHostAddress(hostAddress, subnetMask, errors);
        }

        if (errors.Count > 0)
        {
            return StaticIpv4ValidationResult.Invalid(errors);
        }

        return StaticIpv4ValidationResult.Valid(
            new StaticIpv4Configuration(
                address!.Value.Text,
                mask!.Value.Text,
                gateway?.Text,
                primaryDns?.Text,
                secondaryDns?.Text));
    }

    private static Ipv4Value? ParseRequired(
        string? input,
        StaticIpv4ConfigurationField field,
        ICollection<StaticIpv4ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            Add(errors, field, StaticIpv4ValidationErrorCode.Required);
            return null;
        }

        if (!Ipv4Value.TryParse(input, out Ipv4Value value))
        {
            Add(errors, field, StaticIpv4ValidationErrorCode.InvalidIpv4);
            return null;
        }

        return value;
    }

    private static Ipv4Value? ParseOptional(
        string? input,
        StaticIpv4ConfigurationField field,
        ICollection<StaticIpv4ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (!Ipv4Value.TryParse(input, out Ipv4Value value))
        {
            Add(errors, field, StaticIpv4ValidationErrorCode.InvalidIpv4);
            return null;
        }

        return value;
    }

    private static Ipv4Value? ValidateGateway(
        Ipv4Value? gateway,
        Ipv4Value? address,
        Ipv4Value? mask,
        ICollection<StaticIpv4ValidationError> errors)
    {
        if (gateway is not { } value)
        {
            return null;
        }

        if (!IsNormalAdapterAddress(value))
        {
            Add(errors, StaticIpv4ConfigurationField.Gateway, StaticIpv4ValidationErrorCode.AddressNotAllowed);
            return null;
        }

        if (address is { } requestedAddress && value.Bits == requestedAddress.Bits)
        {
            Add(
                errors,
                StaticIpv4ConfigurationField.Gateway,
                StaticIpv4ValidationErrorCode.GatewayMatchesHostAddress);
            return null;
        }

        if (address is { } hostAddress && mask is { } subnetMask)
        {
            if ((value.Bits & subnetMask.Bits) != (hostAddress.Bits & subnetMask.Bits))
            {
                Add(errors, StaticIpv4ConfigurationField.Gateway, StaticIpv4ValidationErrorCode.GatewayOutsideSubnet);
                return null;
            }

            if (HasConventionalHostBits(subnetMask.Bits))
            {
                uint hostPart = value.Bits & ~subnetMask.Bits;
                uint hostMask = ~subnetMask.Bits;

                if (hostPart == 0 || hostPart == hostMask)
                {
                    Add(errors, StaticIpv4ConfigurationField.Gateway, StaticIpv4ValidationErrorCode.AddressNotAllowed);
                    return null;
                }
            }
        }

        return value;
    }

    private static Ipv4Value? ValidateDns(
        Ipv4Value? dns,
        StaticIpv4ConfigurationField field,
        ICollection<StaticIpv4ValidationError> errors)
    {
        if (dns is not { } value)
        {
            return null;
        }

        if (value.IsUnspecified || value.IsLimitedBroadcast || value.IsMulticast)
        {
            Add(errors, field, StaticIpv4ValidationErrorCode.AddressNotAllowed);
            return null;
        }

        return value;
    }

    private static void ValidateHostAddress(
        Ipv4Value address,
        Ipv4Value mask,
        ICollection<StaticIpv4ValidationError> errors)
    {
        // RFC 3021-style /31 point-to-point endpoints and /32 host routes do not
        // have conventional network/broadcast host values. Both are accepted.
        if (!HasConventionalHostBits(mask.Bits))
        {
            return;
        }

        uint hostPart = address.Bits & ~mask.Bits;

        if (hostPart == 0)
        {
            Add(errors, StaticIpv4ConfigurationField.Ipv4Address, StaticIpv4ValidationErrorCode.NetworkAddressNotAllowed);
        }
        else if (hostPart == ~mask.Bits)
        {
            Add(errors, StaticIpv4ConfigurationField.Ipv4Address, StaticIpv4ValidationErrorCode.BroadcastAddressNotAllowed);
        }
    }

    private static bool IsNormalAdapterAddress(Ipv4Value value) =>
        !value.IsUnspecified &&
        !value.IsLimitedBroadcast &&
        !value.IsLoopback &&
        !value.IsMulticast;

    private static bool IsContiguousMask(uint mask)
    {
        uint inverted = ~mask;
        return (inverted & (inverted + 1)) == 0;
    }

    private static bool HasConventionalHostBits(uint mask) => ~mask > 1;

    private static void Add(
        ICollection<StaticIpv4ValidationError> errors,
        StaticIpv4ConfigurationField field,
        StaticIpv4ValidationErrorCode code) =>
        errors.Add(new StaticIpv4ValidationError(field, code));
}
