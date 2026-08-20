using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeNetworkAdapterConfigurator : INetworkAdapterConfigurator
{
    private int _activeCalls;

    public NetworkApplyResult Result { get; set; } = SuccessfulResult();

    public Func<NetworkAdapterId, StaticIpv4MutationPlan, Task<NetworkApplyResult>>? Handler { get; set; }

    public NetworkApplyResult DhcpResult { get; set; } = SuccessfulDhcpResult();

    public Func<NetworkAdapterId, DhcpMutationPlan, Task<NetworkApplyResult>>? DhcpHandler { get; set; }

    public List<StaticIpv4MutationPlan> Plans { get; } = new();

    public List<DhcpMutationPlan> DhcpPlans { get; } = new();

    public int ApplyCount { get; private set; }

    public int DhcpApplyCount { get; private set; }

    public int MaximumConcurrentCalls { get; private set; }

    public async Task<NetworkApplyResult> ApplyStaticAsync(
        NetworkAdapterId adapterId,
        StaticIpv4MutationPlan mutationPlan,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplyCount++;
        Plans.Add(mutationPlan);
        int active = Interlocked.Increment(ref _activeCalls);
        MaximumConcurrentCalls = Math.Max(MaximumConcurrentCalls, active);

        try
        {
            return Handler is null
                ? Result
                : await Handler(adapterId, mutationPlan).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref _activeCalls);
        }
    }

    public async Task<NetworkApplyResult> ApplyDhcpAsync(
        NetworkAdapterId adapterId,
        DhcpMutationPlan mutationPlan,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DhcpApplyCount++;
        DhcpPlans.Add(mutationPlan);
        int active = Interlocked.Increment(ref _activeCalls);
        MaximumConcurrentCalls = Math.Max(MaximumConcurrentCalls, active);

        try
        {
            return DhcpHandler is null
                ? DhcpResult
                : await DhcpHandler(adapterId, mutationPlan).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref _activeCalls);
        }
    }

    public static NetworkApplyResult SuccessfulResult(
        NetworkMutationStepStatus gatewayStatus = NetworkMutationStepStatus.Succeeded) =>
        new(
            new NetworkMutationStepResult(NetworkMutationStepStatus.Succeeded, 0),
            new NetworkMutationStepResult(gatewayStatus, gatewayStatus == NetworkMutationStepStatus.NotRequired ? null : 0),
            new NetworkMutationStepResult(NetworkMutationStepStatus.Succeeded, 0),
            NetworkMutationFailureKind.None);

    public static NetworkApplyResult SuccessfulDhcpResult() =>
        new(
            new NetworkMutationStepResult(NetworkMutationStepStatus.Succeeded, 0),
            NetworkMutationStepResult.NotAttempted(),
            new NetworkMutationStepResult(NetworkMutationStepStatus.Succeeded, 0),
            NetworkMutationFailureKind.None);
}
