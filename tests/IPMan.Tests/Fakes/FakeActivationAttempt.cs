namespace IPMan.Tests.Fakes;

internal sealed class FakeActivationAttempt
{
    public int FailuresBeforeSuccess { get; set; }

    public bool AlwaysFail { get; set; }

    public int AttemptCount { get; private set; }

    public Task<bool> TrySendAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AttemptCount++;

        bool succeeded = !AlwaysFail && AttemptCount > FailuresBeforeSuccess;
        return Task.FromResult(succeeded);
    }
}
