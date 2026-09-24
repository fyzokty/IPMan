using System.Windows;
using System.Windows.Controls;
using IPMan.App.Resources;

namespace IPMan.App.Presentation;

/// <summary>Shows network-state confirmation prompts through the WPF message box.</summary>
public sealed class WpfUserConfirmationService : IUserConfirmationService
{
    /// <inheritdoc />
    public bool Confirm(UserConfirmationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ShowDoNotShowAgain)
        {
            return ConfirmWithOptions(request).Accepted;
        }

        return MessageBox.Show(
            request.Message,
            request.Title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    /// <inheritdoc />
    public (bool Accepted, bool DoNotShowAgain) ConfirmWithOptions(UserConfirmationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.ShowDoNotShowAgain)
        {
            return (Confirm(request), false);
        }

        CheckBox doNotShowAgain = new()
        {
            Content = Strings.ConfirmDoNotShowAgain,
            Margin = new Thickness(0, 12, 0, 0)
        };
        Button accept = new()
        {
            Content = Strings.CommandOk,
            IsDefault = true,
            MinWidth = 80,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Button decline = new()
        {
            Content = Strings.CommandCancel,
            IsCancel = true,
            MinWidth = 80,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Window window = new()
        {
            Title = request.Title,
            Width = 460,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Owner = System.Windows.Application.Current?.MainWindow
        };
        StackPanel panel = new() { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = request.Message, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(doNotShowAgain);
        StackPanel buttons = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        buttons.Children.Add(decline);
        buttons.Children.Add(accept);
        panel.Children.Add(buttons);
        window.Content = panel;

        accept.Click += (_, _) => window.DialogResult = true;
        decline.Click += (_, _) => window.DialogResult = false;
        bool accepted = window.ShowDialog() == true;
        return (accepted, accepted && doNotShowAgain.IsChecked == true);
    }
}
