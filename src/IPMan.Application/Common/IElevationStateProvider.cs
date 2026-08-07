namespace IPMan.Application.Common;

/// <summary>
/// Reports whether the current process runs with administrative rights.
/// The operating-system check itself belongs to infrastructure.
/// </summary>
public interface IElevationStateProvider
{
    bool IsElevated { get; }
}
