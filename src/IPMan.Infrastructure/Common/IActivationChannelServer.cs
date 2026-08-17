namespace IPMan.Infrastructure.Common;

/// <summary>
/// Receives the single supported activation signal from another local IPMan
/// process. Intentionally internal because activation is a process-lifecycle
/// transport detail rather than an application service.
/// </summary>
internal interface IActivationChannelServer : IDisposable
{
    /// <summary>Raised when a valid activation signal is received.</summary>
    event EventHandler? ActivationRequested;

    /// <summary>Starts accepting activation signals.</summary>
    void StartListening();

    /// <summary>Stops accepting activation signals.</summary>
    void StopListening();
}
