using System.Windows;

namespace IPMan.App.Presentation;

public sealed class WpfClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Clipboard.Clear();
            return;
        }

        Clipboard.SetText(text);
    }
}
