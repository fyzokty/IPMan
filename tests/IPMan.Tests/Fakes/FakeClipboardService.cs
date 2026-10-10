using IPMan.App.Presentation;

namespace IPMan.Tests.Fakes;

public sealed class FakeClipboardService : IClipboardService
{
    public List<string> CopiedTexts { get; } = new();

    public string? LastCopiedText => CopiedTexts.Count > 0 ? CopiedTexts[^1] : null;

    public bool Succeeds { get; set; } = true;

    public Exception? Failure { get; set; }

    public bool SetText(string text)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        CopiedTexts.Add(text);
        return Succeeds;
    }
}
