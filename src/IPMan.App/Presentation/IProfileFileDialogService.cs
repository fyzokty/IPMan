using System.IO;

namespace IPMan.App.Presentation;

/// <summary>Opens profile documents without exposing file paths to ViewModels.</summary>
public interface IProfileFileDialogService
{
    /// <summary>Lets the user select a profile document, or returns null when cancelled.</summary>
    Stream? OpenProfile();

    /// <summary>Lets the user choose an export destination, or returns null when cancelled.</summary>
    Stream? CreateProfile(string suggestedFileName);
}
