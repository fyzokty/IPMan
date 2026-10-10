using System.Windows;
using System.Windows.Controls;
using IPMan.App.Presentation;

namespace IPMan.App.Views;

/// <summary>Shared themed frame for modal confirmation and text input prompts.</summary>
internal sealed class PromptWindow : Window
{
    internal PromptWindow(string title, string glyph, bool informational = false)
    {
        Title = title;
        Width = 460;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Owner = System.Windows.Application.Current?.MainWindow;
        SetResourceReference(BackgroundProperty, "SurfaceBrush");
        SetResourceReference(ForegroundProperty, "PrimaryTextBrush");
        SetResourceReference(FontFamilyProperty, "BodyFont");
        FontSize = 14;

        DockPanel frame = new();
        Border footer = new()
        {
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(20, 12, 20, 12)
        };
        footer.SetResourceReference(BackgroundProperty, "WindowBackgroundBrush");
        footer.SetResourceReference(BorderBrushProperty, "BorderBrush");
        DockPanel.SetDock(footer, Dock.Bottom);
        Buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        footer.Child = Buttons;
        frame.Children.Add(footer);

        Grid body = new() { Margin = new Thickness(24) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        body.ColumnDefinitions.Add(new ColumnDefinition());
        TextBlock icon = new() { Text = glyph, FontSize = 28, Margin = new Thickness(0, 0, 16, 0) };
        icon.SetResourceReference(FontFamilyProperty, "IconFont");
        icon.SetResourceReference(ForegroundProperty, informational ? "InfoBorderBrush" : "WarningBorderBrush");
        body.Children.Add(icon);
        Body = new StackPanel();
        Grid.SetColumn(Body, 1);
        body.Children.Add(Body);
        frame.Children.Add(body);
        Content = frame;
        FluentMotion.SetEntrance(this, true);
    }

    internal StackPanel Body { get; }

    internal StackPanel Buttons { get; }

    internal Button AddButton(string text, bool accept)
    {
        Button button = new()
        {
            Content = text,
            IsDefault = accept,
            IsCancel = !accept,
            MinWidth = 84,
            Margin = new Thickness(8, 0, 0, 0)
        };
        button.SetResourceReference(StyleProperty, accept ? "PrimaryActionButtonStyle" : "SecondaryActionButtonStyle");
        button.Click += (_, _) => DialogResult = accept;
        Buttons.Children.Add(button);
        return button;
    }
}
