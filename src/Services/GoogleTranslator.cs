using System.Net.Http;
using System.Text.Json;
using TranslateTool.Models;

namespace TranslateTool.Services;

public class GoogleTranslator : ITranslator
{
    public string Name => "Google 翻译";

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

        var sl = MapLanguageCode(sourceLanguage);
        var tl = MapLanguageCode(targetLanguage);

        // 方式1: Google Translate API (gtx)
        if (await TryGoogleApi(text, sl, tl, result))
            return result;

        // 方式2: MyMemory API (可靠的免费备用)
        if (await TryMyMemoryApi(text, sl, tl, result))
            return result;

        result.IsSuccess = false;
        result.ErrorMessage = "翻译服务暂时不可用，请使用百度或 DeepL 引擎";
        return result;
    }

    private async Task<bool> TryGoogleApi(string text, string sl, string tl, TranslationResult result)
    {
        try
        {
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={Uri.EscapeDataString(sl)}&tl={Uri.EscapeDataString(tl)}&dt=t&dj=1&q={Uri.EscapeDataString(text)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            var response = await HttpShared.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();

            // Google 返回验证码页面时是 HTML，直接跳过
            if (string.IsNullOrWhiteSpace(json) || json.StartsWith("<") || json.Contains("captcha"))
                return false;

            return ParseGoogleResponse(json, result);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TryMyMemoryApi(string text, string sl, string tl, TranslationResult result)
    {
        try
        {
            var from = MapToSimpleCode(sl);
            var to = MapToSimpleCode(tl);

            // MyMemory 不支持 auto，默认用 en 作为源语言（MyMemory 能处理中英混合）
            if (from == "auto") from = "en";

            var url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair={Uri.EscapeDataString(from)}|{Uri.EscapeDataString(to)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "Mozilla/5.0");

            var response = await HttpShared.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("responseData", out var responseData) &&
                responseData.TryGetProperty("translatedText", out var translatedText))
            {
                var translated = translatedText.GetString();
                if (!string.IsNullOrEmpty(translated) && !translated.Contains("INVALID") && translated != text)
                {
                    result.TranslatedText = translated;
                    result.IsSuccess = true;
                    result.Engine = Name;
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string MapToSimpleCode(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "auto" or "auto-detect" => "auto",
            "zh" or "zh-cn" or "chinese" => "zh",
            "en" or "english" => "en",
            "ja" or "japanese" => "ja",
            "ko" or "korean" => "ko",
            "fr" or "french" => "fr",
            "de" or "german" => "de",
            "es" or "spanish" => "es",
            "it" or "italian" => "it",
            "pt" or "portuguese" => "pt",
            "ru" or "russian" => "ru",
            "ar" or "arabic" => "ar",
            _ => code.Split('-')[0].ToLower()
        };
    }

    private static bool ParseGoogleResponse(string json, TranslationResult result)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("sentences", out var sentences))
            {
                var sb = new System.Text.StringBuilder();
                foreach (var s in sentences.EnumerateArray())
                {
                    if (s.TryGetProperty("trans", out var trans))
                    {
                        sb.Append(trans.GetString());
                    }
                }
                if (sb.Length > 0)
                {
                    result.TranslatedText = sb.ToString();
                    result.IsSuccess = true;
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    private static string MapLanguageCode(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "auto" or "auto-detect" => "auto",
            "zh-cn" or "zh" or "chinese" => "zh",
            "zh-tw" or "zh-hant" => "zh",
            "en" or "english" => "en",
            "ja" or "japanese" => "ja",
            "ko" or "korean" => "ko",
            "fr" or "french" => "fr",
            "de" or "german" => "de",
            "es" or "spanish" => "es",
            "it" or "italian" => "it",
            "pt" or "portuguese" => "pt",
            "ru" or "russian" => "ru",
            "ar" or "arabic" => "ar",
            _ => code.Split('-')[0].ToLower()
        };
    }
}
