using System.IO;
using IPMan.Application.Profiles;
using IPMan.Domain.Profiles;

namespace IPMan.Tests.Fakes;

public sealed class FakeProfileEditorCatalog : IProfileCatalog
{
    public IReadOnlyList<NetworkProfile> Profiles { get; set; } = [];
    public IReadOnlyList<NetworkProfileProblem> Problems => [];
    public List<NetworkProfile> Saved { get; } = [];
    public ProfileSaveResult? SaveResult { get; set; }
    public Exception? Failure { get; set; }
    public event EventHandler? Changed;

    public Task<ProfileSaveResult> SaveAsync(NetworkProfile profile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Failure is not null) return Task.FromException<ProfileSaveResult>(Failure);
        if (SaveResult is { IsSuccess: false }) return Task.FromResult(SaveResult);
        Saved.Add(profile);
        Profiles = Profiles.Where(item => item.ProfileId != profile.ProfileId).Append(profile).ToArray();
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.FromResult(SaveResult ?? ProfileSaveResult.Success(profile));
    }

    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public void StartWatching() { }
    public void StopWatching() { }
    public void Dispose() { }
    public Task<ProfileDeleteResult> DeleteAsync(string profileId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
    public Task<ProfileImportResult> ImportAsync(Stream source, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
    public Task<ProfileExportResult> ExportAsync(string profileId, Stream destination, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
