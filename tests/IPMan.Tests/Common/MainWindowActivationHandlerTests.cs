using IPMan.App.Views;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Common;

public sealed class MainWindowActivationHandlerTests
{
    [Fact]
    public void OnActivationRequested_PostsWindowActivationToUiDispatcher()
    {
        FakeUiDispatcher dispatcher = new();
        int activationCount = 0;
        MainWindowActivationHandler handler = new(
            dispatcher,
            () => activationCount++);

        handler.OnActivationRequested(this, EventArgs.Empty);

        Assert.Equal(1, dispatcher.PostCount);
        Assert.Equal(1, activationCount);
    }
}
