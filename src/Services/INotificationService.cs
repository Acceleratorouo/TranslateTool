namespace TranslateTool.Services;

/// <summary>
/// 翻译结果通知服务抽象，用于显示 Toast 等轻量通知。
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 显示翻译结果通知。
    /// </summary>
    void ShowTranslation(string sourceText, string translatedText, string engineName);
}
