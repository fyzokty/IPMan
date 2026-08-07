namespace IPMan.Application.Networking;

/// <summary>
/// Raised when a discovery pass could not be completed. Discovery remains
/// active; the next network event or manual request triggers a new attempt.
/// </summary>
public sealed class AdapterRefreshFailedEventArgs : EventArgs
{
    public AdapterRefreshFailedEventArgs(string reason, Exception failure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentNullException.ThrowIfNull(failure);

        Reason = reason;
        Failure = failure;
    }

    /// <summary>Technical reason identifier; see <see cref="NetworkChangeReason"/>.</summary>
    public string Reason { get; }

    public Exception Failure { get; }
}
