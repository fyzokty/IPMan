using System.Management;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;

namespace IPMan.Infrastructure.Networking;

/// <summary>Persistent static IPv4 mutation through Win32_NetworkAdapterConfiguration.</summary>
public sealed class WmiNetworkAdapterConfigurator : INetworkAdapterConfigurator
{
    private const string EnableStaticMethod = "EnableStatic";
    private const string SetGatewaysMethod = "SetGateways";
    private const string SetDnsMethod = "SetDNSServerSearchOrder";

    private readonly IWmiNetworkAdapterSessionFactory _sessionFactory;

    public WmiNetworkAdapterConfigurator()
        : this(new SystemWmiNetworkAdapterSessionFactory())
    {
    }

    internal WmiNetworkAdapterConfigurator(IWmiNetworkAdapterSessionFactory sessionFactory)
    {
        ArgumentNullException.ThrowIfNull(sessionFactory);
        _sessionFactory = sessionFactory;
    }

    public async Task<NetworkApplyResult> ApplyStaticAsync(
        NetworkAdapterId adapterId,
        StaticIpv4MutationPlan mutationPlan,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mutationPlan);
        ValidatePlan(mutationPlan);
        cancellationToken.ThrowIfCancellationRequested();

        // WMI is synchronous. Once this worker begins its critical mutation,
        // caller cancellation cannot imply that Windows was rolled back.
        return await Task.Run(
                () => ApplyCore(adapterId, mutationPlan),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    private NetworkApplyResult ApplyCore(
        NetworkAdapterId adapterId,
        StaticIpv4MutationPlan plan)
    {
        WmiAdapterResolution resolution;

        try
        {
            resolution = _sessionFactory.ResolveBySettingId(adapterId.Value);
        }
        catch (ManagementException exception)
        {
            return FailureBeforeMutation(NetworkMutationFailureKind.ManagementFailure, exception.Message);
        }

        if (resolution.Status != WmiAdapterResolutionStatus.Found || resolution.Session is null)
        {
            return FailureBeforeMutation(
                resolution.Status == WmiAdapterResolutionStatus.Ambiguous
                    ? NetworkMutationFailureKind.AdapterMappingAmbiguous
                    : NetworkMutationFailureKind.AdapterUnavailable);
        }

        using IWmiNetworkAdapterSession session = resolution.Session;
        StaticIpv4Configuration configuration = plan.Configuration;

        StepExecution ipv4 = InvokeEnableStatic(
            session,
            new Dictionary<string, object?>
            {
                ["IPAddress"] = new[] { configuration.Ipv4Address },
                ["SubnetMask"] = new[] { configuration.SubnetMask }
            },
            plan.PreviousMode);

        if (!ipv4.Result.IsSuccessful)
        {
            return new NetworkApplyResult(
                ipv4.Result,
                NetworkMutationStepResult.NotAttempted(),
                NetworkMutationStepResult.NotAttempted(),
                ipv4.FailureKind,
                ipv4.TechnicalMessage);
        }

        StepExecution gateway;

        if (plan.GatewayMode == GatewayMutationMode.LeaveAbsent)
        {
            gateway = new StepExecution(NetworkMutationStepResult.NotRequired());
        }
        else
        {
            // Microsoft documents the EnableStatic host address as SetGateways'
            // sentinel for clearing the gateway. This is infrastructure-only;
            // user input still rejects a literal self gateway.
            string gatewayValue = plan.GatewayMode == GatewayMutationMode.Clear
                ? configuration.Ipv4Address
                : configuration.Gateway!;
            gateway = Invoke(
                session,
                SetGatewaysMethod,
                new Dictionary<string, object?>
                {
                    ["DefaultIPGateway"] = new[] { gatewayValue },
                    ["GatewayCostMetric"] = new[]
                    {
                        plan.GatewayMode == GatewayMutationMode.Set
                            ? plan.GatewayMetric!.Value
                            : (ushort)1
                    }
                });
        }

        if (!gateway.Result.IsSuccessful)
        {
            return new NetworkApplyResult(
                ipv4.Result,
                gateway.Result,
                NetworkMutationStepResult.NotAttempted(),
                gateway.FailureKind,
                gateway.TechnicalMessage);
        }

        StepExecution dns;

        if (plan.DnsMode == DnsMutationMode.LeaveUnchanged)
        {
            dns = new StepExecution(NetworkMutationStepResult.NotRequired());
        }
        else
        {
            string[] dnsServers = new[] { configuration.PrimaryDns, configuration.SecondaryDns }
                .Where(value => value is not null)
                .Select(value => value!)
                .ToArray();

            // Microsoft documents omission of all input parameters as the way
            // to return from static DNS to automatic source semantics.
            dns = Invoke(
                session,
                SetDnsMethod,
                plan.DnsMode == DnsMutationMode.ClearToAutomatic
                    ? null
                    : new Dictionary<string, object?> { ["DNSServerSearchOrder"] = dnsServers });
        }

        return new NetworkApplyResult(
            ipv4.Result,
            gateway.Result,
            dns.Result,
            dns.FailureKind,
            dns.TechnicalMessage);
    }

    private static StepExecution Invoke(
        IWmiNetworkAdapterSession session,
        string methodName,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        try
        {
            uint code = session.Invoke(methodName, parameters);
            return MapResult(code);
        }
        catch (ManagementException exception)
        {
            return new StepExecution(
                new NetworkMutationStepResult(NetworkMutationStepStatus.Failed),
                NetworkMutationFailureKind.ManagementFailure,
                exception.Message);
        }
    }

    private static StepExecution InvokeEnableStatic(
        IWmiNetworkAdapterSession session,
        IReadOnlyDictionary<string, object?> parameters,
        NetworkConfigurationMode previousMode)
    {
        try
        {
            uint code = session.Invoke(EnableStaticMethod, parameters);

            if (code == 81 && previousMode == NetworkConfigurationMode.Static)
            {
                return new StepExecution(
                    new NetworkMutationStepResult(
                        NetworkMutationStepStatus.SucceededProvisionally,
                        code));
            }

            return MapResult(code);
        }
        catch (ManagementException exception)
        {
            return new StepExecution(
                new NetworkMutationStepResult(NetworkMutationStepStatus.Failed),
                NetworkMutationFailureKind.ManagementFailure,
                exception.Message);
        }
    }

    private static StepExecution MapResult(uint code)
    {
        NetworkMutationStepResult result = code switch
        {
            0 => new NetworkMutationStepResult(NetworkMutationStepStatus.Succeeded, code),
            1 => new NetworkMutationStepResult(NetworkMutationStepStatus.SucceededRestartRequired, code),
            _ => new NetworkMutationStepResult(NetworkMutationStepStatus.Failed, code)
        };

        return new StepExecution(
            result,
            result.IsSuccessful
                ? NetworkMutationFailureKind.None
                : NetworkMutationFailureKind.OperationalFailure);
    }

    private static NetworkApplyResult FailureBeforeMutation(
        NetworkMutationFailureKind kind,
        string? message = null) =>
        new(
            NetworkMutationStepResult.NotAttempted(),
            NetworkMutationStepResult.NotAttempted(),
            NetworkMutationStepResult.NotAttempted(),
            kind,
            message);

    private static void ValidatePlan(StaticIpv4MutationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan.Configuration);

        bool hasGateway = plan.Configuration.Gateway is not null;

        bool gatewayPlanIsValid = plan.GatewayMode switch
        {
            GatewayMutationMode.Set => hasGateway &&
                plan.GatewayMetric is >= 1 and <= 9999,
            GatewayMutationMode.Clear or GatewayMutationMode.LeaveAbsent =>
                !hasGateway && plan.GatewayMetric is null,
            _ => false
        };

        if (!gatewayPlanIsValid)
        {
            throw new ArgumentException("Gateway mutation mode does not match the configuration.", nameof(plan));
        }

        bool hasDns = plan.Configuration.PrimaryDns is not null;

        if ((plan.DnsMode == DnsMutationMode.Set && !hasDns) ||
            (plan.DnsMode == DnsMutationMode.ClearToAutomatic && hasDns))
        {
            throw new ArgumentException("DNS mutation mode does not match the configuration.", nameof(plan));
        }
    }

    private sealed record StepExecution(
        NetworkMutationStepResult Result,
        NetworkMutationFailureKind FailureKind = NetworkMutationFailureKind.None,
        string? TechnicalMessage = null);
}
