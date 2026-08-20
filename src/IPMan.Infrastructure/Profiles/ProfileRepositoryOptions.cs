namespace IPMan.Infrastructure.Profiles;

/// <summary>Configures profile document storage.</summary>
public sealed class ProfileRepositoryOptions
{
    /// <summary>Gets the directory containing profile JSON documents.</summary>
    public required string ProfilesDirectory { get; init; }
}
