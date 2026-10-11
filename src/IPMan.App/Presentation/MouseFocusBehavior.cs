using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace IPMan.App.Presentation;

/// <summary>
/// Keeps pointer clicks from moving keyboard focus onto buttons while leaving keyboard navigation untouched.
/// WPF focuses a <see cref="ButtonBase"/> on every mouse or touch press, which leaves a focus ring on the button
/// after a click and steals focus from a text box that is being edited. The behavior vetoes only the focus change
/// that a pointer causes (via <c>PreviewGotKeyboardFocus</c>); Tab, arrow keys, access keys, Enter/Space, default
/// and cancel buttons keep working, so the existing <c>IsKeyboardFocused</c> ring triggers only appear for keyboard focus.
/// </summary>
public static class MouseFocusBehavior
{
    private static readonly object InstallGate = new();
    private static bool installed;

    /// <summary>Opts a single button back in to receiving focus from pointer clicks.</summary>
    public static readonly DependencyProperty AllowPointerFocusProperty = DependencyProperty.RegisterAttached(
        "AllowPointerFocus", typeof(bool), typeof(MouseFocusBehavior), new PropertyMetadata(false));

    /// <summary>Test seam: reports whether the input that caused the current focus change came from a pointer device.</summary>
    internal static Func<bool> IsPointerInputActive { get; set; } = IsMostRecentInputPointer;

    /// <summary>Gets whether pointer clicks may focus the button.</summary>
    public static bool GetAllowPointerFocus(DependencyObject value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return (bool)value.GetValue(AllowPointerFocusProperty);
    }

    /// <summary>Sets whether pointer clicks may focus the button.</summary>
    public static void SetAllowPointerFocus(DependencyObject value, bool allow)
    {
        ArgumentNullException.ThrowIfNull(value);
        value.SetValue(AllowPointerFocusProperty, allow);
    }

    /// <summary>Registers the class-wide handler once for every <see cref="ButtonBase"/> (buttons, toggles, radios, check boxes).</summary>
    public static void Install()
    {
        lock (InstallGate)
        {
            if (installed)
            {
                return;
            }

            EventManager.RegisterClassHandler(
                typeof(ButtonBase), Keyboard.PreviewGotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnPreviewGotKeyboardFocus));
            installed = true;
        }
    }

    private static void OnPreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is ButtonBase button
            && ReferenceEquals(e.NewFocus, button)
            && !GetAllowPointerFocus(button)
            && IsPointerInputActive())
        {
            e.Handled = true;
        }
    }

    private static bool IsMostRecentInputPointer() =>
        InputManager.Current.MostRecentInputDevice is MouseDevice or StylusDevice or TouchDevice;
}
