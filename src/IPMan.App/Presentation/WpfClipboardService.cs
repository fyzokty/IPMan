using System.Windows;

namespace IPMan.App.Presentation;

public sealed class WpfClipboardService : IClipboardService
{
    public bool SetText(string text)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
            {
                Clipboard.Clear();
                return true;
            }

            Clipboard.SetText(text);
            return true;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
