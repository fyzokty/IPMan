using System.IO;
using IPMan.App.Resources;
using Microsoft.Win32;

namespace IPMan.App.Presentation;

/// <summary>WPF implementation of the profile import/export file prompts.</summary>
public sealed class WpfProfileFileDialogService : IProfileFileDialogService
{
    /// <inheritdoc />
    public Stream? OpenProfile()
    {
        OpenFileDialog dialog = new()
        {
            Title = Strings.ProfileImportDialogTitle,
            Filter = Strings.ProfileJsonFileFilter,
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true
            ? File.Open(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
    }

    /// <inheritdoc />
    public Stream? CreateProfile(string suggestedFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestedFileName);

        SaveFileDialog dialog = new()
        {
            Title = Strings.ProfileExportDialogTitle,
            Filter = Strings.ProfileJsonFileFilter,
            FileName = suggestedFileName,
            AddExtension = true,
            DefaultExt = ".json"
        };

        return dialog.ShowDialog() == true
            ? File.Open(dialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None)
            : null;
    }
}
