using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using IPMan.App.Presentation;
using Xunit;

namespace IPMan.Tests.Presentation;

[Collection(WpfTestCollectionDefinition.Name)]
public sealed class MouseFocusBehaviorTests
{
    [Fact]
    public void PointerPress_DoesNotMoveKeyboardFocusOntoTheButtonAndKeepsTheTextBoxFocused()
    {
        RunWithPointerInput(true, (host, box, button) =>
        {
            Assert.True(Keyboard.Focus(box) == box && box.IsKeyboardFocused, "Test host could not focus the text box.");
            RaiseMouseDown(button);

            DisabledAppearanceTests.Pump(150);

            Assert.False(button.IsKeyboardFocused);
            Assert.True(box.IsKeyboardFocused);
            Assert.Equal(((SolidColorBrush)host.Resources["BorderBrush"]).Color, ((SolidColorBrush)Chrome(button).BorderBrush).Color);
        });
    }

    [Fact]
    public void KeyboardFocus_StillFocusesTheButtonAndShowsTheFocusRing()
    {
        RunWithPointerInput(false, (host, box, button) =>
        {
            Assert.True(Keyboard.Focus(box) == box && box.IsKeyboardFocused, "Test host could not focus the text box.");
            Keyboard.Focus(button);
            DisabledAppearanceTests.Pump(150);

            Assert.True(button.IsKeyboardFocused);
            Assert.False(box.IsKeyboardFocused);
            Assert.Equal(((SolidColorBrush)host.Resources["FocusBrush"]).Color, ((SolidColorBrush)Chrome(button).BorderBrush).Color);
        });
    }

    [Fact]
    public void ClickAfterKeyboardNavigation_UsesFocusFromTheKeyboardOnly()
    {
        RunWithPointerInput(false, (host, box, button) =>
        {
            Keyboard.Focus(button);
            Assert.True(button.IsKeyboardFocused);
            MouseFocusBehavior.IsPointerInputActive = () => true;
            Keyboard.Focus(box);
            Assert.True(box.IsKeyboardFocused);
            RaiseMouseDown(button);
            Assert.False(button.IsKeyboardFocused);
            Assert.True(box.IsKeyboardFocused);
        });
    }

    [Fact]
    public void AllowPointerFocus_OptsASingleButtonBackIn()
    {
        RunWithPointerInput(true, (host, box, button) =>
        {
            MouseFocusBehavior.SetAllowPointerFocus(button, true);
            Keyboard.Focus(box);
            RaiseMouseDown(button);

            Assert.True(button.IsKeyboardFocused);
        });
    }

    [Theory]
    [InlineData(typeof(Button))]
    [InlineData(typeof(ToggleButton))]
    [InlineData(typeof(RadioButton))]
    [InlineData(typeof(CheckBox))]
    public void PointerPress_IsVetoedForEveryButtonBaseTypeAndKeyboardIsNot(Type type)
    {
        WpfTestRunner.Run(() =>
        {
            MouseFocusBehavior.Install();
            Func<bool> previous = MouseFocusBehavior.IsPointerInputActive;
            try
            {
                StackPanel host = new();
                host.Resources.MergedDictionaries.Add(Load("Light"));
                host.Resources.MergedDictionaries.Add(Load("Controls"));
                TextBox box = new();
                ButtonBase target = (ButtonBase)Activator.CreateInstance(type)!;
                host.Children.Add(box);
                host.Children.Add(target);
                using HwndSourceHolder source = new(host);
                Keyboard.Focus(box);
                Assert.True(box.IsKeyboardFocused, "Test host could not focus the text box.");

                MouseFocusBehavior.IsPointerInputActive = () => true;
                Keyboard.Focus(target);
                Assert.False(target.IsKeyboardFocused);
                Assert.True(box.IsKeyboardFocused);

                MouseFocusBehavior.IsPointerInputActive = () => false;
                Keyboard.Focus(target);
                Assert.True(target.IsKeyboardFocused);
            }
            finally
            {
                MouseFocusBehavior.IsPointerInputActive = previous;
            }
        }, "Pointer focus veto timed out.");
    }

    private static void RunWithPointerInput(bool pointer, Action<FrameworkElement, TextBox, Button> body) =>
        WpfTestRunner.Run(() =>
        {
            MouseFocusBehavior.Install();
            Func<bool> previous = MouseFocusBehavior.IsPointerInputActive;
            try
            {
                StackPanel host = new();
                host.Resources.MergedDictionaries.Add(Load("Light"));
                host.Resources.MergedDictionaries.Add(Load("Controls"));
                DisabledAppearanceTests.ShortenMotion(host);
                TextBox box = new() { Text = "192.168.10.50" };
                Button button = new() { Content = "Uygula" };
                host.Children.Add(box);
                host.Children.Add(button);
                using HwndSourceHolder source = new(host);
                DisabledAppearanceTests.Pump(50);
                MouseFocusBehavior.IsPointerInputActive = () => pointer;
                body(host, box, button);
            }
            finally
            {
                MouseFocusBehavior.IsPointerInputActive = previous;
            }
        }, "Mouse focus behavior test timed out.");

    private static Border Chrome(Button button) => (Border)button.Template.FindName("Chrome", button);

    private static void RaiseMouseDown(UIElement target) =>
        target.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent,
            Source = target
        });

    private static ResourceDictionary Load(string name) => (ResourceDictionary)System.Windows.Application.LoadComponent(
        new Uri($"/IPMan;component/Themes/{name}.xaml", UriKind.Relative));

    private sealed class HwndSourceHolder : IDisposable
    {
        private readonly HwndSource _source;

        internal HwndSourceHolder(Visual root) => _source = DisabledAppearanceTests.Show(root);

        public void Dispose() => _source.Dispose();
    }
}
