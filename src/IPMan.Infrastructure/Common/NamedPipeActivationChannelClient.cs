using System.IO.Pipes;
using IPMan.Application.Common;

namespace IPMan.Infrastructure.Common;

/// <summary>Sends the minimal activation signal over the local named pipe.</summary>
internal sealed class NamedPipeActivationChannelClient : IActivationChannelClient
{
    private readonly ActivationChannelRetryPolicy _retryPolicy;

    public NamedPipeActivationChannelClient(
        IDelayProvider delayProvider,
        ActivationChannelOptions options)
    {
        _retryPolicy = new ActivationChannelRetryPolicy(delayProvider, options);
    }

    public Task<bool> TrySendActivationAsync(CancellationToken cancellationToken) =>
        _retryPolicy.ExecuteAsync(TrySendOnceAsync, cancellationToken);

    private static async Task<bool> TrySendOnceAsync(CancellationToken cancellationToken)
    {
        using NamedPipeClientStream pipe = new(
            ".",
            ActivationChannelProtocol.PipeName,
            PipeDirection.Out,
            PipeOptions.Asynchronous);

        try
        {
            await pipe
                .ConnectAsync(timeout: 0, cancellationToken)
                .ConfigureAwait(false);

            byte[] signal = [ActivationChannelProtocol.ActivationSignal];
            await pipe.WriteAsync(signal, cancellationToken).ConfigureAwait(false);
            await pipe.FlushAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}

/// <summary>
/// Applies bounded exponential backoff independently of the activation
/// transport so retry behaviour can be tested without opening a real pipe.
/// </summary>
internal sealed class ActivationChannelRetryPolicy
{
    public static readonly TimeSpan MaximumTimeout = TimeSpan.FromSeconds(30);

    private readonly IDelayProvider _delayProvider;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _initialRetryDelay;
    private readonly TimeSpan _maximumRetryDelay;

    public ActivationChannelRetryPolicy(
        IDelayProvider delayProvider,
        ActivationChannelOptions options)
    {
        ArgumentNullException.ThrowIfNull(delayProvider);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Timeout <= TimeSpan.Zero || options.Timeout > MaximumTimeout)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.Timeout,
                "Activation timeout must be positive and bounded.");
        }

        if (options.InitialRetryDelay <= TimeSpan.Zero ||
            options.InitialRetryDelay > options.Timeout)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.InitialRetryDelay,
                "Initial retry delay must be positive and no longer than the timeout.");
        }

        if (options.MaximumRetryDelay < options.InitialRetryDelay ||
            options.MaximumRetryDelay > options.Timeout)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.MaximumRetryDelay,
                "Maximum retry delay must be between the initial delay and the timeout.");
        }

        _delayProvider = delayProvider;
        _timeout = options.Timeout;
        _initialRetryDelay = options.InitialRetryDelay;
        _maximumRetryDelay = options.MaximumRetryDelay;
    }

    public async Task<bool> ExecuteAsync(
        Func<CancellationToken, Task<bool>> trySendAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trySendAsync);
        cancellationToken.ThrowIfCancellationRequested();

        using CancellationTokenSource timeoutCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(_timeout);

        TimeSpan remainingDelayBudget = _timeout;
        TimeSpan retryDelay = _initialRetryDelay;

        while (true)
        {
            try
            {
                if (await trySendAsync(timeoutCancellation.Token).ConfigureAwait(false))
                {
                    return true;
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (remainingDelayBudget <= TimeSpan.Zero)
            {
                return false;
            }

            TimeSpan actualDelay = retryDelay <= remainingDelayBudget
                ? retryDelay
                : remainingDelayBudget;

            try
            {
                await _delayProvider
                    .DelayAsync(actualDelay, timeoutCancellation.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            remainingDelayBudget -= actualDelay;
            retryDelay = DoubleAndCap(retryDelay, _maximumRetryDelay);
        }
    }

    private static TimeSpan DoubleAndCap(TimeSpan value, TimeSpan maximum)
    {
        long doubledTicks = value.Ticks > long.MaxValue / 2
            ? long.MaxValue
            : value.Ticks * 2;

        return TimeSpan.FromTicks(Math.Min(doubledTicks, maximum.Ticks));
    }
}
