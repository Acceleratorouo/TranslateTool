using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TranslateTool.Models;

namespace TranslateTool.Services;

public class MicrosoftTranslator : ITranslator
{
    public string Name => "微软翻译";

    // 缓存 Token
    private static string? _cachedToken;
    private static string? _cachedKey;
    private static DateTime _tokenExpiry = DateTime.MinValue;
    private static readonly object _tokenLock = new();

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

        var from = MapLanguageCode(sourceLanguage);
        var to = MapLanguageCode(targetLanguage);

        // 方式1: Bing ttranslatev3
        if (await TryBingTranslate(text, from, to, result))
            return result;

        // 方式2: MyMemory API (可靠的免费备用)
        if (await TryMyMemoryApi(text, sourceLanguage, targetLanguage, result))
            return result;

        result.IsSuccess = false;
        result.ErrorMessage = "翻译服务暂时不可用，请使用百度或 DeepL 引擎";
        return result;
    }

    private async Task<bool> TryBingTranslate(string text, string from, string to, TranslationResult result)
    {
        try
        {
            var (token, key) = await GetBingToken();
            if (string.IsNullOrEmpty(token))
                return false;

            var url = "https://www.bing.com/ttranslatev3";

            var parameters = new Dictionary<string, string>
            {
                { "fromLang", from },
                { "to", to },
                { "text", text },
                { "token", token },
                { "key", key ?? DateTime.UtcNow.Millisecond.ToString() }
            };

            var content = new FormUrlEncodedContent(parameters);
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Referrer = new Uri("https://www.bing.com/translator");
            request.Headers.Add("Accept", "application/json");

            var session = new CookieContainer();
            var handler = new HttpClientHandler { CookieContainer = session };
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            // 先获取 cookies
            await client.GetAsync("https://www.bing.com/translator");

            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
                return false;

            var responseBody = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseBody) || responseBody.Contains("statusCode") && !responseBody.Contains("translations"))
                return false;

            return ParseBingResponse(responseBody, result);
        }
        catch
        {
            return false;
        }
    }

    private async Task<(string token, string key)> GetBingToken()
    {
        lock (_tokenLock)
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.Now < _tokenExpiry)
            {
                return (_cachedToken, _cachedKey ?? "");
            }
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.bing.com/translator");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            var response = await HttpShared.Client.SendAsync(request);
            var html = await response.Content.ReadAsStringAsync();

            var tokenMatch = Regex.Match(html, @"params_AbusePreventionHelper\s*=\s*\[(\d+),""([^""]+)"",(\d+)\]");

            if (tokenMatch.Success)
            {
                var token = tokenMatch.Groups[2].Value;
                var key = tokenMatch.Groups[1].Value;

                lock (_tokenLock)
                {
                    _cachedToken = token;
                    _cachedKey = key;
                    _tokenExpiry = DateTime.Now.AddMinutes(10);
                }

                return (token, key);
            }

            return ("", "");
        }
        catch
        {
            return ("", "");
        }
    }

    private async Task<bool> TryMyMemoryApi(string text, string sourceLanguage, string targetLanguage, TranslationResult result)
    {
        try
        {
            var from = MapToSimpleCode(sourceLanguage);
            var to = MapToSimpleCode(targetLanguage);

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

    private static bool ParseBingResponse(string responseBody, TranslationResult result)
    {
        try
        {
            var json = JsonDocument.Parse(responseBody);

            if (json.RootElement.ValueKind == JsonValueKind.Array && json.RootElement.GetArrayLength() > 0)
            {
                var first = json.RootElement[0];
                if (first.TryGetProperty("translations", out var translations) && translations.GetArrayLength() > 0)
                {
                    var translated = translations[0].GetProperty("text").GetString();
                    if (!string.IsNullOrEmpty(translated))
                    {
                        result.TranslatedText = translated;
                        result.IsSuccess = true;
                        return true;
                    }
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string MapLanguageCode(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "auto" or "auto-detect" => "auto-detect",
            "zh-cn" or "zh" or "chinese" => "zh-Hans",
            "zh-tw" or "zh-hant" => "zh-Hant",
            "en" or "english" => "en",
            "ja" or "japanese" => "ja",
            "ko" or "korean" => "ko",
            "fr" or "french" => "fr",
            "de" or "german" => "de",
            "es" or "spanish" => "es",
            _ => code
        };
    }

    private static string MapToSimpleCode(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "auto" or "auto-detect" => "en",
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
