using IPMan.Infrastructure.Common;
using IPMan.Tests.Fakes;
using Xunit;

namespace IPMan.Tests.Common;

public sealed class ActivationChannelTests
{
    [Fact]
    public async Task TrySendActivationAsync_WhenServerIsListening_RaisesActivationEvent()
    {
        using FakeActivationChannelServer server = new();
        FakeActivationChannelClient client = new(server);
        int received = 0;
        server.ActivationRequested += (_, _) => received++;
        server.StartListening();

        bool sent = await client.TrySendActivationAsync(CancellationToken.None);

        Assert.True(sent);
        Assert.Equal(1, server.ActivationCount);
        Assert.Equal(1, received);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitialAttemptsFail_RetriesWithDelayUntilSuccessful()
    {
        ImmediateDelayProvider delay = new();
        FakeActivationAttempt attempt = new() { FailuresBeforeSuccess = 2 };
        ActivationChannelRetryPolicy policy = new(
            delay,
            new ActivationChannelOptions
            {
                Timeout = TimeSpan.FromMilliseconds(200),
                InitialRetryDelay = TimeSpan.FromMilliseconds(20),
                MaximumRetryDelay = TimeSpan.FromMilliseconds(80)
            });

        bool sent = await policy.ExecuteAsync(
            attempt.TrySendAsync,
            CancellationToken.None);

        Assert.True(sent);
        Assert.Equal(3, attempt.AttemptCount);
        Assert.Equal(2, delay.DelayCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEveryAttemptFails_StopsAtTimeoutBudget()
    {
        ImmediateDelayProvider delay = new();
        FakeActivationAttempt attempt = new() { AlwaysFail = true };
        ActivationChannelRetryPolicy policy = new(
            delay,
            new ActivationChannelOptions
            {
                Timeout = TimeSpan.FromMilliseconds(70),
                InitialRetryDelay = TimeSpan.FromMilliseconds(20),
                MaximumRetryDelay = TimeSpan.FromMilliseconds(40)
            });

        bool sent = await policy.ExecuteAsync(
            attempt.TrySendAsync,
            CancellationToken.None);

        Assert.False(sent);
        Assert.Equal(4, attempt.AttemptCount);
        Assert.Equal(3, delay.DelayCount);
    }
}
