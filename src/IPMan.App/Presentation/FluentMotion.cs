using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace IPMan.App.Presentation;

/// <summary>Render-only transitions shared by templates, popups and code-built prompts.</summary>
public static class FluentMotion
{
    private static readonly object Gate = new();
    private static readonly List<WeakReference<FrameworkElement>> Owners = [];
    private static readonly ConditionalWeakTable<FrameworkElement, object> Tracked = new();

    internal static Func<bool> SystemAnimationsEnabled { get; set; } = () => SystemParameters.ClientAreaAnimation;

    static FluentMotion() => SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;

    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(FluentMotion), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, OnEnabledChanged));
    public static readonly DependencyProperty BackgroundProperty = Register("Background", typeof(Brush), null, OnBrushChanged);
    public static readonly DependencyProperty BorderBrushProperty = Register("BorderBrush", typeof(Brush), null, OnBrushChanged);
    public static readonly DependencyProperty OpacityProperty = Register("Opacity", typeof(double), 1d, OnOpacityChanged);
    public static readonly DependencyProperty RevealProperty = Register("Reveal", typeof(bool), true, OnRevealChanged);
    public static readonly DependencyProperty AxisProperty = Register("Axis", typeof(string), "Y", OnRevealChanged);
    public static readonly DependencyProperty PressedProperty = Register("Pressed", typeof(bool), false, OnPressedChanged);
    public static readonly DependencyProperty AngleProperty = Register("Angle", typeof(double), 0d, OnAngleChanged);
    public static readonly DependencyProperty EntranceProperty = Register("Entrance", typeof(bool), false, OnEntranceChanged);
    public static readonly DependencyProperty PopupProperty = Register("Popup", typeof(bool), false, OnPopupChanged);
    public static readonly DependencyProperty TransitionKeyProperty = Register("TransitionKey", typeof(object), null, OnContentChanged);
    public static readonly DependencyProperty PopProperty = Register("Pop", typeof(bool), false, OnPopChanged);
    public static readonly DependencyProperty PopOnClickProperty = Register("PopOnClick", typeof(bool), false, OnPopOnClickChanged);
    public static readonly DependencyProperty BusyProperty = Register("Busy", typeof(bool), false, OnBusyChanged);
    public static readonly DependencyProperty IsInvalidProperty = DependencyProperty.RegisterAttached(
        "IsInvalid", typeof(bool), typeof(FluentMotion), new PropertyMetadata(false));
    private static readonly DependencyPropertyKey VisualOpacityPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "VisualOpacity", typeof(double), typeof(FluentMotion), new PropertyMetadata(1d));
    public static readonly DependencyProperty VisualOpacityProperty = VisualOpacityPropertyKey.DependencyProperty;
    private static readonly DependencyPropertyKey VisualBackgroundPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "VisualBackground", typeof(Brush), typeof(FluentMotion), new PropertyMetadata(null));
    public static readonly DependencyProperty VisualBackgroundProperty = VisualBackgroundPropertyKey.DependencyProperty;
    private static readonly DependencyPropertyKey VisualBorderBrushPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "VisualBorderBrush", typeof(Brush), typeof(FluentMotion), new PropertyMetadata(null));
    public static readonly DependencyProperty VisualBorderBrushProperty = VisualBorderBrushPropertyKey.DependencyProperty;

    public static double GetVisualOpacity(DependencyObject value) => (double)value.GetValue(VisualOpacityProperty);
    public static Brush GetVisualBackground(DependencyObject value) => (Brush)value.GetValue(VisualBackgroundProperty);
    public static Brush GetVisualBorderBrush(DependencyObject value) => (Brush)value.GetValue(VisualBorderBrushProperty);

    public static bool GetIsEnabled(DependencyObject value) => (bool)value.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject value, bool enabled) => value.SetValue(IsEnabledProperty, enabled);
    public static Brush GetBackground(DependencyObject value) => (Brush)value.GetValue(BackgroundProperty);
    public static void SetBackground(DependencyObject value, Brush brush) => value.SetValue(BackgroundProperty, brush);
    public static Brush GetBorderBrush(DependencyObject value) => (Brush)value.GetValue(BorderBrushProperty);
    public static void SetBorderBrush(DependencyObject value, Brush brush) => value.SetValue(BorderBrushProperty, brush);
    public static double GetOpacity(DependencyObject value) => (double)value.GetValue(OpacityProperty);
    public static void SetOpacity(DependencyObject value, double opacity) => value.SetValue(OpacityProperty, opacity);
    public static bool GetReveal(DependencyObject value) => (bool)value.GetValue(RevealProperty);
    public static void SetReveal(DependencyObject value, bool reveal) => value.SetValue(RevealProperty, reveal);
    public static string GetAxis(DependencyObject value) => (string)value.GetValue(AxisProperty);
    public static void SetAxis(DependencyObject value, string axis) => value.SetValue(AxisProperty, axis);
    public static bool GetPressed(DependencyObject value) => (bool)value.GetValue(PressedProperty);
    public static void SetPressed(DependencyObject value, bool pressed) => value.SetValue(PressedProperty, pressed);
    public static double GetAngle(DependencyObject value) => (double)value.GetValue(AngleProperty);
    public static void SetAngle(DependencyObject value, double angle) => value.SetValue(AngleProperty, angle);
    public static bool GetEntrance(DependencyObject value) => (bool)value.GetValue(EntranceProperty);
    public static void SetEntrance(DependencyObject value, bool entrance) => value.SetValue(EntranceProperty, entrance);
    public static bool GetPopup(DependencyObject value) => (bool)value.GetValue(PopupProperty);
    public static void SetPopup(DependencyObject value, bool popup) => value.SetValue(PopupProperty, popup);
    public static object GetTransitionKey(DependencyObject value) => value.GetValue(TransitionKeyProperty);
    public static void SetTransitionKey(DependencyObject value, object content) => value.SetValue(TransitionKeyProperty, content);
    public static bool GetPop(DependencyObject value) => (bool)value.GetValue(PopProperty);
    public static void SetPop(DependencyObject value, bool pop) => value.SetValue(PopProperty, pop);
    public static bool GetPopOnClick(DependencyObject value) => (bool)value.GetValue(PopOnClickProperty);
    public static void SetPopOnClick(DependencyObject value, bool pop) => value.SetValue(PopOnClickProperty, pop);
    public static bool GetBusy(DependencyObject value) => (bool)value.GetValue(BusyProperty);
    public static void SetBusy(DependencyObject value, bool busy) => value.SetValue(BusyProperty, busy);
    public static bool GetIsInvalid(DependencyObject value) => (bool)value.GetValue(IsInvalidProperty);
    public static void SetIsInvalid(DependencyObject value, bool invalid) => value.SetValue(IsInvalidProperty, invalid);

    internal static TimeSpan Duration(FrameworkElement element, string key = "Motion.Normal")
    {
        lock (Gate)
        {
            if (!Tracked.TryGetValue(element, out _))
            {
                Tracked.Add(element, new object());
                Owners.RemoveAll(reference => !reference.TryGetTarget(out _));
                Owners.Add(new WeakReference<FrameworkElement>(element));
            }
        }

        return GetIsEnabled(element) && SystemAnimationsEnabled()
            ? element.TryFindResource(key) as TimeSpan? ?? TimeSpan.FromMilliseconds(167)
            : TimeSpan.Zero;
    }

    private static void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != "ClientAreaAnimation")
        {
            return;
        }

        lock (Gate)
        {
            foreach (WeakReference<FrameworkElement> reference in Owners)
            {
                if (reference.TryGetTarget(out FrameworkElement? element) && !element.Dispatcher.HasShutdownStarted)
                {
                    _ = element.Dispatcher.BeginInvoke(() =>
                    {
                        if (!SystemAnimationsEnabled())
                        {
                            StopAnimations(element);
                        }
                        else if (GetBusy(element))
                        {
                            OnBusyChanged(element, new DependencyPropertyChangedEventArgs(BusyProperty, false, true));
                        }
                    });
                }
            }
        }
    }

    private static DependencyProperty Register(string name, Type type, object? initial, PropertyChangedCallback callback) =>
        DependencyProperty.RegisterAttached(name, type, typeof(FluentMotion), new PropertyMetadata(initial, callback));

    private static void OnBrushChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not Border border)
        {
            return;
        }

        DependencyProperty property = args.Property == BackgroundProperty ? Border.BackgroundProperty : Border.BorderBrushProperty;
        DependencyPropertyKey visualProperty = args.Property == BackgroundProperty ? VisualBackgroundPropertyKey : VisualBorderBrushPropertyKey;
        if (args.NewValue is not SolidColorBrush target)
        {
            border.SetValue(visualProperty, args.NewValue);
            BindingOperations.GetBindingExpression(border, property)?.UpdateTarget();
            return;
        }

        Color previous = (border.GetValue(property) as SolidColorBrush)?.Color ?? target.Color;
        // Every target owns its brush. Theme resources may be frozen and are never animated.
        SolidColorBrush local = new(target.Color);
        // The template binds its base brush to this value; native trigger setters retain precedence.
        border.SetValue(visualProperty, local);
        BindingOperations.GetBindingExpression(border, property)?.UpdateTarget();
        if (border.IsLoaded && Duration(border) != TimeSpan.Zero)
        {
            local.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(previous, target.Color, Duration(border))
            {
                EasingFunction = border.TryFindResource("Motion.EaseOut") as IEasingFunction,
                FillBehavior = FillBehavior.Stop
            });
        }
    }

    private static void OnOpacityChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is FrameworkElement element)
        {
            UpdateOpacity(element);
        }
    }

    private static void UpdateOpacity(FrameworkElement element)
    {
        double previous = element.Opacity;
        element.SetValue(VisualOpacityPropertyKey, GetReveal(element) ? GetOpacity(element) : 0d);
        BindingOperations.GetBindingExpression(element, UIElement.OpacityProperty)?.UpdateTarget();
        Animate(element, element, UIElement.OpacityProperty,
            (double)element.GetAnimationBaseValue(UIElement.OpacityProperty), from: element.IsLoaded ? previous : null);
    }

    private static TransformGroup Transforms(FrameworkElement element)
    {
        if (element.RenderTransform is TransformGroup group && group.Children.Count == 3 &&
            !group.IsFrozen && group.Children[0] is ScaleTransform && group.Children[1] is RotateTransform && group.Children[2] is TranslateTransform)
        {
            return group;
        }

        group = new TransformGroup { Children = { new ScaleTransform(), new RotateTransform(), new TranslateTransform() } };
        element.RenderTransformOrigin = new Point(.5, .5);
        element.RenderTransform = group;
        return group;
    }

    private static void OnRevealChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        bool reveal = GetReveal(element);
        ScaleTransform scale = (ScaleTransform)Transforms(element).Children[0];
        string axis = GetAxis(element);
        UpdateOpacity(element);
        if (axis is "X" or "Both")
        {
            Animate(element, scale, ScaleTransform.ScaleXProperty, reveal ? 1 : 0);
        }

        if (axis is "Y" or "Both")
        {
            Animate(element, scale, ScaleTransform.ScaleYProperty, reveal ? 1 : 0);
        }
    }

    private static void OnPressedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is FrameworkElement element)
        {
            ScaleTransform scale = (ScaleTransform)Transforms(element).Children[0];
            bool pressed = (bool)args.NewValue;
            Animate(element, scale, ScaleTransform.ScaleXProperty, pressed ? .97 : 1, pressed ? "Motion.Fast" : "Motion.Slow", spring: !pressed);
            Animate(element, scale, ScaleTransform.ScaleYProperty, pressed ? .97 : 1, pressed ? "Motion.Fast" : "Motion.Slow", spring: !pressed);
        }
    }

    private static void OnAngleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is FrameworkElement element)
        {
            Animate(element, (RotateTransform)Transforms(element).Children[1], RotateTransform.AngleProperty, (double)args.NewValue);
        }
    }

    private static void OnEntranceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        element.Loaded -= OnLoaded;
        element.IsVisibleChanged -= OnVisibleChanged;
        if ((bool)args.NewValue)
        {
            element.Loaded += OnLoaded;
            element.IsVisibleChanged += OnVisibleChanged;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (ReferenceEquals(sender, args.OriginalSource))
        {
            Enter((FrameworkElement)sender);
        }
    }

    private static void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if ((bool)args.NewValue)
        {
            Enter((FrameworkElement)sender);
        }
    }

    private static void OnPopupChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is Popup popup)
        {
            popup.Opened -= OnPopupOpened;
            if ((bool)args.NewValue)
            {
                popup.Opened += OnPopupOpened;
            }
        }
    }

    private static void OnPopupOpened(object? sender, EventArgs args)
    {
        if (sender is Popup { Child: FrameworkElement child } popup)
        {
            SetIsEnabled(child, GetIsEnabled(popup));
            Enter(child);
        }
    }

    private static void OnContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is FrameworkElement element)
        {
            Enter(element);
        }
    }

    internal static void Enter(FrameworkElement element)
    {
        // Animate the client content, never Window geometry or placement.
        if (element is Window window)
        {
            if (window.Content is FrameworkElement content)
            {
                Enter(content, scale: true);
            }

            return;
        }

        Enter(element, scale: false);
    }

    private static void Enter(FrameworkElement element, bool scale)
    {
        TransformGroup group = Transforms(element);
        Animate(element, element, UIElement.OpacityProperty,
            (double)element.GetAnimationBaseValue(UIElement.OpacityProperty), from: 0);
        Animate(element, (TranslateTransform)group.Children[2], TranslateTransform.YProperty, 0, from: scale ? 0 : 4);
        if (scale)
        {
            Animate(element, (ScaleTransform)group.Children[0], ScaleTransform.ScaleXProperty, 1, from: .98);
            Animate(element, (ScaleTransform)group.Children[0], ScaleTransform.ScaleYProperty, 1, from: .98);
        }
    }

    private static void OnPopChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is FrameworkElement element)
        {
            ScaleTransform scale = (ScaleTransform)Transforms(element).Children[0];
            Animate(element, scale, ScaleTransform.ScaleXProperty, 1, "Motion.Slow", from: 1.2, spring: true);
            Animate(element, scale, ScaleTransform.ScaleYProperty, 1, "Motion.Slow", from: 1.2, spring: true);
        }
    }

    private static void OnPopOnClickChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is FrameworkElement element)
        {
            element.PreviewMouseLeftButtonUp -= OnPopClick;
            if ((bool)args.NewValue)
            {
                element.PreviewMouseLeftButtonUp += OnPopClick;
            }
        }
    }

    private static void OnPopClick(object sender, System.Windows.Input.MouseButtonEventArgs args) =>
        OnPopChanged((DependencyObject)sender, default);

    internal static TranslateTransform Translation(FrameworkElement element) => (TranslateTransform)Transforms(element).Children[2];

    private static void OnBusyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is FrameworkElement element)
        {
            element.BeginAnimation(UIElement.OpacityProperty, null);
            if ((bool)args.NewValue && Duration(element) != TimeSpan.Zero)
            {
                TimeSpan pulse = element.TryFindResource("Motion.Busy") as TimeSpan? ?? TimeSpan.FromSeconds(.8);
                double opacity = (double)element.GetAnimationBaseValue(UIElement.OpacityProperty);
                element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(.35 * opacity, opacity, pulse)
                {
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = element.TryFindResource("Motion.EaseOut") as IEasingFunction
                });
            }
        }
    }

    internal static void Animate(FrameworkElement owner, Animatable target, DependencyProperty property, double to,
        string key = "Motion.Normal", double? from = null, bool spring = false)
    {
        double previous = from ?? (double)target.GetValue(property);
        target.BeginAnimation(property, null);
        // These are private transform components, never a control's styled properties.
        target.SetValue(property, to);
        if ((owner.IsLoaded || from.HasValue) && Duration(owner, key) != TimeSpan.Zero)
        {
            target.BeginAnimation(property, new DoubleAnimation(previous, to, Duration(owner, key))
            {
                EasingFunction = owner.TryFindResource(spring ? "Motion.Spring" : "Motion.EaseOut") as IEasingFunction,
                FillBehavior = FillBehavior.Stop
            });
        }
    }

    internal static void Animate(FrameworkElement owner, FrameworkElement target, DependencyProperty property, double to, double? from = null)
    {
        double previous = from ?? (double)target.GetValue(property);
        target.BeginAnimation(property, null);
        if ((owner.IsLoaded || from.HasValue) && Duration(owner) != TimeSpan.Zero)
        {
            DoubleAnimation animation = new(previous, to, Duration(owner))
            {
                EasingFunction = owner.TryFindResource("Motion.EaseOut") as IEasingFunction,
                FillBehavior = FillBehavior.Stop
            };
            // Animate only the effective value. FillBehavior.Stop restores the binding/style/trigger base.
            target.BeginAnimation(property, animation);
        }
    }

    private static void OnEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is FrameworkElement element)
        {
            if (!(bool)args.NewValue)
            {
                StopAnimations(element);
            }
            else if (GetBusy(element))
            {
                OnBusyChanged(element, new DependencyPropertyChangedEventArgs(BusyProperty, false, true));
            }
        }
    }

    private static void StopAnimations(FrameworkElement element)
    {
        element.BeginAnimation(UIElement.OpacityProperty, null);
        if (element.RenderTransform is TransformGroup transforms)
        {
            foreach (Transform transform in transforms.Children)
            {
                if (transform.IsFrozen)
                {
                    continue;
                }

                if (transform is ScaleTransform scale)
                {
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
                else if (transform is TranslateTransform translate)
                {
                    translate.BeginAnimation(TranslateTransform.XProperty, null);
                    translate.BeginAnimation(TranslateTransform.YProperty, null);
                }
                else if (transform is RotateTransform rotate)
                {
                    rotate.BeginAnimation(RotateTransform.AngleProperty, null);
                }
            }
        }

        if (element is Border border)
        {
            if (border.Background is SolidColorBrush { IsFrozen: false } background)
            {
                background.BeginAnimation(SolidColorBrush.ColorProperty, null);
            }

            if (border.BorderBrush is SolidColorBrush { IsFrozen: false } borderBrush)
            {
                borderBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
            }
        }
    }
}
