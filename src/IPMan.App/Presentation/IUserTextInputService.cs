namespace IPMan.App.Presentation;

/// <summary>Requests a short text value from the user.</summary>
public interface IUserTextInputService
{
    /// <summary>Returns the entered value, or null when the user cancels.</summary>
    string? Request(string title, string label, string initialValue);
}
