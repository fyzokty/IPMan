using System.Windows;

namespace IPMan.App.Presentation;

/// <summary>Shows network-state confirmation prompts through the WPF message box.</summary>
public sealed class WpfUserConfirmationService : IUserConfirmationService
{
    /// <inheritdoc />
    public bool Confirm(UserConfirmationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return MessageBox.Show(
            request.Message,
            request.Title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }
}
