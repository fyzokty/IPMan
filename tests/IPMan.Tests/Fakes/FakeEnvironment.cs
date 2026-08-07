using IPMan.App.Presentation;
using IPMan.Application.Common;

namespace IPMan.Tests.Fakes;

public sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; set; }
}

public sealed class FakeElevationStateProvider : IElevationStateProvider
{
    public FakeElevationStateProvider(bool isElevated) => IsElevated = isElevated;

    public bool IsElevated { get; }
}

public sealed class FakeApplicationVersionProvider : IApplicationVersionProvider
{
    public FakeApplicationVersionProvider(string version) => Version = version;

    public string Version { get; }
}
