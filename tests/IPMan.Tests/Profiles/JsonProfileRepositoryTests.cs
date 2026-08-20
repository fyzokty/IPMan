using System.IO;
using System.Text;
using System.Text.Json;
using IPMan.Application.Profiles;
using IPMan.Domain.Profiles;
using IPMan.Infrastructure.Profiles;
using Xunit;

namespace IPMan.Tests.Profiles;

public sealed class JsonProfileRepositoryTests
{
    [Fact]
    public async Task LoadAllAsync_WhenProfilesDirectoryDoesNotExist_ReturnsEmptyResult()
    {
        string parent = CreateTestDirectory();
        string profilesDirectory = Path.Combine(parent, "Profiles");

        try
        {
            ProfileLoadResult result = await CreateRepository(profilesDirectory)
                .LoadAllAsync(CancellationToken.None);

            Assert.Empty(result.Profiles);
            Assert.Empty(result.Problems);
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenProfileIsValid_WritesOneAtomicCamelCaseDocument()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonProfileRepository repository = CreateRepository(directory);

            ProfileSaveResult result = await repository.SaveAsync(
                TestData.Profile(name: "Factory PLC"),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            string filePath = Assert.Single(Directory.GetFiles(directory, "*.json"));
            Assert.Equal("Factory PLC.json", Path.GetFileName(filePath));
            using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(filePath));
            JsonElement root = document.RootElement;
            Assert.Equal("profile-1", root.GetProperty("profileId").GetString());
            Assert.Equal("Factory PLC", root.GetProperty("name").GetString());
            Assert.Equal(JsonValueKind.String, root.GetProperty("mode").ValueKind);
            Assert.Equal("Static", root.GetProperty("mode").GetString());
            Assert.False(root.TryGetProperty("ProfileId", out _));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenNameBelongsToDifferentProfile_UsesUniqueNameWithoutChangingOriginal()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonProfileRepository repository = CreateRepository(directory);
            ProfileSaveResult first = await repository.SaveAsync(
                TestData.Profile(profileId: "profile-1", name: "PLC"),
                CancellationToken.None);
            string originalPath = Assert.Single(Directory.GetFiles(directory, "*.json"));
            string originalJson = await File.ReadAllTextAsync(originalPath);

            ProfileSaveResult second = await repository.SaveAsync(
                TestData.Profile(profileId: "profile-2", name: "PLC"),
                CancellationToken.None);

            Assert.True(first.IsSuccess);
            Assert.True(second.IsSuccess);
            Assert.Equal("PLC (1)", second.Profile!.Name);
            Assert.True(File.Exists(originalPath));
            Assert.Equal(originalJson, await File.ReadAllTextAsync(originalPath));
            Assert.Equal(2, Directory.GetFiles(directory, "*.json").Length);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenSameProfileKeepsName_DoesNotTreatOwnNameAsCollision()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonProfileRepository repository = CreateRepository(directory);
            await repository.SaveAsync(TestData.Profile(name: "PLC"), CancellationToken.None);

            ProfileSaveResult result = await repository.SaveAsync(
                TestData.Profile(name: "PLC", description: "Updated"),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("PLC", result.Profile!.Name);
            Assert.Single(Directory.GetFiles(directory, "*.json"));
            Assert.True(File.Exists(Path.Combine(directory, "PLC.json")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenProfileNameChanges_WritesNewFileAndDeletesOldFile()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonProfileRepository repository = CreateRepository(directory);
            await repository.SaveAsync(TestData.Profile(name: "PLC"), CancellationToken.None);
            string oldPath = Path.Combine(directory, "PLC.json");

            ProfileSaveResult result = await repository.SaveAsync(
                TestData.Profile(name: "Office"),
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.False(File.Exists(oldPath));
            Assert.True(File.Exists(Path.Combine(directory, "Office.json")));
            Assert.Single(Directory.GetFiles(directory, "*.json"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenOldFileCannotBeDeleted_ReturnsSuccessWithNewFile()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonProfileRepository repository = CreateRepository(directory);
            await repository.SaveAsync(TestData.Profile(name: "PLC"), CancellationToken.None);
            string oldPath = Path.Combine(directory, "PLC.json");

            ProfileSaveResult result;
            await using (FileStream lockStream = new(
                             oldPath,
                             FileMode.Open,
                             FileAccess.Read,
                             FileShare.Read))
            {
                result = await repository.SaveAsync(
                    TestData.Profile(name: "Office"),
                    CancellationToken.None);
            }

            Assert.True(result.IsSuccess);
            Assert.True(File.Exists(oldPath));
            Assert.True(File.Exists(Path.Combine(directory, "Office.json")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAllAsync_WhenOneDocumentIsMalformed_IsolatesProblemAndPreservesFile()
    {
        string directory = CreateTestDirectory();
        string brokenPath = Path.Combine(directory, "broken.json");

        try
        {
            JsonProfileRepository repository = CreateRepository(directory);
            await repository.SaveAsync(TestData.Profile(), CancellationToken.None);
            await File.WriteAllTextAsync(brokenPath, "{ definitely not json");

            ProfileLoadResult result = await repository.LoadAllAsync(CancellationToken.None);

            Assert.Single(result.Profiles);
            NetworkProfileProblem problem = Assert.Single(result.Problems);
            Assert.Equal(brokenPath, problem.FilePath);
            Assert.Equal(ProfileLoadFailureKind.MalformedJson, problem.Kind);
            Assert.True(File.Exists(brokenPath));
            Assert.Equal("{ definitely not json", await File.ReadAllTextAsync(brokenPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAllAsync_WhenProfileIdAppearsTwice_ReturnsMostRecentlyModifiedProfile()
    {
        string directory = CreateTestDirectory();
        DateTimeOffset older = new(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);
        DateTimeOffset newer = older.AddMinutes(30);

        try
        {
            await WriteProfileAsync(
                Path.Combine(directory, "old.json"),
                TestData.Profile(name: "Old", modifiedAtUtc: older));
            await WriteProfileAsync(
                Path.Combine(directory, "new.json"),
                TestData.Profile(name: "New", modifiedAtUtc: newer));

            ProfileLoadResult result = await CreateRepository(directory)
                .LoadAllAsync(CancellationToken.None);

            NetworkProfile profile = Assert.Single(result.Profiles);
            Assert.Equal(newer, profile.ModifiedAtUtc);
            Assert.Equal("New", profile.Name);
            Assert.Empty(result.Problems);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteAsync_WhenProfileExists_DeletesFileAndReturnsSuccess()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonProfileRepository repository = CreateRepository(directory);
            await repository.SaveAsync(TestData.Profile(), CancellationToken.None);

            ProfileDeleteResult result = await repository.DeleteAsync(
                "profile-1",
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Empty(Directory.GetFiles(directory, "*.json"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteAsync_WhenProfileDoesNotExist_ReturnsNotFoundWithoutThrowing()
    {
        string directory = CreateTestDirectory();

        try
        {
            ProfileDeleteResult result = await CreateRepository(directory).DeleteAsync(
                "missing",
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ProfileDeleteStatus.NotFound, result.Status);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsync_WhenProfileExists_WritesReadableProfileJson()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonProfileRepository repository = CreateRepository(directory);
            ProfileSaveResult saved = await repository.SaveAsync(
                TestData.Profile(),
                CancellationToken.None);
            await using MemoryStream destination = new();

            ProfileExportResult result = await repository.ExportAsync(
                "profile-1",
                destination,
                CancellationToken.None);
            destination.Position = 0;
            NetworkProfile exported = await new ProfileJsonCodec().DeserializeAsync(
                destination,
                CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(saved.Profile, exported);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsync_WhenProfileDoesNotExist_ReturnsNotFoundWithoutThrowing()
    {
        string directory = CreateTestDirectory();

        try
        {
            await using MemoryStream destination = new();

            ProfileExportResult result = await CreateRepository(directory).ExportAsync(
                "missing",
                destination,
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ProfileExportStatus.NotFound, result.Status);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportAsync_WhenStreamContainsValidProfile_SavesProfile()
    {
        string directory = CreateTestDirectory();

        try
        {
            JsonProfileRepository repository = CreateRepository(directory);
            await using MemoryStream source = new();
            await new ProfileJsonCodec().SerializeAsync(
                source,
                TestData.Profile(profileId: "imported-profile", name: "Imported"),
                CancellationToken.None);
            source.Position = 0;

            ProfileImportResult result = await repository.ImportAsync(
                source,
                CancellationToken.None);
            ProfileLoadResult loaded = await repository.LoadAllAsync(CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("imported-profile", result.Profile!.ProfileId);
            Assert.Equal(result.Profile, Assert.Single(loaded.Profiles));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportAsync_WhenStreamContainsMalformedJson_ReturnsInvalidContentWithoutThrowing()
    {
        string directory = CreateTestDirectory();

        try
        {
            await using MemoryStream source = new(Encoding.UTF8.GetBytes("{"));

            ProfileImportResult result = await CreateRepository(directory).ImportAsync(
                source,
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ProfileImportStatus.InvalidContent, result.Status);
            Assert.Empty(Directory.GetFiles(directory, "*.json"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_WhenProfilesPathIsAFile_ReturnsIoFailureWithoutThrowing()
    {
        string parent = CreateTestDirectory();
        string invalidDirectory = Path.Combine(parent, "not-a-directory");
        await File.WriteAllTextAsync(invalidDirectory, "occupied");

        try
        {
            ProfileSaveResult result = await CreateRepository(invalidDirectory).SaveAsync(
                TestData.Profile(),
                CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Equal(ProfileSaveStatus.IoFailure, result.Status);
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    private static JsonProfileRepository CreateRepository(string directory) =>
        new(new ProfileRepositoryOptions { ProfilesDirectory = directory });

    private static async Task WriteProfileAsync(string path, NetworkProfile profile)
    {
        await using FileStream stream = File.Create(path);
        await new ProfileJsonCodec().SerializeAsync(stream, profile, CancellationToken.None);
    }

    private static string CreateTestDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"IPMan-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
