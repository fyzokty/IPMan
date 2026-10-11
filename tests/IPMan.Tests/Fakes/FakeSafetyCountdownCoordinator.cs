using IPMan.App.Presentation;
using IPMan.Domain.Settings;

namespace IPMan.Tests.Fakes;

public sealed class FakeSafetyCountdownCoordinator : ISafetyCountdownCoordinator
{
    public bool IsCountdownActive { get; set; }
    public event EventHandler<AppSettings>? SettingsChanged;
    public List<SafeSwitchRequest> Requests { get; } = [];
    public List<SafeSwitchApplyResult> ApplyResults { get; } = [];
    public SafeSwitchOutcome Outcome { get; set; } = new(SafeSwitchStatus.Kept);
    public bool SkipApply { get; set; }
    public TaskCompletionSource<SafeSwitchOutcome>? PendingOutcome { get; set; }
    public bool ExitApproved { get; set; }
    public int ExitRequests { get; private set; }

    public async Task<SafeSwitchOutcome> RunAsync(SafeSwitchRequest request,
        Func<CancellationToken, Task<SafeSwitchApplyResult>> applyAsync, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (SkipApply)
        {
            return Outcome;
        }
        SafeSwitchApplyResult applied = await applyAsync(cancellationToken);
        ApplyResults.Add(applied);
        if (applied != SafeSwitchApplyResult.Applied)
        {
            return new(applied == SafeSwitchApplyResult.NoChange ? SafeSwitchStatus.NoChange : SafeSwitchStatus.ApplyFailed);
        }
        IsCountdownActive = true;
        try
        {
            return PendingOutcome is null ? Outcome : await PendingOutcome.Task;
        }
        finally
        {
            IsCountdownActive = false;
        }
    }

    public Task<bool> RequestExitAsync(CancellationToken cancellationToken)
    {
        ExitRequests++;
        return Task.FromResult(ExitApproved);
    }

    public void PublishSettings(AppSettings settings) => SettingsChanged?.Invoke(this, settings);
}
