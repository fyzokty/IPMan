using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPMan.App.Presentation;
using IPMan.App.Resources;
using IPMan.Application.Common;

namespace IPMan.App.ViewModels;

/// <summary>
/// Uses an absolute monotonic deadline so delayed dispatcher ticks never extend the safety
/// window and wall-clock changes can neither freeze nor shorten it.
/// </summary>
public sealed partial class SafetyCountdownViewModel : ObservableObject, IDisposable
{
    public const int DurationSeconds = 15;
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(DurationSeconds);
    private readonly IMonotonicClock _clock;
    private readonly ITimerService _timerService;
    private readonly TaskCompletionSource<SafetyCountdownDecision> _decision = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private IDisposable? _timer;
    private TimeSpan _deadline;
    private TimeSpan? _pausedRemaining;
    private bool _started;

    [ObservableProperty]
    private int _remainingSeconds = DurationSeconds;

    [ObservableProperty]
    private double _progress = 1;

    /// <param name="unverified">The mutation ran but could not be verified; the title says so.</param>
    public SafetyCountdownViewModel(SafeSwitchRequest request, IMonotonicClock clock, ITimerService timerService,
        bool unverified = false)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(timerService);
        _clock = clock;
        _timerService = timerService;
        IsUnverified = unverified;
        Title = unverified
            ? Strings.FormatSafeSwitchUnverifiedCountdownTitle(request.SettingName)
            : Strings.FormatSafeSwitchCountdownTitle(request.SettingName);
        Body = Strings.FormatSafeSwitchCountdownBody(DurationSeconds, request.PreviousSettingName);
    }

    public string Title { get; }
    public string Body { get; }
    public bool IsUnverified { get; }
    public bool IsPaused => _pausedRemaining is not null;
    public Task<SafetyCountdownDecision> Decision => _decision.Task;

    public void Start()
    {
        if (_started || Decision.IsCompleted)
        {
            return;
        }
        _started = true;
        _deadline = _clock.Elapsed + Duration;
        _timer = _timerService.Start(TimeSpan.FromMilliseconds(100), Tick);
    }

    /// <summary>Freezes the remaining time, e.g. while the exit question is open.</summary>
    public void Pause()
    {
        if (!_started || Decision.IsCompleted || _pausedRemaining is not null)
        {
            return;
        }
        _pausedRemaining = Remaining();
        Publish(_pausedRemaining.Value);
    }

    /// <summary>Continues from the remaining time captured by <see cref="Pause"/>.</summary>
    public void Resume()
    {
        if (_pausedRemaining is not TimeSpan remaining)
        {
            return;
        }
        _pausedRemaining = null;
        if (Decision.IsCompleted)
        {
            return;
        }
        _deadline = _clock.Elapsed + remaining;
        Tick();
    }

    [RelayCommand]
    private void Keep() => Complete(SafetyCountdownDecision.Keep);

    [RelayCommand]
    private void Revert() => Complete(SafetyCountdownDecision.Revert);

    /// <summary>Returns false when an earlier decision already won.</summary>
    public bool Complete(SafetyCountdownDecision decision)
    {
        if (_decision.TrySetResult(decision))
        {
            Dispose();
            return true;
        }
        return false;
    }

    private TimeSpan Remaining()
    {
        TimeSpan remaining = _deadline - _clock.Elapsed;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining > Duration ? Duration : remaining;
    }

    private void Publish(TimeSpan remaining)
    {
        RemainingSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
        Progress = remaining.TotalSeconds / DurationSeconds;
    }

    private void Tick()
    {
        if (_pausedRemaining is not null || Decision.IsCompleted)
        {
            return;
        }
        TimeSpan remaining = Remaining();
        Publish(remaining);
        if (remaining <= TimeSpan.Zero)
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
