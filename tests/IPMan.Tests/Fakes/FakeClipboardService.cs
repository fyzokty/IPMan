using IPMan.App.Presentation;

namespace IPMan.Tests.Fakes;

public sealed class FakeClipboardService : IClipboardService
{
    public List<string> CopiedTexts { get; } = new();

    public string? LastCopiedText => CopiedTexts.Count > 0 ? CopiedTexts[^1] : null;

    public bool SetText(string text)
    {
        CopiedTexts.Add(text);
        return true;
    }
}
