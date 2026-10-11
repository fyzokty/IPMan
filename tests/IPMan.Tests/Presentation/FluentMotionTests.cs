using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using IPMan.App.Presentation;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Presentation;

[Collection(WpfTestCollectionDefinition.Name)]
public sealed class FluentMotionTests
{
    [Fact]
    public void SelectionAndFeedback_KeepMeasuredGeometryAndCommands()
    {
        WpfTestRunner.Run(() =>
        {
            StackPanel host = Host();
            FluentMotion.SetIsEnabled(host, false);
            TestButton button = new() { Content = "Copy", Style = (Style)host.Resources["CopyButtonStyle"] };
            TabItem first = new() { Header = "Ethernet", Content = "One" };
            TabItem second = new() { Header = "Ethernet", Content = "Two" };
            TabControl tabs = new() { Items = { first, second } };
            ListBoxItem row = new() { Content = "Profile" };
            ListBox list = new() { Items = { row } };
            Expander expander = new() { Header = "Problems", Content = new TextBlock { Text = "Details" } };
            host.Children.Add(button);
            host.Children.Add(tabs);
            host.Children.Add(list);
            host.Children.Add(expander);
            Layout(host);
            Size buttonSize = button.DesiredSize;
            Size firstSize = first.DesiredSize;
            Size rowSize = row.DesiredSize;
            Size expanderSize = expander.DesiredSize;

            first.IsSelected = false;
            second.IsSelected = true;
            row.IsSelected = true;
            expander.IsExpanded = true;
            CopyFeedback.Show(button, true);
            Layout(host);
            Assert.Equal(buttonSize, button.DesiredSize);
            Assert.Equal(firstSize, first.DesiredSize);
            Assert.Equal(rowSize, row.DesiredSize);
            Assert.Equal(expanderSize, expander.DesiredSize);
            Assert.Equal(FontWeights.Normal, first.FontWeight);
            Assert.Equal(first.FontWeight, second.FontWeight);
            Border chrome = (Border)button.Template.FindName("Chrome", button);
            FluentMotion.SetPressed(chrome, true);
            Layout(host);
            Assert.Equal(buttonSize, button.DesiredSize);
            Assert.Equal(new Thickness(1), chrome.BorderThickness);
            Assert.Equal(.97, ((ScaleTransform)((TransformGroup)chrome.RenderTransform).Children[0]).ScaleX);
            FluentMotion.SetPressed(chrome, false);
            Assert.Equal(1, ((ScaleTransform)((TransformGroup)chrome.RenderTransform).Children[0]).ScaleX);
            CopyFeedback.Reset(button);
        }, "Stable control geometry timed out.");
    }

    [Fact]
    public void EntranceAndDisableSwitch_StartAndStopStoryboardsWithoutNativeWindows()
    {
        WpfTestRunner.Run(() =>
        {
            StackPanel host = Host();
            Border content = new() { Background = Brushes.White };
            content.SetBinding(UIElement.OpacityProperty,
                new Binding { Path = new PropertyPath(FluentMotion.VisualOpacityProperty), Source = content });
            host.Children.Add(content);
            FluentMotion.Enter(content);
            Assert.Equal(SystemParameters.ClientAreaAnimation, content.HasAnimatedProperties);
            FluentMotion.SetIsEnabled(host, false);
            Assert.False(content.HasAnimatedProperties);
            Assert.False(content.RenderTransform.HasAnimatedProperties);
            FluentMotion.Enter(content);
            Assert.Equal(1, content.Opacity);
            Assert.False(content.HasAnimatedProperties);
            Assert.Equal(TimeSpan.Zero, FluentMotion.Duration(content));
            FluentMotion.SetReveal(content, false);
            Assert.Equal(0, content.Opacity);
            FluentMotion.SetReveal(content, true);
            Assert.Equal(1, content.Opacity);
            Assert.Null(content.LayoutTransform as TransformGroup);
        }, "Motion accessibility switch timed out.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CopyClick_ObservesCommandResultAndOnlyAnimatesClickedButton(bool succeeds)
    {
        WpfTestRunner.Run(() =>
        {
            FakeClipboardService clipboard = new() { Succeeds = succeeds };
            AdapterViewModel adapter = new(TestData.Snapshot(), clipboard, new StaticIpv4ConfigurationValidator());
            StackPanel host = Host();
            FluentMotion.SetIsEnabled(host, false);
            TestButton clicked = CopyButton(host, adapter);
            TestButton other = CopyButton(host, adapter);
            Layout(host);
            object command = clicked.Command;
            clicked.Press();
            DrainDispatcher();
            Assert.Equal(succeeds ? "Success" : "Error", CopyFeedback.GetState(clicked));
            Assert.Equal("Idle", CopyFeedback.GetState(other));
            Assert.Same(command, clicked.Command);
            Assert.Single(clipboard.CopiedTexts);
            TextBlock check = (TextBlock)clicked.Template.FindName("SuccessGlyph", clicked);
            Assert.Equal(succeeds ? 1 : 0, check.Opacity);

            clipboard.Succeeds = !succeeds;
            clicked.Press();
            DrainDispatcher();
            Assert.Equal(succeeds ? "Error" : "Success", CopyFeedback.GetState(clicked));
            Assert.Equal(2, clipboard.CopiedTexts.Count);
            clicked.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, clicked));
            Assert.Equal("Idle", CopyFeedback.GetState(clicked));
            Assert.Equal(0, check.Opacity);
        }, "Copy result feedback timed out.");
    }

    [Fact]
    public void CopyFeedback_UsesConfiguredHoldAndResetsThroughDispatcher()
    {
        WpfTestRunner.Run(() =>
        {
            StackPanel host = Host();
            Assert.Equal(TimeSpan.FromSeconds(1.5), host.Resources["Motion.CopyHold"]);
            FluentMotion.SetIsEnabled(host, false);
            TestButton button = new() { Style = (Style)host.Resources["CopyButtonStyle"] };
            host.Children.Add(button);
            Layout(host);
            button.Resources["Motion.CopyHold"] = TimeSpan.Zero;
            CopyFeedback.Show(button, true);
            Assert.Equal("Success", CopyFeedback.GetState(button));
            DrainDispatcher();
            Assert.Equal("Idle", CopyFeedback.GetState(button));
        }, "Copy feedback reset timed out.");
    }

    [Fact]
    public void CopyClick_WhenUnloadedBeforeCommandFeedbackDispatch_DiscardsPendingFeedback()
    {
        WpfTestRunner.Run(() =>
        {
            StackPanel host = Host();
            AdapterViewModel adapter = new(TestData.Snapshot(), new FakeClipboardService(), new StaticIpv4ConfigurationValidator());
            TestButton button = CopyButton(host, adapter);
            Layout(host);
            button.Press();
            button.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, button));
            DrainDispatcher();
            Assert.Equal("Idle", CopyFeedback.GetState(button));
        }, "Unloaded copy feedback cancellation timed out.");
    }

    [Fact]
    public void ErrorShake_UsesPrivateTransformsAndObeysCentralDisableSwitch()
    {
        WpfTestRunner.Run(() =>
        {
            StackPanel host = Host();
            TestButton button = new() { Style = (Style)host.Resources["CopyButtonStyle"] };
            host.Children.Add(button);
            Layout(host);
            CopyFeedback.Show(button, false);
            Border chrome = (Border)button.Template.FindName("Chrome", button);
            TranslateTransform translation = FluentMotion.Translation(chrome);
            Assert.False(translation.IsFrozen);
            Assert.Equal(SystemParameters.ClientAreaAnimation, translation.HasAnimatedProperties);
            FluentMotion.SetIsEnabled(host, false);
            Assert.False(translation.HasAnimatedProperties);
            Assert.Equal(0, translation.X);
            CopyFeedback.Reset(button);
        }, "Private copy error transforms timed out.");
    }

    [Fact]
    public void PrivateAnimatedBrushes_FollowThemeReplacementWithoutMutatingResources()
    {
        WpfTestRunner.Run(() =>
        {
            StackPanel host = Host();
            FluentMotion.SetIsEnabled(host, false);
            Button button = new() { Content = "Apply", Style = (Style)host.Resources["PrimaryActionButtonStyle"] };
            host.Children.Add(button);
            Layout(host);
            Border chrome = (Border)button.Template.FindName("Chrome", button);
            Assert.NotSame(button.Background, chrome.Background);
            Assert.False(chrome.Background.IsFrozen);
            Assert.Equal(((SolidColorBrush)button.Background).Color, ((SolidColorBrush)chrome.Background).Color);
            host.Resources.MergedDictionaries[0] = Load("Dark");
            Layout(host);
            Assert.Equal(Color.FromRgb(76, 194, 255), ((SolidColorBrush)chrome.Background).Color);
            Assert.Equal(((SolidColorBrush)button.BorderBrush).Color, ((SolidColorBrush)chrome.BorderBrush).Color);
            CopyFeedback.SetIsEnabled(button, true);
            CopyFeedback.Show(button, false);
            CopyFeedback.Reset(button);
        }, "Private theme brush replacement timed out.");
    }

    [Fact]
    public void Motion_WithOpacityBindingAndNativeBrushTrigger_RestoresDeclarativeValues()
    {
        WpfTestRunner.Run(() =>
        {
            Assert.True(SystemParameters.ClientAreaAnimation);
            StackPanel host = Host();
            DisabledAppearanceTests.ShortenMotion(host);
            Border content = new() { Width = 100, Height = 32 };
            Style style = new(typeof(Border));
            style.Setters.Add(new Setter(UIElement.OpacityProperty, new Binding("Tag") { RelativeSource = RelativeSource.Self }));
            style.Setters.Add(new Setter(Border.BackgroundProperty, new MotionBinding(FluentMotion.VisualBackgroundProperty)));
            style.Setters.Add(new Setter(Border.BorderBrushProperty, new MotionBinding(FluentMotion.VisualBorderBrushProperty)));
            Trigger disabled = new() { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Gray));
            disabled.Setters.Add(new Setter(Border.BorderBrushProperty, Brushes.DimGray));
            style.Triggers.Add(disabled);
            content.Tag = .45d;
            content.Style = style;
            host.Children.Add(content);
            using System.Windows.Interop.HwndSource source = DisabledAppearanceTests.Show(host);
            DisabledAppearanceTests.Pump(DisabledAppearanceTests.Settle);

            FluentMotion.SetBackground(content, Brushes.Red);
            FluentMotion.SetBorderBrush(content, Brushes.Blue);
            FluentMotion.Enter(content);
            FluentMotion.SetBusy(content, true);
            Assert.True(content.HasAnimatedProperties);
            content.IsEnabled = false;
            FluentMotion.SetBackground(content, Brushes.Green);
            FluentMotion.SetBorderBrush(content, Brushes.Yellow);
            content.Tag = .3d;
            FluentMotion.SetBusy(content, false);
            FluentMotion.Enter(content);
            DisabledAppearanceTests.Pump(DisabledAppearanceTests.Settle);
            Assert.Equal(.3, content.Opacity);
            Assert.Equal(.3, content.GetAnimationBaseValue(UIElement.OpacityProperty));
            Assert.NotNull(BindingOperations.GetBindingExpression(content, UIElement.OpacityProperty));
            Assert.Equal(Colors.Gray, ((SolidColorBrush)content.Background).Color);
            Assert.Equal(Colors.DimGray, ((SolidColorBrush)content.BorderBrush).Color);

            content.IsEnabled = true;
            Assert.Equal(Colors.Green, ((SolidColorBrush)content.Background).Color);
            Assert.Equal(Colors.Yellow, ((SolidColorBrush)content.BorderBrush).Color);
            FluentMotion.SetBusy(content, true);
            FluentMotion.SetIsEnabled(content, false);
            Assert.Equal(.3, content.Opacity);
            Assert.False(content.HasAnimatedProperties);
        }, "Motion declarative values timed out.");
    }

    [Fact]
    public void CopyAndReveal_WithMotionOn_SettleOnLatestTemplateState()
    {
        WpfTestRunner.Run(() =>
        {
            Assert.True(SystemParameters.ClientAreaAnimation);
            StackPanel host = Host();
            DisabledAppearanceTests.ShortenMotion(host);
            Button copy = new() { Style = (Style)host.Resources["CopyButtonStyle"] };
            CheckBox check = new() { Content = "Check", IsChecked = true };
            host.Children.Add(copy);
            host.Children.Add(check);
            using System.Windows.Interop.HwndSource source = DisabledAppearanceTests.Show(host);
            DisabledAppearanceTests.Pump(DisabledAppearanceTests.Settle);
            TextBlock success = (TextBlock)copy.Template.FindName("SuccessGlyph", copy);
            TextBlock mark = (TextBlock)check.Template.FindName("Mark", check);
            CopyFeedback.Show(copy, true);
            check.IsChecked = false;
            DisabledAppearanceTests.Pump(5);
            CopyFeedback.Reset(copy);
            check.IsChecked = true;
            DisabledAppearanceTests.Pump(5);
            CopyFeedback.Show(copy, true);
            check.IsChecked = false;
            FluentMotion.Enter(mark);
            DisabledAppearanceTests.Pump(DisabledAppearanceTests.Settle);
            Assert.Equal(1, success.Opacity);
            Assert.Equal(0, mark.Opacity);
            CopyFeedback.Reset(copy);
            DisabledAppearanceTests.Pump(DisabledAppearanceTests.Settle);
            Assert.Equal(0, success.Opacity);
        }, "Copy/reveal latest state timed out.");
    }

    private static TestButton CopyButton(StackPanel host, AdapterViewModel adapter)
    {
        TestButton button = new()
        {
            Style = (Style)host.Resources["CopyButtonStyle"],
            Command = adapter.CopyCurrentFieldCommand,
            CommandParameter = DraftField.Ipv4Address
        };
        button.SetBinding(CopyFeedback.ResultProperty, new Binding(nameof(adapter.CopySucceeded)) { Source = adapter });
        host.Children.Add(button);
        return button;
    }

    private static StackPanel Host()
    {
        StackPanel host = new();
        host.Resources.MergedDictionaries.Add(Load("Light"));
        host.Resources.MergedDictionaries.Add(Load("Controls"));
        return host;
    }

    private static ResourceDictionary Load(string name) => (ResourceDictionary)System.Windows.Application.LoadComponent(
        new Uri($"/IPMan;component/Themes/{name}.xaml", UriKind.Relative));

    private static void Layout(FrameworkElement host)
    {
        host.Measure(new Size(800, 1000));
        host.Arrange(new Rect(0, 0, 800, 1000));
        host.UpdateLayout();
    }

    private static void DrainDispatcher()
    {
        DispatcherFrame frame = new();
        _ = Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
        Dispatcher.PushFrame(frame);
    }

    private sealed class TestButton : Button
    {
        internal void Press() => OnClick();
    }
}
