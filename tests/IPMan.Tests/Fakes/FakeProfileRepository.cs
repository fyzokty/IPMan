using System.IO;
using IPMan.Application.Profiles;
using IPMan.Domain.Profiles;

namespace IPMan.Tests.Fakes;

public sealed class FakeProfileRepository : IProfileRepository
{
    public ProfileLoadResult LoadResult { get; set; } = new([], []);

    public ProfileSaveResult SaveResult { get; set; } =
        ProfileSaveResult.Success(TestData.Profile());

    public ProfileDeleteResult DeleteResult { get; set; } = ProfileDeleteResult.Success();

    public ProfileImportResult ImportResult { get; set; } =
        ProfileImportResult.Success(TestData.Profile());

    public ProfileExportResult ExportResult { get; set; } = ProfileExportResult.Success();

    public List<CancellationToken> LoadCalls { get; } = new();

    public List<NetworkProfile> SavedProfiles { get; } = new();

    public List<string> DeletedProfileIds { get; } = new();

    public List<Stream> ImportedSources { get; } = new();

    public List<(string ProfileId, Stream Destination)> ExportedProfiles { get; } = new();

    public Action? OnLoad { get; set; }

    public Action<NetworkProfile>? OnSave { get; set; }

    public Action<string>? OnDelete { get; set; }

    public Func<CancellationToken, Task<ProfileLoadResult>>? LoadAsyncOverride { get; set; }

    public Task<ProfileLoadResult> LoadAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LoadCalls.Add(cancellationToken);
        OnLoad?.Invoke();

        return LoadAsyncOverride is null
            ? Task.FromResult(LoadResult)
            : LoadAsyncOverride(cancellationToken);
    }

    public Task<ProfileSaveResult> SaveAsync(
        NetworkProfile profile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SavedProfiles.Add(profile);
        OnSave?.Invoke(profile);
        return Task.FromResult(SaveResult);
    }

    public Task<ProfileDeleteResult> DeleteAsync(
        string profileId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DeletedProfileIds.Add(profileId);
        OnDelete?.Invoke(profileId);
        return Task.FromResult(DeleteResult);
    }

    public Task<ProfileImportResult> ImportAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ImportedSources.Add(source);
        return Task.FromResult(ImportResult);
    }

    public Task<ProfileExportResult> ExportAsync(
        string profileId,
        Stream destination,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExportedProfiles.Add((profileId, destination));
        return Task.FromResult(ExportResult);
    }
}
