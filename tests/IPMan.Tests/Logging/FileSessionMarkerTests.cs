using System;
using System.IO;
using IPMan.Infrastructure.Logging;
using Xunit;

namespace IPMan.Tests.Logging;

public sealed class FileSessionMarkerTests
{
    [Fact]
    public void CreateAndDelete_WhenMarkerIsAvailable_ManagesCurrentSessionMarker()
    {
        string directory = CreateTemporaryDirectory();
        string markerPath = Path.Combine(directory, "session.marker");
        try
        {
            FileSessionMarker marker = new(markerPath);

            marker.Create();
            marker.Delete();

            Assert.False(File.Exists(markerPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TryConsumeStale_WhenMarkerExists_DeletesItAndReturnsTrue()
    {
        string directory = CreateTemporaryDirectory();
        string markerPath = Path.Combine(directory, "session.marker");
        try
        {
            FileSessionMarker marker = new(markerPath);
            marker.Create();

            bool staleSessionDetected = marker.TryConsumeStale();

            Assert.True(staleSessionDetected);
            Assert.False(File.Exists(markerPath));
            Assert.False(marker.TryConsumeStale());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Operations_WhenParentPathIsAFile_DoNotThrow()
    {
        string directory = CreateTemporaryDirectory();
        string blockingFile = Path.Combine(directory, "not-a-directory");
        try
        {
            File.WriteAllText(blockingFile, "block");
            FileSessionMarker marker = new(Path.Combine(blockingFile, "session.marker"));

            marker.Create();
            marker.Delete();
            bool staleSessionDetected = marker.TryConsumeStale();

            Assert.False(staleSessionDetected);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IPMan.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
