using System.Text;
using IPMan.Application.Logging;

namespace IPMan.Infrastructure.Logging;

/// <summary>Writes critical diagnostics to a bounded rolling per-user text file.</summary>
public sealed class RollingCriticalFileLogger : ICriticalLogger
{
    private const long MaximumLength = 5 * 1024 * 1024;
    private readonly object _gate = new();
    private readonly string _path;
    private readonly string _version;

    /// <summary>Initializes a logger with its destination and application version.</summary>
    public RollingCriticalFileLogger(string path, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        _path = Path.GetFullPath(path);
        _version = version;
    }

    /// <inheritdoc />
    public event EventHandler? WriteFailed;

    /// <inheritdoc />
    public void Log(CriticalLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        try
        {
            lock (_gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                RotateIfNeeded();
                File.AppendAllText(_path, Format(entry, _version), new UTF8Encoding(false));
            }
        }
        catch (IOException)
        {
            NotifyWriteFailed();
        }
        catch (UnauthorizedAccessException)
        {
            NotifyWriteFailed();
        }
    }

    /// <summary>Formats one diagnostic record.</summary>
    public static string Format(CriticalLogEntry entry, string version)
    {
        StringBuilder builder = new();
        builder.Append(DateTimeOffset.Now.ToString("O", System.Globalization.CultureInfo.InvariantCulture))
            .Append(" | Version=").Append(version)
            .Append(" | Category=").Append(entry.Category)
            .Append(" | Code=").Append(entry.ErrorCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none")
            .Append(" | Message=").AppendLine(entry.Message);
        for (Exception? exception = entry.Exception; exception is not null; exception = exception.InnerException)
        {
            builder.Append(exception.GetType().FullName).Append(": ").AppendLine(exception.Message);
            builder.AppendLine(exception.StackTrace);
        }
        return builder.ToString();
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_path) || new FileInfo(_path).Length < MaximumLength)
        {
            return;
        }

        string archive3 = _path + ".3";
        if (File.Exists(archive3))
        {
            File.Delete(archive3);
        }

        for (int index = 2; index >= 1; index--)
        {
            string source = _path + "." + index;
            if (File.Exists(source))
            {
                File.Move(source, _path + "." + (index + 1), overwrite: true);
            }
        }

        File.Move(_path, _path + ".1", overwrite: true);
    }

    private void NotifyWriteFailed()
    {
        try
        {
            WriteFailed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            // Diagnostics must not affect the caller when a notification subscriber fails.
        }
    }
}
