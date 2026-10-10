using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Common;
using IPMan.Application.Diagnostics;
using IPMan.Application.Profiles;

namespace IPMan.App.ViewModels;

/// <summary>Collects independently readable diagnostics and copies the displayed report.</summary>
public sealed partial class DiagnosticsViewModel : ObservableObject
{
    private readonly ISystemDiagnosticsProvider _system;
    private readonly IApplicationVersionProvider _version;
    private readonly IElevationStateProvider _elevation;
    private readonly Func<int> _adapterCount;
    private readonly IProfileCatalog _profiles;
    private readonly IClock _clock;
    private readonly IClipboardService _clipboard;

    [ObservableProperty]
    private string _reportText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<DiagnosticsReportRow> _reportRows = Array.Empty<DiagnosticsReportRow>();

    [ObservableProperty]
    private string _copyStatus = string.Empty;

    public DiagnosticsViewModel(
        ISystemDiagnosticsProvider system,
        IApplicationVersionProvider version,
        IElevationStateProvider elevation,
        Func<int> adapterCount,
        IProfileCatalog profiles,
        IClock clock,
        IClipboardService clipboard)
    {
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(elevation);
        ArgumentNullException.ThrowIfNull(adapterCount);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(clipboard);
        _system = system;
        _version = version;
        _elevation = elevation;
        _adapterCount = adapterCount;
        _profiles = profiles;
        _clock = clock;
        _clipboard = clipboard;
    }

    [RelayCommand]
    private void Refresh()
    {
        ReportRows = [
            ReadField(Strings.DiagnosticsWindowsVersion, () =>
            {
                WindowsVersionInfo windows = _system.ReadWindowsVersion();
                return Strings.FormatDiagnosticsWindowsVersion(
                    windows.ProductName, windows.DisplayVersion, windows.BuildNumber, windows.Architecture);
            }),
            ReadField(Strings.DiagnosticsApplicationVersion, () => _version.InformationalVersion),
            ReadField(Strings.DiagnosticsAdministrator, () => _elevation.IsElevated
                ? Strings.DiagnosticsAdministratorYes : Strings.DiagnosticsAdministratorNo),
            ReadField(Strings.DiagnosticsAdapterCount, () => _adapterCount().ToString(CultureInfo.CurrentCulture)),
            ReadField(Strings.DiagnosticsProfilesDirectory, () => _system.ProfilesDirectory),
            ReadField(Strings.DiagnosticsProfilesDirectoryStatus, () => _system.ReadProfileDirectoryStatus() switch
            {
                ProfileDirectoryStatus.Exists => Strings.DiagnosticsDirectoryExists,
                ProfileDirectoryStatus.NotCreated => Strings.DiagnosticsDirectoryNotCreated,
                ProfileDirectoryStatus.Inaccessible => Strings.DiagnosticsDirectoryInaccessible,
                _ => Strings.DiagnosticsUnreadable
            }),
            ReadField(Strings.DiagnosticsProfileCount, () => _profiles.Profiles.Count.ToString(CultureInfo.CurrentCulture)),
            ReadField(Strings.DiagnosticsProblemCount, () => _profiles.Problems.Count.ToString(CultureInfo.CurrentCulture)),
            ReadField(Strings.DiagnosticsCriticalLog, () => _system.CriticalLogExists()
                ? _system.CriticalLogFile : Strings.FormatDiagnosticsMissingLog(_system.CriticalLogFile)),
            ReadField(Strings.DiagnosticsCreatedAt, () => _clock.UtcNow.ToLocalTime().ToString("G", CultureInfo.CurrentCulture))];
        ReportText = string.Join(Environment.NewLine, ReportRows.Select(row => $"{row.Name}: {row.Value}"));
        CopyStatus = string.Empty;
    }

    [RelayCommand]
    private void Copy()
    {
        try
        {
            CopyStatus = _clipboard.SetText(ReportText)
                ? Strings.DiagnosticsCopySucceeded : Strings.DiagnosticsCopyFailed;
        }
        catch (Exception)
        {
            // Clipboard availability must never affect the diagnostics window.
            CopyStatus = Strings.DiagnosticsCopyFailed;
        }
    }

    private static DiagnosticsReportRow ReadField(string name, Func<string> read)
    {
        string value;
        try
        {
            value = read();
        }
        catch (Exception)
        {
            // Each source is isolated so the rest of the report remains available.
            value = Strings.DiagnosticsUnreadable;
        }

        return new DiagnosticsReportRow(name, value);
    }
}

/// <summary>A display row from the same independently collected diagnostics as the plain text report.</summary>
public sealed record DiagnosticsReportRow(string Name, string Value);
