using System.Text.Json;
using IPMan.Application.Profiles;
using IPMan.Domain.Profiles;
using IPMan.Infrastructure.Common;

namespace IPMan.Infrastructure.Profiles;

/// <summary>Stores portable profiles as isolated, atomically written JSON documents.</summary>
public sealed class JsonProfileRepository : IProfileRepository
{
    private readonly string _profilesDirectory;
    private readonly ProfileJsonCodec _codec = new();

    /// <summary>Initializes a profile repository with an explicit storage directory.</summary>
    public JsonProfileRepository(ProfileRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.ProfilesDirectory))
        {
            throw new ArgumentException("Profiles directory is required.", nameof(options));
        }

        _profilesDirectory = Path.GetFullPath(options.ProfilesDirectory);
    }

    /// <inheritdoc />
    public async Task<ProfileLoadResult> LoadAllAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProfileScanResult scan = await ScanAsync(cancellationToken).ConfigureAwait(false);
        Dictionary<string, StoredProfile> profilesById = new(StringComparer.Ordinal);

        foreach (StoredProfile stored in scan.Profiles)
        {
            if (!profilesById.TryGetValue(stored.Profile.ProfileId, out StoredProfile? existing) ||
                stored.Profile.ModifiedAtUtc > existing.Profile.ModifiedAtUtc)
            {
                profilesById[stored.Profile.ProfileId] = stored;
            }
        }

        return new ProfileLoadResult(
            profilesById.Values.Select(stored => stored.Profile).ToArray(),
            scan.Problems);
    }

    /// <inheritdoc />
    public async Task<ProfileSaveResult> SaveAsync(
        NetworkProfile profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            ProfileScanResult scan = await ScanAsync(cancellationToken).ConfigureAwait(false);
            if (scan.DirectoryReadFailed)
            {
                return ProfileSaveResult.Failed(ProfileSaveStatus.IoFailure);
            }

            IEnumerable<string> otherNames = scan.Profiles
                .Where(stored => !string.Equals(
                    stored.Profile.ProfileId,
                    profile.ProfileId,
                    StringComparison.Ordinal))
                .Select(stored => stored.Profile.Name);
            string finalName = ProfileNameResolver.ResolveUnique(profile.Name, otherNames);
            NetworkProfile finalProfile = profile with { Name = finalName };
            string destinationPath = ResolveDestinationPath(finalProfile, scan);

            await AtomicJsonFileWriter.WriteAsync(
                destinationPath,
                overwrite: true,
                (stream, token) => _codec.SerializeAsync(stream, finalProfile, token),
                cancellationToken).ConfigureAwait(false);

            foreach (StoredProfile stored in scan.Profiles.Where(stored =>
                         string.Equals(
                             stored.Profile.ProfileId,
                             profile.ProfileId,
                             StringComparison.Ordinal) &&
                         !string.Equals(
                             stored.FilePath,
                             destinationPath,
                             StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    File.Delete(stored.FilePath);
                }
                catch (IOException)
                {
                    // The new file is authoritative; an older duplicate can be ignored.
                }
                catch (UnauthorizedAccessException)
                {
                    // Preserve the successful profile write result.
                }
            }

            return ProfileSaveResult.Success(finalProfile);
        }
        catch (ProfileJsonException)
        {
            return ProfileSaveResult.Failed(ProfileSaveStatus.InvalidContent);
        }
        catch (JsonException)
        {
            return ProfileSaveResult.Failed(ProfileSaveStatus.InvalidContent);
        }
        catch (UnauthorizedAccessException)
        {
            return ProfileSaveResult.Failed(ProfileSaveStatus.AccessDenied);
        }
        catch (IOException)
        {
            return ProfileSaveResult.Failed(ProfileSaveStatus.IoFailure);
        }
    }

    /// <inheritdoc />
    public async Task<ProfileDeleteResult> DeleteAsync(
        string profileId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            ProfileScanResult scan = await ScanAsync(cancellationToken).ConfigureAwait(false);
            if (scan.DirectoryReadFailed)
            {
                return ProfileDeleteResult.Failed(ProfileDeleteStatus.IoFailure);
            }

            StoredProfile[] matches = scan.Profiles
                .Where(stored => string.Equals(
                    stored.Profile.ProfileId,
                    profileId,
                    StringComparison.Ordinal))
                .ToArray();
            if (matches.Length == 0)
            {
                return ProfileDeleteResult.Failed(ProfileDeleteStatus.NotFound);
            }

            foreach (StoredProfile stored in matches)
            {
                File.Delete(stored.FilePath);
            }

            return ProfileDeleteResult.Success();
        }
        catch (UnauthorizedAccessException)
        {
            return ProfileDeleteResult.Failed(ProfileDeleteStatus.AccessDenied);
        }
        catch (IOException)
        {
            return ProfileDeleteResult.Failed(ProfileDeleteStatus.IoFailure);
        }
    }

    /// <inheritdoc />
    public async Task<ProfileImportResult> ImportAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            NetworkProfile profile = await _codec
                .DeserializeAsync(source, cancellationToken)
                .ConfigureAwait(false);
            ProfileSaveResult saveResult = await SaveAsync(profile, cancellationToken)
                .ConfigureAwait(false);
            return saveResult.Status switch
            {
                ProfileSaveStatus.Success => ProfileImportResult.Success(saveResult.Profile!),
                ProfileSaveStatus.AccessDenied =>
                    ProfileImportResult.Failed(ProfileImportStatus.AccessDenied),
                ProfileSaveStatus.IoFailure =>
                    ProfileImportResult.Failed(ProfileImportStatus.IoFailure),
                _ => ProfileImportResult.Failed(ProfileImportStatus.InvalidContent)
            };
        }
        catch (JsonException)
        {
            return ProfileImportResult.Failed(ProfileImportStatus.InvalidContent);
        }
        catch (UnauthorizedAccessException)
        {
            return ProfileImportResult.Failed(ProfileImportStatus.AccessDenied);
        }
        catch (IOException)
        {
            return ProfileImportResult.Failed(ProfileImportStatus.IoFailure);
        }
    }

    /// <inheritdoc />
    public async Task<ProfileExportResult> ExportAsync(
        string profileId,
        Stream destination,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentNullException.ThrowIfNull(destination);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            ProfileScanResult scan = await ScanAsync(cancellationToken).ConfigureAwait(false);
            if (scan.DirectoryReadFailed)
            {
                return ProfileExportResult.Failed(ProfileExportStatus.IoFailure);
            }

            NetworkProfile? profile = scan.Profiles
                .Where(stored => string.Equals(
                    stored.Profile.ProfileId,
                    profileId,
                    StringComparison.Ordinal))
                .OrderByDescending(stored => stored.Profile.ModifiedAtUtc)
                .Select(stored => stored.Profile)
                .FirstOrDefault();
            if (profile is null)
            {
                return ProfileExportResult.Failed(ProfileExportStatus.NotFound);
            }

            await _codec.SerializeAsync(destination, profile, cancellationToken)
                .ConfigureAwait(false);
            return ProfileExportResult.Success();
        }
        catch (UnauthorizedAccessException)
        {
            return ProfileExportResult.Failed(ProfileExportStatus.AccessDenied);
        }
        catch (IOException)
        {
            return ProfileExportResult.Failed(ProfileExportStatus.IoFailure);
        }
    }

    private async Task<ProfileScanResult> ScanAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_profilesDirectory))
        {
            return ProfileScanResult.Empty;
        }

        string[] filePaths;
        try
        {
            filePaths = Directory.GetFiles(_profilesDirectory, "*.json", SearchOption.TopDirectoryOnly);
        }
        catch (UnauthorizedAccessException)
        {
            return ProfileScanResult.DirectoryFailure(_profilesDirectory);
        }
        catch (IOException)
        {
            return ProfileScanResult.DirectoryFailure(_profilesDirectory);
        }

        List<StoredProfile> profiles = new();
        List<NetworkProfileProblem> problems = new();
        foreach (string filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using FileStream stream = new(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                NetworkProfile profile = await _codec
                    .DeserializeAsync(stream, cancellationToken)
                    .ConfigureAwait(false);
                profiles.Add(new StoredProfile(filePath, profile));
            }
            catch (ProfileJsonException exception)
            {
                problems.Add(new NetworkProfileProblem(filePath, exception.FailureKind));
            }
            catch (JsonException)
            {
                problems.Add(new NetworkProfileProblem(
                    filePath,
                    ProfileLoadFailureKind.MalformedJson));
            }
            catch (UnauthorizedAccessException)
            {
                problems.Add(new NetworkProfileProblem(filePath, ProfileLoadFailureKind.ReadFailure));
            }
            catch (IOException)
            {
                problems.Add(new NetworkProfileProblem(filePath, ProfileLoadFailureKind.ReadFailure));
            }
        }

        return new ProfileScanResult(profiles, problems, filePaths, DirectoryReadFailed: false);
    }

    private static string SanitizeFileName(string name)
    {
        HashSet<char> invalidCharacters = new(Path.GetInvalidFileNameChars());
        char[] sanitized = name
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray();
        string result = new string(sanitized).Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(result) ? "profile" : result;
    }

    private string ResolveDestinationPath(NetworkProfile profile, ProfileScanResult scan)
    {
        string baseName = SanitizeFileName(profile.Name);
        string path = Path.Combine(_profilesDirectory, $"{baseName}.json");
        bool belongsToProfile = scan.Profiles.Any(stored =>
            string.Equals(stored.FilePath, path, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                stored.Profile.ProfileId,
                profile.ProfileId,
                StringComparison.Ordinal));
        bool collides = !belongsToProfile && scan.FilePaths.Any(existingPath =>
            string.Equals(existingPath, path, StringComparison.OrdinalIgnoreCase));
        if (!collides)
        {
            return path;
        }

        string shortId = SanitizeFileName(profile.ProfileId);
        shortId = shortId[..Math.Min(8, shortId.Length)];
        return Path.Combine(_profilesDirectory, $"{baseName}-{shortId}.json");
    }

    private sealed record StoredProfile(string FilePath, NetworkProfile Profile);

    private sealed record ProfileScanResult(
        IReadOnlyList<StoredProfile> Profiles,
        IReadOnlyList<NetworkProfileProblem> Problems,
        IReadOnlyList<string> FilePaths,
        bool DirectoryReadFailed)
    {
        public static ProfileScanResult Empty { get; } =
            new([], [], [], DirectoryReadFailed: false);

        public static ProfileScanResult DirectoryFailure(string path) =>
            new(
                [],
                [new NetworkProfileProblem(path, ProfileLoadFailureKind.ReadFailure)],
                [],
                DirectoryReadFailed: true);
    }
}
