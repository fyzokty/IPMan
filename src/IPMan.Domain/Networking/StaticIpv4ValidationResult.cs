namespace IPMan.Domain.Networking;

/// <summary>Validation failures or the one canonical configuration produced from valid input.</summary>
public sealed class StaticIpv4ValidationResult
{
    private StaticIpv4ValidationResult(
        StaticIpv4Configuration? normalizedConfiguration,
        IReadOnlyList<StaticIpv4ValidationError> errors)
    {
        NormalizedConfiguration = normalizedConfiguration;
        Errors = errors;
    }

    public bool IsValid => NormalizedConfiguration is not null;

    public StaticIpv4Configuration? NormalizedConfiguration { get; }

    public IReadOnlyList<StaticIpv4ValidationError> Errors { get; }

    public static StaticIpv4ValidationResult Valid(StaticIpv4Configuration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return new StaticIpv4ValidationResult(configuration, Array.Empty<StaticIpv4ValidationError>());
    }

    public static StaticIpv4ValidationResult Invalid(
        IEnumerable<StaticIpv4ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        StaticIpv4ValidationError[] errorArray = errors.ToArray();

        if (errorArray.Length == 0)
        {
            throw new ArgumentException("An invalid result requires at least one error.", nameof(errors));
        }

        return new StaticIpv4ValidationResult(null, Array.AsReadOnly(errorArray));
    }
}
