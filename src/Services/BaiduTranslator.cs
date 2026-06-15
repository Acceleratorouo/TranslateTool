using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TranslateTool.Models;

namespace TranslateTool.Services;

public class BaiduTranslator : ITranslator
{
    public string Name => "百度翻译";

    private static string? _appId;
    private static string? _secretKey;

    public static void SetCredentials(string appId, string secretKey)
    {
        _appId = appId;
        _secretKey = secretKey;
    }

    public static bool HasCredentials => !string.IsNullOrEmpty(_appId) && !string.IsNullOrEmpty(_secretKey);

    public async Task<TranslationResult> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage)
    {
        if (HasCredentials)
        {
            return await TranslateWithApiAsync(text, sourceLanguage, targetLanguage);
        }
        return await TranslateWithWebAsync(text, sourceLanguage, targetLanguage);
    }

    private async Task<TranslationResult> TranslateWithApiAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        int maxRetries = 2,
        int retryDelayMs = 1500)
    {
        var result = new TranslationResult
        {
            SourceText = text,
            SourceLanguage = sourceLanguage,
            TargetLanguage = targetLanguage,
            Engine = Name
        };

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var salt = Random.Shared.Next(10000, 99999).ToString();
                var sign = ComputeMd5($"{_appId}{text}{salt}{_secretKey}");

                var parameters = new Dictionary<string, string>
                {
                    { "q", text },
                    { "from", MapLanguageCode(sourceLanguage) },
                    { "to", MapLanguageCode(targetLanguage) },
                    { "appid", _appId! },
                    { "salt", salt },
                    { "sign", sign }
                };

                var content = new FormUrlEncodedContent(parameters);
                var response = await HttpShared.Client.PostAsync(
                    "https://fanyi-api.baidu.com/api/trans/vip/translate", content);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (attempt < maxRetries)
                    {
                        var delay = retryDelayMs * (attempt + 1);
                        result.ErrorMessage = $"请求受限，{delay / 1000}秒后重试...";
                        await Task.Delay(TimeSpan.FromMilliseconds(delay));
                        continue;
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"百度API错误: HTTP {(int)response.StatusCode}";
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("error_code", out var errorCode))
                {
                    var errorMsg = doc.RootElement.TryGetProperty("error_msg", out var msg) ? msg.GetString() : "未知错误";
                    result.IsSuccess = false;
                    result.ErrorMessage = $"百度API错误 [{errorCode.GetString()}]: {errorMsg}";
                    return result;
                }

                if (doc.RootElement.TryGetProperty("trans_result", out var transResult))
                {
                    var sb = new StringBuilder();
                    foreach (var item in transResult.EnumerateArray())
                    {
                        if (item.TryGetProperty("dst", out var dst))
                        {
                            if (sb.Length > 0) sb.AppendLine();
                            sb.Append(dst.GetString());
                        }
                    }
                    result.TranslatedText = sb.ToString();
                    result.IsSuccess = true;
                    return result;
                }

                result.IsSuccess = false;
                result.ErrorMessage = "百度API返回格式异常";
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                if (attempt < maxRetries)
                {
                    await Task.Delay(retryDelayMs * (attempt + 1));
                }
            }
        }
        return result;
    }

    private async Task<TranslationResult> TranslateWithWebAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        int maxRetries = 2,
        int retryDelayMs = 1500)
    {
        var result = new TranslationResult
        {
            SourceText = text,
            SourceLanguage = sourceLanguage,
            TargetLanguage = targetLanguage,
            Engine = Name
        };

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var from = MapLanguageCode(sourceLanguage);
                var to = MapLanguageCode(targetLanguage);

                var url = "https://fanyi.baidu.com/transapi?" +
                          $"from={Uri.EscapeDataString(from)}" +
                          $"&to={Uri.EscapeDataString(to)}" +
                          $"&query={Uri.EscapeDataString(text)}" +
                          $"&source=web";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Referrer = new Uri("https://fanyi.baidu.com/");
                request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");

                var response = await HttpShared.Client.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    if (attempt < maxRetries)
                    {
                        var delay = retryDelayMs * (attempt + 1);
                        result.ErrorMessage = $"请求受限，{delay / 1000}秒后重试...";
                        await Task.Delay(TimeSpan.FromMilliseconds(delay));
                        continue;
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"百度翻译错误: HTTP {(int)response.StatusCode}";
                    return result;
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "百度翻译返回空结果";
                    return result;
                }

                if (responseBody.StartsWith("{") || responseBody.StartsWith("["))
                {
                    var doc = JsonDocument.Parse(responseBody);

                    if (doc.RootElement.TryGetProperty("result", out var resultObj) &&
                        resultObj.TryGetProperty("result", out var resultArr))
                    {
                        var sb = new StringBuilder();
                        foreach (var row in resultArr.EnumerateArray())
                        {
                            if (row.ValueKind == JsonValueKind.Array && row.GetArrayLength() > 0)
                            {
                                var inner = row[0];
                                if (inner.ValueKind == JsonValueKind.Array && inner.GetArrayLength() > 1)
                                {
                                    var translated = inner[1].GetString();
                                    if (!string.IsNullOrEmpty(translated))
                                    {
                                        if (sb.Length > 0) sb.AppendLine();
                                        sb.Append(translated);
                                    }
                                }
                            }
                        }
                        if (sb.Length > 0)
                        {
                            result.TranslatedText = sb.ToString();
                            result.IsSuccess = true;
                            return result;
                        }
                    }

                    if (doc.RootElement.TryGetProperty("data", out var dataArr) &&
                        dataArr.ValueKind == JsonValueKind.Array)
                    {
                        var sb = new StringBuilder();
                        foreach (var item in dataArr.EnumerateArray())
                        {
                            if (item.TryGetProperty("dst", out var dst))
                            {
                                if (sb.Length > 0) sb.AppendLine();
                                sb.Append(dst.GetString());
                            }
                        }
                        if (sb.Length > 0)
                        {
                            result.TranslatedText = sb.ToString();
                            result.IsSuccess = true;
                            return result;
                        }
                    }

                    if (doc.RootElement.TryGetProperty("trans_result", out var transArr) &&
                        transArr.ValueKind == JsonValueKind.Array)
                    {
                        var sb = new StringBuilder();
                        foreach (var item in transArr.EnumerateArray())
                        {
                            if (item.TryGetProperty("dst", out var dst))
                            {
                                if (sb.Length > 0) sb.AppendLine();
                                sb.Append(dst.GetString());
                            }
                        }
                        if (sb.Length > 0)
                        {
                            result.TranslatedText = sb.ToString();
                            result.IsSuccess = true;
                            return result;
                        }
                    }

                    result.IsSuccess = false;
                    result.ErrorMessage = "百度翻译返回格式异常，请尝试其他引擎或配置百度API密钥";
                    return result;
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "百度翻译返回非JSON数据，建议配置百度翻译API密钥";
                    return result;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                if (attempt < maxRetries)
                {
                    await Task.Delay(retryDelayMs * (attempt + 1));
                }
            }
        }
        return result;
    }

    private static string MapLanguageCode(string code)
    {
        return code.ToLowerInvariant() switch
        {
            "auto" or "auto-detect" => "auto",
            "zh-cn" or "zh" or "chinese" => "zh",
            "zh-tw" or "zh-hant" => "cht",
            "en" or "english" => "en",
            "ja" or "japanese" => "jp",
            "ko" or "korean" => "kor",
            "fr" or "french" => "fra",
            "de" or "german" => "de",
            "es" or "spanish" => "spa",
            "it" or "italian" => "it",
            "pt" or "portuguese" => "pt",
            "ru" or "russian" => "ru",
            "ar" or "arabic" => "ara",
            "th" or "thai" => "th",
            "vi" or "vietnamese" => "vie",
            _ => code.ToLowerInvariant()
        };
    }

    private static string ComputeMd5(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
