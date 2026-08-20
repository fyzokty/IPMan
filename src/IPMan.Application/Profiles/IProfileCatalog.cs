using IPMan.Domain.Profiles;

namespace IPMan.Application.Profiles;

/// <summary>Provides the authoritative in-memory view of persisted profiles.</summary>
public interface IProfileCatalog : IDisposable
{
    /// <summary>Gets profiles ordered for display.</summary>
    IReadOnlyList<NetworkProfile> Profiles { get; }

    /// <summary>Gets problems encountered while loading individual profile documents.</summary>
    IReadOnlyList<NetworkProfileProblem> Problems { get; }

    /// <summary>Raised after a load succeeds or fails.</summary>
    event EventHandler? Changed;

    /// <summary>Loads the initial catalog contents.</summary>
    Task InitializeAsync(CancellationToken cancellationToken);

    /// <summary>Starts refreshing the catalog when its directory changes.</summary>
    void StartWatching();

    /// <summary>Stops refreshing the catalog when its directory changes.</summary>
    void StopWatching();

    /// <summary>Saves a profile and refreshes the catalog afterward.</summary>
    Task<ProfileSaveResult> SaveAsync(
        NetworkProfile profile,
        CancellationToken cancellationToken);

    /// <summary>Deletes a profile and refreshes the catalog afterward.</summary>
    Task<ProfileDeleteResult> DeleteAsync(
        string profileId,
        CancellationToken cancellationToken);
}
