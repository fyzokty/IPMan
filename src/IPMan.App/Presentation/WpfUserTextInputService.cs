using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using IPMan.App.Resources;
using IPMan.App.Views;

namespace IPMan.App.Presentation;

/// <summary>Shows a small modal WPF text prompt.</summary>
public sealed class WpfUserTextInputService : IUserTextInputService
{
    /// <inheritdoc />
    public string? Request(string title, string label, string initialValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(initialValue);

        PromptWindow window = CreatePrompt(title, label, initialValue, out TextBox input);
        return window.ShowDialog() == true ? input.Text : null;
    }

    internal static PromptWindow CreatePrompt(string title, string label, string initialValue, out TextBox input)
    {
        input = new TextBox { Text = initialValue, MinWidth = 260 };
        AutomationProperties.SetName(input, label);
        PromptWindow window = new(title, "\uE8AC", informational: true);
        window.Body.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        window.Body.Children.Add(input);
        window.AddButton(Strings.CommandCancel, false);
        window.AddButton(Strings.CommandOk, true);
        return window;
    }
}
