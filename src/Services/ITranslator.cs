using TranslateTool.Models;

namespace TranslateTool.Services;

public interface ITranslator
{
    string Name { get; }

    Task<TranslationResult> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage);
}
