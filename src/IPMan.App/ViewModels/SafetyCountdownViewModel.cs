using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Common;

namespace IPMan.App.ViewModels;

/// <summary>Uses an absolute deadline so delayed dispatcher ticks never extend the safety window.</summary>
public sealed partial class SafetyCountdownViewModel : ObservableObject, IDisposable
{
    public const int DurationSeconds = 15;
    private readonly IClock _clock;
    private readonly ITimerService _timerService;
    private readonly TaskCompletionSource<SafetyCountdownDecision> _decision = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private IDisposable? _timer;
    private DateTimeOffset _deadline;
    private bool _started;

    [ObservableProperty]
    private int _remainingSeconds = DurationSeconds;

    [ObservableProperty]
    private double _progress = 1;

    public SafetyCountdownViewModel(SafeSwitchRequest request, IClock clock, ITimerService timerService)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(timerService);
        _clock = clock;
        _timerService = timerService;
        Title = Strings.FormatSafeSwitchCountdownTitle(request.SettingName);
        Body = Strings.FormatSafeSwitchCountdownBody(DurationSeconds, request.PreviousSettingName);
    }

    public string Title { get; }
    public string Body { get; }
    public Task<SafetyCountdownDecision> Decision => _decision.Task;

    public void Start()
    {
        if (_started || Decision.IsCompleted)
        {
            return;
        }
        _started = true;
        _deadline = _clock.UtcNow.AddSeconds(DurationSeconds);
        _timer = _timerService.Start(TimeSpan.FromMilliseconds(100), Tick);
    }

    [RelayCommand]
    private void Keep() => Complete(SafetyCountdownDecision.Keep);

    [RelayCommand]
    private void Revert() => Complete(SafetyCountdownDecision.Revert);

    public void Complete(SafetyCountdownDecision decision)
    {
        if (_decision.TrySetResult(decision))
        {
            Dispose();
        }
    }

    private void Tick()
    {
        double remaining = Math.Clamp((_deadline - _clock.UtcNow).TotalSeconds, 0, DurationSeconds);
        RemainingSeconds = (int)Math.Ceiling(remaining);
        Progress = remaining / DurationSeconds;
        if (remaining <= 0)
        {
            Complete(SafetyCountdownDecision.TimedOut);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
