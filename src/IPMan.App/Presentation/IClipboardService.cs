namespace IPMan.App.Presentation;

/// <summary>
/// Clipboard access behind an abstraction so ViewModels stay testable and no
/// static OS clipboard call is scattered through presentation code.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Places <paramref name="text"/> on the clipboard. An empty value clears it
    /// rather than failing.
    /// </summary>
    bool SetText(string text);
}
