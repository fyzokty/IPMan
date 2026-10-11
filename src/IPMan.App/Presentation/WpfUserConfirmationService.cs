using System.Windows;
using System.Windows.Controls;
using IPMan.App.Resources;
using IPMan.App.Views;

namespace IPMan.App.Presentation;

/// <summary>Shows network-state confirmation prompts in a themed modal WPF window.</summary>
public sealed class WpfUserConfirmationService : IUserConfirmationService
{
    /// <inheritdoc />
    public bool Confirm(UserConfirmationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShowPrompt(request).Accepted;
    }

    /// <inheritdoc />
    public (bool Accepted, bool DoNotShowAgain) ConfirmWithOptions(UserConfirmationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShowPrompt(request);
    }

    private static (bool Accepted, bool DoNotShowAgain) ShowPrompt(UserConfirmationRequest request)
    {
        PromptWindow window = CreatePrompt(request, out CheckBox doNotShowAgain);
        bool accepted = window.ShowDialog() == true;
        return (accepted, accepted && request.ShowDoNotShowAgain && doNotShowAgain.IsChecked == true);
    }

    internal static PromptWindow CreatePrompt(UserConfirmationRequest request, out CheckBox doNotShowAgain)
    {
        bool trayPreference = request.Title == Strings.TrayPreferenceTitle;
        PromptWindow window = new(request.Title, trayPreference ? "\uE946" : "\uE7BA", trayPreference);
        DialogWindowPlacement.Prepare(window, System.Windows.Application.Current?.MainWindow);
        TextBlock title = new() { Text = request.Title, FontSize = 20, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 16) };
        window.Body.Children.Add(title);
        TextBlock message = new() { Text = request.Message, TextWrapping = TextWrapping.Wrap };
        message.SetResourceReference(TextBlock.ForegroundProperty, "SecondaryTextBrush");
        window.Body.Children.Add(message);
        doNotShowAgain = new CheckBox
        {
            Content = Strings.ConfirmDoNotShowAgain,
            Margin = new Thickness(0, 20, 0, 0),
            Visibility = request.ShowDoNotShowAgain ? Visibility.Visible : Visibility.Collapsed
        };
        window.Body.Children.Add(doNotShowAgain);
        window.AddButton(Strings.CommandCancel, false);
        window.AddButton(Strings.CommandOk, true);
        return window;
    }
}
