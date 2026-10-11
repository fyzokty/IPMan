using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using IPMan.App.Presentation;
using Xunit;

namespace IPMan.Tests.Presentation;

internal static class WpfTestRunner
{
    internal static void RunWithSystemAnimations(Action action, string timeoutMessage, bool enabled = true) =>
        Run(() =>
        {
            Func<bool> previous = FluentMotion.SystemAnimationsEnabled;
            try
            {
                FluentMotion.SystemAnimationsEnabled = () => enabled;
                action();
            }
            finally
            {
                FluentMotion.SystemAnimationsEnabled = previous;
            }
        }, timeoutMessage);

    // Call only from the WPF/STA collection so each dispatcher is shut down before the next test starts.
    internal static void Run(Action action, string timeoutMessage)
    {
        Exception? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                try
                {
                    action();
                }
                finally
                {
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), timeoutMessage);
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
