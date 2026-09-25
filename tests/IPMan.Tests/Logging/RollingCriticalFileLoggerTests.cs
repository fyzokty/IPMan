using System;
using System.IO;
using System.Threading.Tasks;
using IPMan.Application.Logging;
using IPMan.Infrastructure.Logging;
using Xunit;

namespace IPMan.Tests.Logging;

public sealed class RollingCriticalFileLoggerTests
{
    private const long MaximumLogLength = 5 * 1024 * 1024;

    [Fact]
    public void Format_WhenEntryHasInnerException_IncludesAllDiagnosticFields()
    {
        Exception innerException = CreateException();
        CriticalLogEntry entry = new(
            CriticalLogCategory.NetworkApi,
            "Network request failed.",
            -42,
            new InvalidOperationException("Outer failure.", innerException));

        string formatted = RollingCriticalFileLogger.Format(entry, "1.2.3");

        Assert.Matches("^\\d{4}-\\d{2}-\\d{2}T", formatted);
        Assert.Contains("Version=1.2.3", formatted, StringComparison.Ordinal);
        Assert.Contains("Category=NetworkApi", formatted, StringComparison.Ordinal);
        Assert.Contains("Code=-42", formatted, StringComparison.Ordinal);
        Assert.Contains("Message=Network request failed.", formatted, StringComparison.Ordinal);
        Assert.Contains(typeof(InvalidOperationException).FullName!, formatted, StringComparison.Ordinal);
        Assert.Contains("Outer failure.", formatted, StringComparison.Ordinal);
        Assert.Contains(typeof(ArgumentException).FullName!, formatted, StringComparison.Ordinal);
        Assert.Contains("Inner failure.", formatted, StringComparison.Ordinal);
        Assert.Contains(nameof(CreateException), formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void Log_WhenFileReachesLimit_RotatesAndKeepsThreeArchives()
    {
        string directory = CreateTemporaryDirectory();
        string logPath = Path.Combine(directory, "ipman-critical.log");
        try
        {
            RollingCriticalFileLogger logger = new(logPath, "1.0.0");
            CriticalLogEntry entry = new(CriticalLogCategory.Json, "Invalid JSON.");

            for (int index = 0; index < 4; index++)
            {
                CreateMaximumSizedFile(logPath);
                logger.Log(entry);
            }

            Assert.True(File.Exists(logPath));
            Assert.True(File.Exists(logPath + ".1"));
            Assert.True(File.Exists(logPath + ".2"));
            Assert.True(File.Exists(logPath + ".3"));
            Assert.False(File.Exists(logPath + ".4"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Log_WhenDestinationCannotBeWritten_RaisesWriteFailedWithoutThrowing()
    {
        string directory = CreateTemporaryDirectory();
        string blockingFile = Path.Combine(directory, "not-a-directory");
        try
        {
            File.WriteAllText(blockingFile, "block");
            RollingCriticalFileLogger logger = new(
                Path.Combine(blockingFile, "ipman-critical.log"),
                "1.0.0");
            bool writeFailed = false;
            logger.WriteFailed += (_, _) => writeFailed = true;

            logger.Log(new CriticalLogEntry(CriticalLogCategory.Json, "Invalid JSON."));

            Assert.True(writeFailed);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Log_WhenCalledConcurrently_WritesEveryRecord()
    {
        string directory = CreateTemporaryDirectory();
        string logPath = Path.Combine(directory, "ipman-critical.log");
        const int recordCount = 32;
        try
        {
            RollingCriticalFileLogger logger = new(logPath, "1.0.0");

            Parallel.For(0, recordCount, _ =>
                logger.Log(new CriticalLogEntry(CriticalLogCategory.Unhandled, "Concurrent failure.")));

            string logContents = File.ReadAllText(logPath);
            int writtenRecordCount = logContents.Split("Message=Concurrent failure.", StringSplitOptions.None).Length - 1;
            Assert.Equal(recordCount, writtenRecordCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ArgumentException CreateException()
    {
        try
        {
            throw new ArgumentException("Inner failure.");
        }
        catch (ArgumentException exception)
        {
            return exception;
        }
    }

    private static void CreateMaximumSizedFile(string path)
    {
        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        stream.SetLength(MaximumLogLength);
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "IPMan.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
