using IPMan.Domain.Profiles;

namespace IPMan.Application.Profiles;

/// <summary>Persists and transfers portable network profiles.</summary>
public interface IProfileRepository
{
    /// <summary>Loads every readable profile while reporting isolated document problems.</summary>
    Task<ProfileLoadResult> LoadAllAsync(CancellationToken cancellationToken);

    /// <summary>Saves a profile and returns its final, uniquely named representation.</summary>
    Task<ProfileSaveResult> SaveAsync(
        NetworkProfile profile,
        CancellationToken cancellationToken);

    /// <summary>Deletes the profile identified by JSON content identity.</summary>
    Task<ProfileDeleteResult> DeleteAsync(
        string profileId,
        CancellationToken cancellationToken);

    /// <summary>Imports one profile from a caller-owned stream.</summary>
    Task<ProfileImportResult> ImportAsync(
        Stream source,
        CancellationToken cancellationToken);

    /// <summary>Exports one profile to a caller-owned stream.</summary>
    Task<ProfileExportResult> ExportAsync(
        string profileId,
        Stream destination,
        CancellationToken cancellationToken);
}
