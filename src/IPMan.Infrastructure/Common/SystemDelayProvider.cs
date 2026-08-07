using IPMan.Application.Common;

namespace IPMan.Infrastructure.Common;

public sealed class SystemDelayProvider : IDelayProvider
{
    public Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken) =>
        duration <= TimeSpan.Zero
            ? Task.CompletedTask
            : Task.Delay(duration, cancellationToken);
}
