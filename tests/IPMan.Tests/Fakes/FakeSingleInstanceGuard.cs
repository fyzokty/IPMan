using IPMan.Infrastructure.Common;

namespace IPMan.Tests.Fakes;

internal sealed class FakeSingleInstanceGuard : ISingleInstanceGuard
{
    public SingleInstanceAcquisition Acquisition { get; set; } =
        SingleInstanceAcquisition.Acquired;

    public int AcquireCount { get; private set; }

    public int ReleaseCount { get; private set; }

    public int DisposeCount { get; private set; }

    public SingleInstanceAcquisition TryAcquire()
    {
        AcquireCount++;
        return Acquisition;
    }

    public void Release() => ReleaseCount++;

    public void Dispose() => DisposeCount++;
}
