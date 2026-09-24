namespace IPMan.App.Presentation;

/// <summary>Asks the user to confirm an action that changes Windows network state.</summary>
public interface IUserConfirmationService
{
    /// <summary>Returns whether the user accepted the described action.</summary>
    bool Confirm(UserConfirmationRequest request);

    /// <summary>Returns the confirmation and, when offered, the opt-out choice.</summary>
    (bool Accepted, bool DoNotShowAgain) ConfirmWithOptions(UserConfirmationRequest request) =>
        (Confirm(request), false);
}
