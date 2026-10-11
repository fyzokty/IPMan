using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
using IPMan.Domain.Networking;
using Xunit;

namespace IPMan.Tests.Presentation;

public sealed class ApplyResultMessageFormatterTests
{
    [Fact]
    public void StaticFormatter_DescribesEveryStatusWithoutLeakingEnumNames()
    {
        foreach (StaticIpv4ApplyStatus status in Enum.GetValues<StaticIpv4ApplyStatus>())
        {
            StaticIpv4ApplyResult result = new(
                status,
                SafetyBlock: status == StaticIpv4ApplyStatus.SafetyBlocked
                    ? StaticIpv4SafetyBlock.MultipleIpv4Addresses
                    : StaticIpv4SafetyBlock.None);

            ApplyStatusMessage message = ApplyResultMessageFormatter.Describe(result);

            Assert.False(string.IsNullOrWhiteSpace(message.Text));
            Assert.NotEqual(status.ToString(), message.Text);
        }
    }

    [Fact]
    public void DhcpFormatter_DescribesEveryStatusWithoutLeakingEnumNames()
    {
        foreach (DhcpApplyStatus status in Enum.GetValues<DhcpApplyStatus>())
        {
            DhcpApplyResult result = new(
                status,
                status == DhcpApplyStatus.SafetyBlocked
                    ? StaticIpv4SafetyBlock.MultipleIpv4Addresses
                    : StaticIpv4SafetyBlock.None);

            ApplyStatusMessage message = ApplyResultMessageFormatter.Describe(result);

            Assert.False(string.IsNullOrWhiteSpace(message.Text));
            Assert.NotEqual(status.ToString(), message.Text);
        }
    }

    [Fact]
    public void RestoreFormatter_DescribesEveryStatusWithoutLeakingEnumNames()
    {
        foreach (RecoveryRestoreStatus status in Enum.GetValues<RecoveryRestoreStatus>())
        {
            RecoveryRestoreResult result = new(
                status,
                SafetyBlock: status == RecoveryRestoreStatus.SafetyBlocked
                    ? StaticIpv4SafetyBlock.MultipleIpv4Addresses
                    : StaticIpv4SafetyBlock.None);

            ApplyStatusMessage message = ApplyResultMessageFormatter.Describe(result);

            Assert.False(string.IsNullOrWhiteSpace(message.Text));
            Assert.NotEqual(status.ToString(), message.Text);
        }
    }

    [Fact]
    public void SafetyFormatter_UsesDistinctMessageForEveryActualSafetyBlock()
    {
        List<string> messages = new();

        foreach (StaticIpv4SafetyBlock safetyBlock in Enum.GetValues<StaticIpv4SafetyBlock>())
        {
            if (safetyBlock == StaticIpv4SafetyBlock.None)
            {
                continue;
            }

            ApplyStatusMessage message = ApplyResultMessageFormatter.Describe(
                new StaticIpv4ApplyResult(
                    StaticIpv4ApplyStatus.SafetyBlocked,
                    SafetyBlock: safetyBlock));

            Assert.False(string.IsNullOrWhiteSpace(message.Text));
            Assert.NotEqual(safetyBlock.ToString(), message.Text);
            messages.Add(message.Text);
        }

        Assert.Equal(messages.Count, messages.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void SuccessAndValidationFailure_HaveExpectedSeverities()
    {
        ApplyStatusMessage success = ApplyResultMessageFormatter.Describe(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.VerifiedSuccess));
        ApplyStatusMessage validationFailure = ApplyResultMessageFormatter.Describe(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ValidationFailed));

        Assert.Equal(ApplyStatusSeverity.Success, success.Severity);
        Assert.Equal(ApplyStatusSeverity.Error, validationFailure.Severity);
    }

    [Fact]
    public void ConfirmationRequiredStatuses_UseInlineWarningMessages()
    {
        ApplyStatusMessage staticConflict = ApplyResultMessageFormatter.Describe(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ConflictConfirmationRequired));
        ApplyStatusMessage staticProbe = ApplyResultMessageFormatter.Describe(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired));
        ApplyStatusMessage restoreConflict = ApplyResultMessageFormatter.Describe(
            new RecoveryRestoreResult(RecoveryRestoreStatus.ConflictConfirmationRequired));
        ApplyStatusMessage restoreProbe = ApplyResultMessageFormatter.Describe(
            new RecoveryRestoreResult(RecoveryRestoreStatus.ProbeIndeterminateConfirmationRequired));

        Assert.Equal(Compose(Strings.ApplyConflictUnconfirmed, Strings.ApplyConflictUnconfirmedAction), staticConflict.Text);
        Assert.Equal(ApplyStatusSeverity.Warning, staticConflict.Severity);
        Assert.Equal(Compose(Strings.ApplyProbeIndeterminateUnconfirmed, Strings.ApplyProbeIndeterminateUnconfirmedAction), staticProbe.Text);
        Assert.Equal(ApplyStatusSeverity.Warning, staticProbe.Severity);
        Assert.Equal(staticConflict.Text, restoreConflict.Text);
        Assert.Equal(ApplyStatusSeverity.Warning, restoreConflict.Severity);
        Assert.Equal(staticProbe.Text, restoreProbe.Text);
        Assert.Equal(ApplyStatusSeverity.Warning, restoreProbe.Severity);
    }

    private static string Compose(string reason, string next) =>
        string.Join(" ", reason, Strings.NetworkUnchanged, next);

    [Fact]
    public void NetworkUnchangedAssurance_IsOnlyUsedForOutcomesThatStoppedBeforeMutation()
    {
        StaticIpv4ApplyStatus[] stopped =
        [
            StaticIpv4ApplyStatus.ValidationFailed, StaticIpv4ApplyStatus.AdapterUnavailable,
            StaticIpv4ApplyStatus.AdapterReadFailed, StaticIpv4ApplyStatus.SafetyBlocked,
            StaticIpv4ApplyStatus.ConflictConfirmationRequired, StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired,
            StaticIpv4ApplyStatus.RecoveryCaptureFailed, StaticIpv4ApplyStatus.Cancelled,
            StaticIpv4ApplyStatus.RecoveryStateUnavailable
        ];

        foreach (StaticIpv4ApplyStatus status in Enum.GetValues<StaticIpv4ApplyStatus>())
        {
            ApplyStatusMessage message = ApplyResultMessageFormatter.Describe(new StaticIpv4ApplyResult(
                status,
                SafetyBlock: status == StaticIpv4ApplyStatus.SafetyBlocked
                    ? StaticIpv4SafetyBlock.NotElevated
                    : StaticIpv4SafetyBlock.None));

            Assert.Equal(
                stopped.Contains(status),
                message.Text.Contains(Strings.NetworkUnchanged, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void RejectedMutation_PromisesUnchangedOnlyWhenNothingWasSentToWindows()
    {
        NetworkApplyResult notAttempted = new(NetworkMutationStepResult.NotAttempted(), NetworkMutationStepResult.NotAttempted(),
            NetworkMutationStepResult.NotAttempted(), NetworkMutationFailureKind.AdapterUnavailable);

        ApplyStatusMessage neverSent = ApplyResultMessageFormatter.Describe(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.MutationFailed, Mutation: notAttempted));
        ApplyStatusMessage unknown = ApplyResultMessageFormatter.Describe(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.MutationFailed));

        Assert.Contains(Strings.NetworkUnchanged, neverSent.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(Strings.NetworkUnchanged, unknown.Text, StringComparison.Ordinal);
        Assert.Contains(Strings.NetworkUnverified, unknown.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidInput_ListsFieldsAfterTheFormulaAndGivesAnExample()
    {
        ApplyStatusMessage message = ApplyResultMessageFormatter.DescribeInvalidInput("Bilgisayarın adresi");

        Assert.StartsWith(Strings.ApplyValidationFailed, message.Text, StringComparison.Ordinal);
        Assert.Contains(Strings.NetworkUnchanged, message.Text, StringComparison.Ordinal);
        Assert.Contains(Strings.ExampleAddress, message.Text, StringComparison.Ordinal);
        Assert.EndsWith("Bilgisayarın adresi", message.Text, StringComparison.Ordinal);
    }
}
