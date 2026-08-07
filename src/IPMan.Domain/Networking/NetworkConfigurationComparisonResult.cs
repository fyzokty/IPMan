namespace IPMan.Domain.Networking;

/// <summary>Typed comparison result used for no-change detection and later field highlighting.</summary>
public sealed record NetworkConfigurationComparisonResult(NetworkConfigurationDifference Differences)
{
    public bool IsEquivalent => Differences == NetworkConfigurationDifference.None;

    public bool HasDifference(NetworkConfigurationDifference difference) =>
        difference != NetworkConfigurationDifference.None && Differences.HasFlag(difference);
}
