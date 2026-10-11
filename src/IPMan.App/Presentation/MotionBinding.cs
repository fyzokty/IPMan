using System.Windows;
using System.Windows.Data;

namespace IPMan.App.Presentation;

/// <summary>Gives template motion a declarative base without writing a native local value.</summary>
public sealed class MotionBinding : Binding
{
    public MotionBinding(DependencyProperty property)
    {
        Path = new PropertyPath(property);
        RelativeSource = new RelativeSource(RelativeSourceMode.Self);
    }
}
