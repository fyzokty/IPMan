using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.App.ViewModels;
using IPMan.Application.Networking;
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
        Assert.Equal(
            "Adres çakışması onayı alınamadığı için ayarlar uygulanmadı.",
            Strings.ApplyConflictUnconfirmed);
        Assert.Equal(
            "Çakışma denetimi sonuçsuz kaldığı için ayarlar uygulanmadı.",
            Strings.ApplyProbeIndeterminateUnconfirmed);

        ApplyStatusMessage staticConflict = ApplyResultMessageFormatter.Describe(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ConflictConfirmationRequired));
        ApplyStatusMessage staticProbe = ApplyResultMessageFormatter.Describe(
            new StaticIpv4ApplyResult(StaticIpv4ApplyStatus.ProbeIndeterminateConfirmationRequired));
        ApplyStatusMessage restoreConflict = ApplyResultMessageFormatter.Describe(
            new RecoveryRestoreResult(RecoveryRestoreStatus.ConflictConfirmationRequired));
        ApplyStatusMessage restoreProbe = ApplyResultMessageFormatter.Describe(
            new RecoveryRestoreResult(RecoveryRestoreStatus.ProbeIndeterminateConfirmationRequired));

        Assert.Equal(Strings.ApplyConflictUnconfirmed, staticConflict.Text);
        Assert.Equal(ApplyStatusSeverity.Warning, staticConflict.Severity);
        Assert.Equal(Strings.ApplyProbeIndeterminateUnconfirmed, staticProbe.Text);
        Assert.Equal(ApplyStatusSeverity.Warning, staticProbe.Severity);
        Assert.Equal(Strings.ApplyConflictUnconfirmed, restoreConflict.Text);
        Assert.Equal(ApplyStatusSeverity.Warning, restoreConflict.Severity);
        Assert.Equal(Strings.ApplyProbeIndeterminateUnconfirmed, restoreProbe.Text);
        Assert.Equal(ApplyStatusSeverity.Warning, restoreProbe.Severity);
    }
}
