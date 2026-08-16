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
    private readonly IIpv4DefaultRouteManager _defaultRouteManager;
    private readonly IManualIpv4DnsWriter _manualDnsWriter;

    public WmiNetworkAdapterConfigurator(IIpv4DefaultRouteManager defaultRouteManager)
        : this(
            new SystemWmiNetworkAdapterSessionFactory(),
            defaultRouteManager,
            new WindowsManualIpv4DnsWriter())
    {
    }

    internal WmiNetworkAdapterConfigurator(
        IWmiNetworkAdapterSessionFactory sessionFactory,
        IIpv4DefaultRouteManager defaultRouteManager)
        : this(sessionFactory, defaultRouteManager, new WindowsManualIpv4DnsWriter())
    {
    }

    internal WmiNetworkAdapterConfigurator(
        IWmiNetworkAdapterSessionFactory sessionFactory,
        IIpv4DefaultRouteManager defaultRouteManager,
        IManualIpv4DnsWriter manualDnsWriter)
    {
        ArgumentNullException.ThrowIfNull(sessionFactory);
        ArgumentNullException.ThrowIfNull(defaultRouteManager);
        ArgumentNullException.ThrowIfNull(manualDnsWriter);
        _sessionFactory = sessionFactory;
        _defaultRouteManager = defaultRouteManager;
        _manualDnsWriter = manualDnsWriter;
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
        else if (plan.GatewayMode == GatewayMutationMode.Clear)
        {
            gateway = ClearDefaultRoutes(adapterId);
        }
        else
        {
            gateway = Invoke(
                session,
                SetGatewaysMethod,
                new Dictionary<string, object?>
                {
                    ["DefaultIPGateway"] = new[] { configuration.Gateway! },
                    ["GatewayCostMetric"] = new[] { plan.GatewayMetric!.Value }
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
        else if (plan.DnsMode == DnsMutationMode.ClearToAutomatic)
        {
            // The WMI contract resets DNS when its one input value is null.
            // Supplying no input object causes some providers to reject the
            // call with code 68; an empty string or array is also invalid.
            dns = InvokeDnsReset(
                session,
                new Dictionary<string, object?> { ["DNSServerSearchOrder"] = null });
        }
        else
        {
            string[] dnsServers = new[] { configuration.PrimaryDns, configuration.SecondaryDns }
                .Where(value => value is not null)
                .Select(value => value!)
                .ToArray();
            dns = SetManualIpv4Dns(adapterId, dnsServers);
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

    private static StepExecution InvokeDnsReset(
        IWmiNetworkAdapterSession session,
        IReadOnlyDictionary<string, object?> parameters)
    {
        try
        {
            uint code = session.Invoke(SetDnsMethod, parameters);
            return MapDnsResetResult(code);
        }
        catch (ManagementException exception)
        {
            return new StepExecution(
                new NetworkMutationStepResult(NetworkMutationStepStatus.Failed),
                NetworkMutationFailureKind.ManagementFailure,
                exception.Message);
        }
    }

    private StepExecution ClearDefaultRoutes(NetworkAdapterId adapterId)
    {
        Ipv4DefaultRouteClearResult result = _defaultRouteManager.Clear(adapterId);

        if (result.IsSuccess)
        {
            return new StepExecution(
                new NetworkMutationStepResult(NetworkMutationStepStatus.Succeeded));
        }

        NetworkMutationFailureKind failure = result.Status switch
        {
            Ipv4DefaultRouteClearStatus.InvalidAdapterIdentity or
                Ipv4DefaultRouteClearStatus.InterfaceResolutionFailed =>
                NetworkMutationFailureKind.InterfaceResolutionFailure,
            Ipv4DefaultRouteClearStatus.PersistentStoreReadFailed or
                Ipv4DefaultRouteClearStatus.PersistentStoreDeleteFailed =>
                NetworkMutationFailureKind.PersistentRouteFailure,
            Ipv4DefaultRouteClearStatus.ActiveStoreReadFailed or
                Ipv4DefaultRouteClearStatus.ActiveStoreDeleteFailed =>
                NetworkMutationFailureKind.ActiveRouteFailure,
            Ipv4DefaultRouteClearStatus.RouteStillPresent =>
                NetworkMutationFailureKind.RouteVerificationFailure,
            Ipv4DefaultRouteClearStatus.AccessDenied =>
                NetworkMutationFailureKind.AccessDenied,
            _ => NetworkMutationFailureKind.OperationalFailure
        };

        return new StepExecution(
            new NetworkMutationStepResult(NetworkMutationStepStatus.Failed, result.TechnicalCode),
            failure,
            result.Status.ToString());
    }

    private StepExecution SetManualIpv4Dns(
        NetworkAdapterId adapterId,
        IReadOnlyList<string> servers)
    {
        ManualIpv4DnsWriteResult result = _manualDnsWriter.Write(adapterId, servers);

        if (result.IsSuccess)
        {
            return new StepExecution(
                new NetworkMutationStepResult(
                    NetworkMutationStepStatus.Succeeded,
                    result.TechnicalCode));
        }

        return new StepExecution(
            new NetworkMutationStepResult(
                NetworkMutationStepStatus.Failed,
                result.TechnicalCode),
            result.Status == ManualIpv4DnsWriteStatus.InvalidAdapterIdentity
                ? NetworkMutationFailureKind.InterfaceResolutionFailure
                : NetworkMutationFailureKind.OperationalFailure,
            result.Status.ToString());
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

    private static StepExecution MapDnsResetResult(uint code)
    {
        StepExecution execution = MapResult(code);

        if (execution.Result.IsSuccessful)
        {
            return execution;
        }

        return code switch
        {
            64 => execution with
            {
                TechnicalMessage = "Automatic DNS reset is not supported by the WMI provider."
            },
            68 => execution with
            {
                TechnicalMessage = "Automatic DNS reset was rejected as an invalid input parameter."
            },
            91 => execution with
            {
                FailureKind = NetworkMutationFailureKind.AccessDenied,
                TechnicalMessage = "Automatic DNS reset was denied by the WMI provider."
            },
            _ => execution with
            {
                TechnicalMessage = "Automatic DNS reset failed in the WMI provider."
            }
        };
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
