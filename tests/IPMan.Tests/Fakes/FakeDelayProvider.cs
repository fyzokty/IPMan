using IPMan.Application.Common;

namespace IPMan.Tests.Fakes;

/// <summary>
/// Delay provider whose waits complete only when the test releases them, so
/// debounce and reconciliation behaviour can be asserted without wall-clock
/// timing.
/// <para>
/// Waits are tracked per requested duration, which lets a test drive the
/// debounce window and the reconciliation interval independently.
/// </para>
/// </summary>
public sealed class FakeDelayProvider : IDelayProvider, IDisposable
{
    private readonly Dictionary<TimeSpan, DurationState> _states = new();
    private readonly object _sync = new();

    public Task DelayAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        TaskCompletionSource pending;
        DurationState state;

        lock (_sync)
        {
            state = GetState(duration);
            state.StartCount++;
            pending = state.Pending;
        }

        state.Started.Release();

        return pending.Task.WaitAsync(cancellationToken);
    }

    /// <summary>Waits until a delay of this duration has been entered.</summary>
    public Task<bool> WaitForDelayStartedAsync(TimeSpan duration, TimeSpan timeout)
    {
        DurationState state;

        lock (_sync)
        {
            state = GetState(duration);
        }

        return state.Started.WaitAsync(timeout);
    }

    /// <summary>Completes every delay of this duration currently being awaited.</summary>
    public void ReleaseDelays(TimeSpan duration)
    {
        TaskCompletionSource pending;

        lock (_sync)
        {
            DurationState state = GetState(duration);
            pending = state.Pending;
            state.Pending = CreatePending();
        }

        pending.TrySetResult();
    }

    public int DelayCount(TimeSpan duration)
    {
        lock (_sync)
        {
            return GetState(duration).StartCount;
        }
    }

    public int TotalDelayCount
    {
        get
        {
            lock (_sync)
            {
                return _states.Values.Sum(state => state.StartCount);
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            foreach (DurationState state in _states.Values)
            {
                state.Started.Dispose();
            }

            _states.Clear();
        }
    }

    private DurationState GetState(TimeSpan duration)
    {
        if (!_states.TryGetValue(duration, out DurationState? state))
        {
            state = new DurationState();
            _states.Add(duration, state);
        }

        return state;
    }

    private static TaskCompletionSource CreatePending() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class DurationState
    {
        public SemaphoreSlim Started { get; } = new(0);

        public TaskCompletionSource Pending { get; set; } = CreatePending();

        public int StartCount { get; set; }
    }
}
