using System.Diagnostics;
using System.Reflection;

namespace IPMan.App.Presentation;

/// <summary>Supplies the application version shown in the status bar.</summary>
public interface IApplicationVersionProvider
{
    string Version { get; }
}

public sealed class AssemblyApplicationVersionProvider : IApplicationVersionProvider
{
    private readonly Lazy<string> _version = new(ReadVersion);

    public string Version => _version.Value;

    private static string ReadVersion()
    {
        Assembly assembly = typeof(AssemblyApplicationVersionProvider).Assembly;

        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            // Strip the source-revision suffix the SDK appends (e.g. "1.0.0+abc123").
            int metadataStart = informational.IndexOf('+', StringComparison.Ordinal);
            return metadataStart < 0 ? informational : informational[..metadataStart];
        }

        return FileVersionInfo
            .GetVersionInfo(assembly.Location)
            .FileVersion ?? assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
