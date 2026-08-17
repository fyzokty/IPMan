namespace IPMan.Infrastructure.Common;

/// <summary>
/// Sends the single supported activation signal to the current interactive
/// instance. Intentionally internal because it is a local lifecycle protocol,
/// not a general remote-control API.
/// </summary>
internal interface IActivationChannelClient
{
    /// <summary>Returns whether the activation signal reached the server.</summary>
    Task<bool> TrySendActivationAsync(CancellationToken cancellationToken);
}

/// <summary>Constants for the deliberately minimal activation protocol.</summary>
internal static class ActivationChannelProtocol
{
    public const string PipeName = "IPMan.Activation";
    public const byte ActivationSignal = 1;
}
