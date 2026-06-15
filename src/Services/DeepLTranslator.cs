using System.Net.Http;
using System.Text;
using System.Text.Json;
using TranslateTool.Models;

namespace TranslateTool.Services;

public class DeepLTranslator : ITranslator
{
    public string Name => "DeepL 翻译";

    private static string? _apiKey;
    private static readonly HttpClient Client = HttpShared.Client;

    public static void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
    }

    public static bool HasApiKey => !string.IsNullOrEmpty(_apiKey);

    public async Task<TranslationResult> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage)
    {
        var result = new TranslationResult
        {
            SourceText = text,
            SourceLanguage = sourceLanguage,
            TargetLanguage = targetLanguage,
            Engine = Name
        };

        if (!HasApiKey)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "DeepL API Key 未配置。请在设置中配置 DeepL API Key。";
            return result;
        }

        try
        {
            var parameters = new Dictionary<string, string>
            {
                { "text", text },
                { "target_lang", MapLanguageCode(targetLanguage) }
            };

            // 如果不是自动检测，添加源语言参数
            if (!string.Equals(sourceLanguage, "auto", StringComparison.OrdinalIgnoreCase))
            {
                parameters.Add("source_lang", MapLanguageCode(sourceLanguage));
            }

            var content = new FormUrlEncodedContent(parameters);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api-free.deepl.com/v2/translate")
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"DeepL-Auth-Key {_apiKey}");

            var response = await Client.SendAsync(request);

            // 处理速率限制错误
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                response.StatusCode == (System.Net.HttpStatusCode)456)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "DeepL API 请求受限（超过免费版 500,000 字符/月限制），请稍后重试或升级计划。";
                return result;
            }

            if (!response.IsSuccessStatusCode)
            {
                result.IsSuccess = false;
                var errorBody = await response.Content.ReadAsStringAsync();
                result.ErrorMessage = $"DeepL API 错误: HTTP {(int)response.StatusCode} - {errorBody}";
                return result;
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("translations", out var translations))
            {
                var sb = new StringBuilder();
                foreach (var translation in translations.EnumerateArray())
                {
                    if (translation.TryGetProperty("text", out var textElement))
                    {
                        if (sb.Length > 0) sb.AppendLine();
                        sb.Append(textElement.GetString());
                    }
                }

                result.TranslatedText = sb.ToString();
                result.IsSuccess = true;
            }
            else
            {
                result.IsSuccess = false;
                result.ErrorMessage = "DeepL API 返回格式异常";
            }

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private static string MapLanguageCode(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "auto" or "auto-detect" => "",  // DeepL 使用空字符串表示自动检测
            "zh-cn" or "zh" or "chinese" => "ZH",
            "en" or "english" => "EN",
            "ja" or "japanese" => "JA",
            "ko" or "korean" => "KO",
            "fr" or "french" => "FR",
            "de" or "german" => "DE",
            "es" or "spanish" => "ES",
            "it" or "italian" => "IT",
            "pt" or "portuguese" => "PT",
            "ru" or "russian" => "RU",
            "ar" or "arabic" => "AR",
            "nl" or "dutch" => "NL",
            "pl" or "polish" => "PL",
            "sv" or "swedish" => "SV",
            "da" or "danish" => "DA",
            "fi" or "finnish" => "FI",
            "el" or "greek" => "EL",
            "cs" or "czech" => "CS",
            "ro" or "romanian" => "RO",
            "hu" or "hungarian" => "HU",
            _ => code.ToUpperInvariant()
        };
    }
}