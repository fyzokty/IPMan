using IPMan.Application.Logging;

namespace IPMan.Infrastructure.Logging;

/// <summary>Uses a local file to identify sessions that did not close cleanly.</summary>
public sealed class FileSessionMarker : ISessionMarker
{
    private readonly string _path;

    /// <summary>Initializes a marker at the supplied path.</summary>
    public FileSessionMarker(string path) => _path = Path.GetFullPath(path);

    /// <inheritdoc />
    public bool TryConsumeStale()
    {
        try { if (!File.Exists(_path)) return false; File.Delete(_path); return true; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    /// <inheritdoc />
    public void Create()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(_path)!); File.WriteAllText(_path, $"{DateTimeOffset.Now:O}|{Environment.ProcessId}"); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <inheritdoc />
    public void Delete()
    {
        try { if (File.Exists(_path)) File.Delete(_path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
