namespace IPMan.App.Presentation;

/// <summary>Describes a confirmation prompt shown before changing Windows network state.</summary>
/// <param name="Title">The localized prompt title.</param>
/// <param name="Message">The localized prompt message.</param>
public sealed record UserConfirmationRequest(string Title, string Message);
