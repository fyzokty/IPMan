using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using IPMan.App.ViewModels;

namespace IPMan.App.Presentation;

/// <summary>Observes completed copy commands without replacing commands or their bindings.</summary>
public static class CopyFeedback
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(CopyFeedback), new PropertyMetadata(false, OnEnabledChanged));
    public static readonly DependencyProperty IconOnlyProperty = DependencyProperty.RegisterAttached(
        "IconOnly", typeof(bool), typeof(CopyFeedback), new PropertyMetadata(false));
    public static readonly DependencyProperty ResultProperty = DependencyProperty.RegisterAttached(
        "Result", typeof(object), typeof(CopyFeedback), new PropertyMetadata(null));
    public static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State", typeof(string), typeof(CopyFeedback), new PropertyMetadata("Idle"));
    private static readonly DependencyProperty TimerProperty = DependencyProperty.RegisterAttached(
        "Timer", typeof(DispatcherTimer), typeof(CopyFeedback));
    private static readonly DependencyProperty VersionProperty = DependencyProperty.RegisterAttached(
        "Version", typeof(int), typeof(CopyFeedback), new PropertyMetadata(0));

    public static bool GetIsEnabled(DependencyObject value) => (bool)value.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject value, bool enabled) => value.SetValue(IsEnabledProperty, enabled);
    public static bool GetIconOnly(DependencyObject value) => (bool)value.GetValue(IconOnlyProperty);
    public static void SetIconOnly(DependencyObject value, bool enabled) => value.SetValue(IconOnlyProperty, enabled);
    public static object GetResult(DependencyObject value) => value.GetValue(ResultProperty);
    public static void SetResult(DependencyObject value, object result) => value.SetValue(ResultProperty, result);
    public static string GetState(DependencyObject value) => (string)value.GetValue(StateProperty);
    public static void SetState(DependencyObject value, string state) => value.SetValue(StateProperty, state);

    private static void OnEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is Button button)
        {
            button.Click -= OnClick;
            button.Unloaded -= OnUnloaded;
            if ((bool)args.NewValue)
            {
                button.Click += OnClick;
                button.Unloaded += OnUnloaded;
            }
            else
            {
                Reset(button);
            }
        }
    }

    private static void OnClick(object sender, RoutedEventArgs args)
    {
        Button button = (Button)sender;
        int version = (int)button.GetValue(VersionProperty) + 1;
        button.SetValue(VersionProperty, version);
        // Button raises Click before executing ICommand; evaluate after that same dispatch completes.
        _ = button.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, () =>
        {
            if (version != (int)button.GetValue(VersionProperty) || !GetIsEnabled(button))
            {
                return;
            }

            BindingOperations.GetBindingExpression(button, ResultProperty)?.UpdateTarget();
            object result = GetResult(button);
            if (result is bool or ApplyStatusSeverity)
            {
                Show(button, result is true or ApplyStatusSeverity.Success);
            }
        });
    }

    internal static void Show(Button button, bool succeeded)
    {
        Reset(button);
        SetState(button, succeeded ? "Success" : "Error");
        if (!succeeded && FluentMotion.Duration(button) != TimeSpan.Zero &&
            button.Template.FindName("Chrome", button) is FrameworkElement chrome)
        {
            TranslateTransform translate = FluentMotion.Translation(chrome);
            DoubleAnimationUsingKeyFrames shake = new() { Duration = FluentMotion.Duration(chrome, "Motion.Slow"), FillBehavior = FillBehavior.Stop };
            foreach ((double time, double offset) in new[] { (0d, 0d), (.2, -3d), (.4, 3d), (.6, -2d), (.8, 2d), (1d, 0d) })
            {
                shake.KeyFrames.Add(new LinearDoubleKeyFrame(offset, KeyTime.FromPercent(time)));
            }

            translate.BeginAnimation(TranslateTransform.XProperty, shake);
        }

        TimeSpan hold = button.TryFindResource("Motion.CopyHold") as TimeSpan? ?? TimeSpan.FromSeconds(1.5);
        DispatcherTimer timer = new(DispatcherPriority.Normal, button.Dispatcher) { Interval = hold };
        timer.Tick += (_, _) => Reset(button);
        button.SetValue(TimerProperty, timer);
        timer.Start();
    }

    private static void OnUnloaded(object sender, RoutedEventArgs args) => Reset((Button)sender);

    internal static void Reset(Button button)
    {
        button.SetValue(VersionProperty, (int)button.GetValue(VersionProperty) + 1);
        (button.GetValue(TimerProperty) as DispatcherTimer)?.Stop();
        button.ClearValue(TimerProperty);
        if (button.Template?.FindName("Chrome", button) is FrameworkElement { RenderTransform: TransformGroup transforms } &&
            transforms.Children.LastOrDefault() is TranslateTransform { IsFrozen: false } translate)
        {
            translate.BeginAnimation(TranslateTransform.XProperty, null);
        }

        SetState(button, "Idle");
    }
}
