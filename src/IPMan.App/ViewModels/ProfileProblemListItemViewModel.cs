using System.IO;
using IPMan.App.Presentation;
using IPMan.Domain.Profiles;

namespace IPMan.App.ViewModels;

/// <summary>Presents an isolated unreadable profile document as information only.</summary>
public sealed class ProfileProblemListItemViewModel
{
    /// <summary>Creates an informational malformed-profile item.</summary>
    public ProfileProblemListItemViewModel(NetworkProfileProblem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);

        FileName = Path.GetFileName(problem.FilePath);
        Reason = ProfileResultMessageFormatter.Describe(problem.Kind);
    }

    public string FileName { get; }

    public string Reason { get; }
}
