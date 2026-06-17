using System.Reflection;

namespace TranslateTool.Services;

public static class TranslatorFactory
{
    private const string GoogleTranslatorTypeName = "TranslateTool.Services.GoogleTranslator";
    private const string MicrosoftTranslatorTypeName = "TranslateTool.Services.MicrosoftTranslator";
    private const string BaiduTranslatorTypeName = "TranslateTool.Services.BaiduTranslator";
    private const string DeepLTranslatorTypeName = "TranslateTool.Services.DeepLTranslator";
    private const string AiTranslatorTypeName = "TranslateTool.Services.AiTranslator";

    public static ITranslator Create(string engineName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(engineName);

        var normalized = engineName.Trim().ToLowerInvariant();
        var translatorTypeName = normalized switch
        {
            "google" or "google translate" => GoogleTranslatorTypeName,
            "microsoft" or "微软翻译" => MicrosoftTranslatorTypeName,
            "baidu" or "百度翻译" => BaiduTranslatorTypeName,
            "deepl" or "deepl 翻译" => DeepLTranslatorTypeName,
            "ai" or "ai翻译" or "ai 翻译" => AiTranslatorTypeName,
            _ => throw new NotSupportedException($"翻译引擎 '{engineName}' 暂不支持。可用引擎: baidu, google, microsoft, deepl, ai")
        };

        var translatorType = Assembly.GetExecutingAssembly().GetType(translatorTypeName, throwOnError: false);
        if (translatorType is null)
        {
            throw new NotSupportedException(
                $"翻译引擎 '{translatorTypeName}' 尚未实现。");
        }

        if (Activator.CreateInstance(translatorType) is not ITranslator translator)
        {
            throw new InvalidOperationException(
                $"类型 '{translatorType.FullName}' 未实现 ITranslator 接口。");
        }

        return translator;
    }

    /// <summary>
    /// 获取所有可用的翻译引擎名称
    /// </summary>
    public static string[] GetAvailableEngines()
    {
        return ["baidu", "google", "microsoft", "deepl", "ai"];
    }
}
