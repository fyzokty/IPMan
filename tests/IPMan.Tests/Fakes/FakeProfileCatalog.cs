using System.IO;
using IPMan.Application.Profiles;
using IPMan.Domain.Profiles;

namespace IPMan.Tests.Fakes;

public sealed class FakeProfileCatalog : IProfileCatalog
{
    public IReadOnlyList<NetworkProfile> ProfileItems { get; set; } = [];

    public IReadOnlyList<NetworkProfileProblem> ProblemItems { get; set; } = [];

    public bool FailProfiles { get; set; }

    public bool FailProblems { get; set; }

    public IReadOnlyList<NetworkProfile> Profiles => FailProfiles
        ? throw new InvalidOperationException("Profile source failed.") : ProfileItems;

    public IReadOnlyList<NetworkProfileProblem> Problems => FailProblems
        ? throw new InvalidOperationException("Problem source failed.") : ProblemItems;

    public event EventHandler? Changed;

    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void StartWatching() { }

    public void StopWatching() { }

    public Task<ProfileSaveResult> SaveAsync(NetworkProfile profile, CancellationToken cancellationToken) =>
        Task.FromResult(ProfileSaveResult.Success(profile));

    public Task<ProfileDeleteResult> DeleteAsync(string profileId, CancellationToken cancellationToken) =>
        Task.FromResult(ProfileDeleteResult.Success());

    public Task<ProfileImportResult> ImportAsync(Stream source, CancellationToken cancellationToken) =>
        Task.FromResult(ProfileImportResult.Failed(ProfileImportStatus.InvalidContent));

    public Task<ProfileExportResult> ExportAsync(string profileId, Stream destination, CancellationToken cancellationToken) =>
        Task.FromResult(ProfileExportResult.Success());

    public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public void Dispose() { }
}
