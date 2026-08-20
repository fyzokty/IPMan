namespace IPMan.Domain.Profiles;

/// <summary>Describes one profile document that could not be loaded.</summary>
public sealed record NetworkProfileProblem(string FilePath, ProfileLoadFailureKind Kind);
