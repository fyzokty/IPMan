using System.Security.Principal;
using IPMan.Application.Common;

namespace IPMan.Infrastructure.Common;

/// <summary>
/// Reads the elevation state of the current Windows process token.
/// The value cannot change while the process runs, so it is read once.
/// </summary>
public sealed class WindowsElevationStateProvider : IElevationStateProvider
{
    private readonly Lazy<bool> _isElevated = new(ReadIsElevated);

    public bool IsElevated => _isElevated.Value;

    private static bool ReadIsElevated()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
