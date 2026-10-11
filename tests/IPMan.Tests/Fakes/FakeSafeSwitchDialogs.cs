using IPMan.App.Presentation;
using IPMan.App.ViewModels;

namespace IPMan.Tests.Fakes;

public sealed class FakeInternetLossConfirmationService : IInternetLossConfirmationService
{
    public InternetLossConfirmationResult Result { get; set; } = new(true);
    public List<InternetLossConfirmationViewModel> Requests { get; } = [];

    public Task<InternetLossConfirmationResult> ConfirmAsync(InternetLossConfirmationViewModel viewModel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(viewModel);
        return Task.FromResult(Result);
    }
}

public sealed class FakeSafetyCountdownDialogService : ISafetyCountdownDialogService
{
    public List<SafetyCountdownViewModel> Countdowns { get; } = [];
    public SafeSwitchExitDecision ExitDecision { get; set; } = SafeSwitchExitDecision.Cancel;
    public int ExitPromptCount { get; private set; }
    public Exception? ShowFailure { get; set; }
    public Action? DuringExitPrompt { get; set; }
    public TaskCompletionSource CountdownShown { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task ShowAsync(SafetyCountdownViewModel viewModel)
    {
        Countdowns.Add(viewModel);
        CountdownShown.TrySetResult();
        if (ShowFailure is not null)
        {
            throw ShowFailure;
        }
        await viewModel.Decision;
    }

    public int ExitDeferredCount { get; private set; }

    public Task<SafeSwitchExitDecision> ConfirmExitAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExitPromptCount++;
        DuringExitPrompt?.Invoke();
        return Task.FromResult(ExitDecision);
    }

    public Task ShowExitDeferredAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ExitDeferredCount++;
        return Task.CompletedTask;
    }
}
