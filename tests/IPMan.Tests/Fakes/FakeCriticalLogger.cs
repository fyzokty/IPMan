using IPMan.Application.Logging;

namespace IPMan.Tests.Fakes;

public sealed class FakeCriticalLogger : ICriticalLogger
{
    public List<CriticalLogEntry> Entries { get; } = new();
    public Exception? Failure { get; set; }

    public event EventHandler? WriteFailed
    {
        add { }
        remove { }
    }

    public void Log(CriticalLogEntry entry)
    {
        Entries.Add(entry);
        if (Failure is not null)
        {
            throw Failure;
        }
    }
}
