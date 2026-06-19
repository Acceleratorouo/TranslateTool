using TranslateTool.Services;

namespace TranslateTool.Tests.Fakes;

public sealed class FakeNotificationService : INotificationService
{
    public (string Source, string Translated, string Engine)? LastNotification { get; private set; }

    public void ShowTranslation(string sourceText, string translatedText, string engineName)
    {
        LastNotification = (sourceText, translatedText, engineName);
    }
}
