using IPMan.App.Services;
using IPMan.Application.Common;
using IPMan.Application.Settings;
using IPMan.Domain.Settings;
using Xunit;

namespace IPMan.Tests.Services;

public sealed class OperationNotificationServiceTests
{
    [Theory]
    [InlineData(NotificationMode.Disabled, false, false, false, false)]
    [InlineData(NotificationMode.WhenUnfocused, true, false, true, false)]
    [InlineData(NotificationMode.WhenUnfocused, false, false, true, true)]
    [InlineData(NotificationMode.WhenUnfocused, true, true, true, true)]
    [InlineData(NotificationMode.WhenUnfocused, true, false, false, true)]
    [InlineData(NotificationMode.Always, true, false, true, true)]
    public void ShouldShow_WhenModeAndWindowStateAreProvided_ReturnsExpected(
        NotificationMode mode,
        bool isActive,
        bool isMinimized,
        bool isVisible,
        bool expected)
    {
        bool result = OperationNotificationService.ShouldShow(
            mode,
            new OperationNotificationService.NotificationWindowState(isActive, isMinimized, isVisible));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Report_WhenNotificationsAreDisabled_StillKeepsNewestFiftyHistoryItems()
    {
        OperationNotificationService service = new(
            new FakeSettingsRepository(AppSettings.Default),
            new FixedClock());

        for (int index = 0; index < 51; index++)
        {
            service.Report("Uygula", $"{{00000000-0000-0000-0000-{index:D12}}}", $"Bağdaştırıcı {index}", index % 2 == 0);
        }

        Assert.Equal(50, service.History.Count);
        Assert.Equal("Bağdaştırıcı 50", service.History[0].AdapterName);
        Assert.Equal("Bağdaştırıcı 1", service.History[^1].AdapterName);
        Assert.Contains("Uygula", service.History[0].Text, StringComparison.Ordinal);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeSettingsRepository : IAppSettingsRepository
    {
        private readonly AppSettings _settings;

        public FakeSettingsRepository(AppSettings settings) => _settings = settings;

        public bool LastLoadFailed => false;

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(_settings);

        public Task<SettingsSaveResult> SaveAsync(AppSettings settings, CancellationToken cancellationToken) =>
            Task.FromResult(SettingsSaveResult.Success());
    }
}
