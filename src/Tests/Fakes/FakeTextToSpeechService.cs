using TranslateTool.Services;

namespace TranslateTool.Tests.Fakes;

public sealed class FakeTextToSpeechService : ITextToSpeechService
{
    public string? LastSpokenText { get; private set; }

    public void Speak(string text)
    {
        LastSpokenText = text;
    }
}
