using System.Windows;
using System.Windows.Controls;
using IPMan.App.Resources;

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

        TextBox input = new() { Text = initialValue, MinWidth = 260 };
        Window window = new()
        {
            Title = title,
            Width = 360,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Owner = System.Windows.Application.Current?.MainWindow,
            Content = CreateContent(label, input)
        };

        return window.ShowDialog() == true ? input.Text : null;
    }

    private static StackPanel CreateContent(string label, TextBox input)
    {
        StackPanel panel = new() { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = label });
        panel.Children.Add(input);

        Button accept = new() { Content = Strings.CommandOk, IsDefault = true, MinWidth = 75 };
        accept.Click += (_, _) => Window.GetWindow(accept)!.DialogResult = true;
        Button cancel = new() { Content = Strings.CommandCancel, IsCancel = true, MinWidth = 75 };

        StackPanel buttons = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        buttons.Children.Add(accept);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        return panel;
    }
}
