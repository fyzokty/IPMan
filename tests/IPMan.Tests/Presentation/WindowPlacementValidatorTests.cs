using System.Windows;
using IPMan.App.Presentation;
using IPMan.Domain.Settings;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed class WindowPlacementValidatorTests
{
    [Fact]
    public void Validate_WhenPlacementIsEntirelyInsideMonitor_PreservesPlacement()
    {
        AppWindowPlacement placement = new(120, 80, 900, 600, IsMaximized: false);

        AppWindowPlacement result = WindowPlacementValidator.Validate(
            placement,
            [new Rect(0, 0, 1920, 1080)],
            new Rect(0, 0, 1920, 1080),
            720,
            480,
            1100,
            700);

        Assert.Equal(placement, result);
    }

    [Fact]
    public void Validate_WhenPlacementIsPartlyOutsideMonitor_CentersOnPrimaryWorkArea()
    {
        AppWindowPlacement result = WindowPlacementValidator.Validate(
            new AppWindowPlacement(1500, 100, 900, 600, IsMaximized: false),
            [new Rect(0, 0, 1920, 1080)],
            new Rect(0, 0, 1920, 1080),
            720,
            480,
            1100,
            700);

        Assert.Equal(510, result.Left);
        Assert.Equal(240, result.Top);
        Assert.Equal(900, result.Width);
        Assert.Equal(600, result.Height);
    }

    [Fact]
    public void Validate_WhenSavedMonitorIsUnavailable_CentersOnPrimaryWorkArea()
    {
        AppWindowPlacement result = WindowPlacementValidator.Validate(
            new AppWindowPlacement(-1500, 20, 800, 600, IsMaximized: false),
            [new Rect(0, 0, 1920, 1080)],
            new Rect(0, 0, 1920, 1080),
            720,
            480,
            1100,
            700);

        Assert.Equal(560, result.Left);
        Assert.Equal(240, result.Top);
    }

    [Fact]
    public void Validate_WhenSizeIsInvalid_UsesDefaultSizeAndCentersIt()
    {
        AppWindowPlacement result = WindowPlacementValidator.Validate(
            new AppWindowPlacement(20, 30, 100, 0, IsMaximized: false),
            [],
            new Rect(0, 0, 1920, 1080),
            720,
            480,
            1100,
            700);

        Assert.Equal(1100, result.Width);
        Assert.Equal(700, result.Height);
        Assert.Equal(410, result.Left);
        Assert.Equal(190, result.Top);
    }

    [Fact]
    public void Validate_WhenPlacementIsMaximized_PreservesMaximizedState()
    {
        AppWindowPlacement result = WindowPlacementValidator.Validate(
            new AppWindowPlacement(100, 100, 1000, 700, IsMaximized: true),
            [new Rect(0, 0, 1920, 1080), new Rect(1920, 0, 1920, 1080)],
            new Rect(0, 0, 1920, 1080),
            720,
            480,
            1100,
            700);

        Assert.True(result.IsMaximized);
    }
}
