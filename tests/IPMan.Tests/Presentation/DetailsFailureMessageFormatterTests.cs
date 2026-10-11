using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed class DetailsFailureMessageFormatterTests
{
    public static IEnumerable<object[]> StaticStatuses => Enum.GetValues<StaticIpv4ApplyStatus>().Select(status => new object[] { status });
    public static IEnumerable<object[]> DhcpStatuses => Enum.GetValues<DhcpApplyStatus>().Select(status => new object[] { status });

    [Theory]
    [MemberData(nameof(StaticStatuses))]
    public void StaticOutcome_OnlyPreMutationStopsPromiseUnchanged(StaticIpv4ApplyStatus status)
    {
        StaticIpv4ApplyResult result = new(status, SafetyBlock: status == StaticIpv4ApplyStatus.SafetyBlocked ? StaticIpv4SafetyBlock.NotElevated : StaticIpv4SafetyBlock.None);
        DetailsFailureMessage? message = DetailsFailureMessageFormatter.Describe(result, "Saha PLC", "Ofis");
        if (status is StaticIpv4ApplyStatus.VerifiedSuccess or StaticIpv4ApplyStatus.NoChange) { Assert.Null(message); return; }
        Assert.NotNull(message);
        Assert.Equal(Strings.FormatDetailsFailureTitle("Saha PLC"), message.Title);
        bool safe = status is StaticIpv4ApplyStatus.ValidationFailed or StaticIpv4ApplyStatus.AdapterUnavailable
            or StaticIpv4ApplyStatus.AdapterReadFailed or StaticIpv4ApplyStatus.SafetyBlocked
            or StaticIpv4ApplyStatus.ConflictConfirmationRequired or StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired
            or StaticIpv4ApplyStatus.RecoveryCaptureFailed or StaticIpv4ApplyStatus.RecoveryStateUnavailable or StaticIpv4ApplyStatus.Cancelled;
        Assert.Equal(safe, message.NetworkState == Strings.FormatDetailsUnchanged("Ofis"));
        // The explanation is only the "what happened" sentence; the network state is a separate line.
        Assert.Equal(ApplyResultMessageFormatter.Describe(result).Reason, message.Explanation);
        Assert.DoesNotContain(Strings.NetworkUnchanged, message.Explanation, StringComparison.Ordinal);
        Assert.NotEmpty(message.TechnicalDetails);
    }

    [Theory]
    [MemberData(nameof(DhcpStatuses))]
    public void DhcpOutcome_OnlyPreMutationStopsPromiseUnchanged(DhcpApplyStatus status)
    {
        DhcpApplyResult result = new(status, SafetyBlock: status == DhcpApplyStatus.SafetyBlocked ? StaticIpv4SafetyBlock.NotElevated : StaticIpv4SafetyBlock.None);
        DetailsFailureMessage? message = DetailsFailureMessageFormatter.Describe(result, "Otomatik", "Ofis");
        if (status is DhcpApplyStatus.VerifiedSuccess or DhcpApplyStatus.NoChange) { Assert.Null(message); return; }
        Assert.NotNull(message);
        bool safe = status is DhcpApplyStatus.AdapterUnavailable or DhcpApplyStatus.AdapterReadFailed
            or DhcpApplyStatus.SafetyBlocked or DhcpApplyStatus.RecoveryCaptureFailed or DhcpApplyStatus.RecoveryStateUnavailable or DhcpApplyStatus.Cancelled;
        Assert.Equal(safe, message.NetworkState == Strings.FormatDetailsUnchanged("Ofis"));
    }

    [Fact]
    public void MutationNeverAttempted_MayPromiseUnchangedAndPreservesTechnicalCodes()
    {
        NetworkApplyResult mutation = new(NetworkMutationStepResult.NotAttempted(), NetworkMutationStepResult.NotAttempted(),
            NetworkMutationStepResult.NotAttempted(), NetworkMutationFailureKind.AdapterUnavailable, "diagnostic");
        DetailsFailureMessage message = DetailsFailureMessageFormatter.Describe(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.MutationFailed, Mutation: mutation), "Saha", "Ofis")!;
        Assert.Equal(Strings.FormatDetailsUnchanged("Ofis"), message.NetworkState);
        Assert.Contains("diagnostic", message.TechnicalDetails, StringComparison.Ordinal);
    }
}
