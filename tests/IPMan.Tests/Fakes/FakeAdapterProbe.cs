using IPMan.Infrastructure.Networking;

namespace IPMan.Tests.Fakes;

public sealed class FakeAdapterProbe : IAdapterProbe
{
    private readonly List<AdapterReadModel> _adapters = new();

    private Exception? _failure;

    public int ReadCount { get; private set; }

    public FakeAdapterProbe WithAdapters(params AdapterReadModel[] adapters)
    {
        _adapters.Clear();
        _adapters.AddRange(adapters);
        return this;
    }

    public FakeAdapterProbe FailWith(Exception failure)
    {
        _failure = failure;
        return this;
    }

    public IReadOnlyList<AdapterReadModel> ReadAdapters()
    {
        ReadCount++;

        if (_failure is not null)
        {
            throw _failure;
        }

        return _adapters.ToArray();
    }
}
