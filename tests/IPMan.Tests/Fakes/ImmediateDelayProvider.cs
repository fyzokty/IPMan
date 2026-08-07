using IPMan.Application.Common;

namespace IPMan.Tests.Fakes;

public sealed class ImmediateDelayProvider : IDelayProvider
{
    public int DelayCount { get; private set; }

    public Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DelayCount++;
        return Task.CompletedTask;
    }
}
