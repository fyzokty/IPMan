namespace IPMan.App.Presentation;

/// <summary>
/// UI synchronization boundary. Infrastructure/application events may arrive on
/// arbitrary threads; ViewModels use this to touch WPF-observed state safely.
/// </summary>
public interface IUiDispatcher
{
    void Post(Action action);
}
