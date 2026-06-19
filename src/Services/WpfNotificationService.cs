using TranslateTool.Views;

namespace TranslateTool.Services;

/// <summary>
/// 基于 WPF Toast 窗口的通知实现。
/// </summary>
public sealed class WpfNotificationService : INotificationService
{
    public void ShowTranslation(string sourceText, string translatedText, string engineName)
    {
        TranslationToastWindow.Show(sourceText, translatedText, engineName);
    }
}
