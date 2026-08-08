using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using IPMan.IntegrationTests.Harness;
using IPMan.IntegrationTests.Observation;

namespace IPMan.IntegrationTests.Evidence;

public sealed record Sprint08BeforeEvidence(
    string RunId,
    DateTimeOffset StartedAtUtc,
    string WindowsVersion,
    string TestAssemblyVersion,
    Sprint08Scenario Scenario,
    IReadOnlyList<string> RequestedDimensions,
    NetworkAdapterId ExactAdapterId,
    StaticIpv4Configuration RequestedConfiguration,
    NetworkObservation Observation,
    NetworkAdapterRecoverySnapshot RecoveryState);

public sealed record Sprint08ResultEvidence(
    Sprint08BeforeEvidence Before,
    DateTimeOffset CompletedAtUtc,
    StaticIpv4ApplyResult? ApplyResult,
    NetworkObservation? AfterObservation,
    NetworkAdapterRecoveryReadResult? AfterRecoveryRead,
    bool RollbackMatchesBeforeState,
    bool Passed,
    IReadOnlyList<string> Differences,
    string? Failure);

public sealed record Sprint08SanitizedSummary(
    string RunId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    Sprint08Scenario Scenario,
    IReadOnlyList<string> RequestedDimensions,
    string AdapterIdHash,
    string? ApplyStatus,
    uint? Ipv4Code,
    uint? GatewayCode,
    uint? DnsCode,
    string? RollbackSnapshotId,
    bool RollbackMatchesBeforeState,
    bool Ipv6Observed,
    bool Ipv6RoutesObserved,
    bool RichDnsSettingsObserved,
    bool RicherDnsObservationComplete,
    bool UnsupportedRicherDnsStatePresent,
    bool Passed,
    int DifferenceCount,
    string? FailureCode,
    string? FailureSummary);
