using IPMan.Domain.Profiles;

namespace IPMan.Application.Profiles;

/// <summary>Contains healthy profiles and independently reported document problems.</summary>
public sealed record ProfileLoadResult(
    IReadOnlyList<NetworkProfile> Profiles,
    IReadOnlyList<NetworkProfileProblem> Problems);
