using TranslateTool.Models;

namespace TranslateTool.Services;

/// <summary>
/// 基于 LLM 的 AI 翻译引擎，实现 ITranslator 接口。
/// 仅在用户选择 "ai" 引擎时启用，不作为全局默认翻译源。
/// </summary>
public class AiTranslator : ITranslator
{
    /// <summary>
    /// 引擎显示名称
    /// </summary>
    public string Name => "AI 翻译";

    /// <summary>
    /// 执行 AI 翻译
    /// </summary>
    /// <param name="text">待翻译文本</param>
    /// <param name="sourceLanguage">源语言</param>
    /// <param name="targetLanguage">目标语言</param>
    /// <returns>翻译结果</returns>
    public async Task<TranslationResult> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TranslationResult
            {
                SourceText = text,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                Engine = Name,
                IsSuccess = false,
                ErrorMessage = "待翻译文本为空"
            };
        }

        var provider = LlmProviderService.GetDefaultProvider();
        if (provider is null || !provider.IsEnabled)
        {
            return new TranslationResult
            {
                SourceText = text,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                Engine = Name,
                IsSuccess = false,
                ErrorMessage = "未配置或未启用 AI 翻译供应商，请前往设置添加。"
            };
        }

        var model = LlmProviderService.Settings.DefaultModel;
        if (string.IsNullOrWhiteSpace(model) && provider.Models.Count > 0)
        {
            model = provider.Models[0];
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            return new TranslationResult
            {
                SourceText = text,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                Engine = Name,
                IsSuccess = false,
                ErrorMessage = "未选择 AI 翻译模型。"
            };
        }

        try
        {
            using var client = CreateClient(provider);
            var systemPrompt = LlmProviderService.Settings.SystemPrompt;
            var userPrompt = BuildPrompt(text, sourceLanguage, targetLanguage);

            var translated = await client.ChatCompletionAsync(
                model,
                systemPrompt,
                userPrompt,
                LlmProviderService.Settings.Temperature,
                LlmProviderService.Settings.MaxTokens);

            if (string.IsNullOrWhiteSpace(translated))
            {
                return new TranslationResult
                {
                    SourceText = text,
                    SourceLanguage = sourceLanguage,
                    TargetLanguage = targetLanguage,
                    Engine = Name,
                    IsSuccess = false,
                    ErrorMessage = "AI 返回了空译文。"
                };
            }

            return new TranslationResult
            {
                SourceText = text,
                TranslatedText = translated.Trim(),
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                Engine = $"{Name} ({provider.DisplayName}/{model})",
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            return new TranslationResult
            {
                SourceText = text,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                Engine = Name,
                IsSuccess = false,
                ErrorMessage = $"AI 翻译请求失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 根据供应商配置创建 LLM 客户端
    /// </summary>
    private static ILlmClient CreateClient(LlmProvider provider)
    {
        return provider.ApiFormat switch
        {
            LlmApiFormat.OpenAiCompatible or LlmApiFormat.Ollama => new OpenAiCompatibleClient(
                provider,
                TimeSpan.FromSeconds(LlmProviderService.Settings.TimeoutSeconds)),
            LlmApiFormat.Gemini => new OpenAiCompatibleClient(
                provider,
                TimeSpan.FromSeconds(LlmProviderService.Settings.TimeoutSeconds)),
            _ => throw new NotSupportedException($"不支持的 API 格式: {provider.ApiFormat}")
        };
    }

    /// <summary>
    /// 构造翻译提示词
    /// </summary>
    private static string BuildPrompt(string text, string sourceLanguage, string targetLanguage)
    {
        var src = sourceLanguage == "auto" ? "自动检测语言" : sourceLanguage;
        return $"将以下文本从 {src} 翻译成 {targetLanguage}，只输出译文，不要解释：\n\n{text}";
    }
}
