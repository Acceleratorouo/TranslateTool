using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TranslateTool.Models;

namespace TranslateTool.Services;

/// <summary>
/// OpenAI 兼容协议的 LLM 客户端实现
///
/// 提供的核心能力：
/// - <see cref="ChatCompletionAsync"/>：发起 chat/completions 请求并返回文本结果
/// - <see cref="ListModelsAsync"/>：拉取服务端可用模型列表
///
/// 构造时会根据 <see cref="LlmProvider"/> 配置设置 BaseAddress 与鉴权头。
/// </summary>
public class OpenAiCompatibleClient : ILlmClient, IDisposable
{
    private readonly HttpClient _client;

    /// <summary>
    /// 构造客户端实例。
    /// </summary>
    /// <param name="provider">LLM 提供商配置，用于设置 BaseAddress 与鉴权头。</param>
    /// <param name="timeout">HTTP 请求超时时间，默认 60 秒。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="provider"/> 为 null 时抛出。</exception>
    public OpenAiCompatibleClient(LlmProvider provider, TimeSpan? timeout = null)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        _client = new HttpClient
        {
            BaseAddress = new Uri(provider.BaseUrl.TrimEnd('/') + "/"),
            Timeout = timeout ?? TimeSpan.FromSeconds(60)
        };

        if (!string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            _client.DefaultRequestHeaders.Add(
                provider.AuthHeader,
                $"{provider.AuthPrefix} {provider.ApiKey}".Trim());
        }
    }

    /// <summary>
    /// 调用 OpenAI 兼容的 chat/completions 接口，返回模型生成的文本内容。
    /// </summary>
    /// <param name="model">目标模型 Id。</param>
    /// <param name="systemPrompt">系统提示词。</param>
    /// <param name="userPrompt">用户输入。</param>
    /// <param name="temperature">采样温度。</param>
    /// <param name="maxTokens">最大生成 token 数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>模型生成的文本内容；若响应中未包含 content 字段则返回 null。</returns>
    /// <exception cref="HttpRequestException">当 API 请求返回非成功状态码时抛出，异常消息包含响应体。</exception>
    /// <exception cref="InvalidOperationException">当 API 返回的响应无法解析为 JSON 时抛出。</exception>
    public async Task<string?> ChatCompletionAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        double temperature,
        int maxTokens,
        CancellationToken ct = default)
    {
        var request = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature,
            max_tokens = maxTokens
        };

        var response = await _client.PostAsJsonAsync("chat/completions", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"API 请求失败: {response.StatusCode} - {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"API 返回了无法解析的响应: {json[..Math.Min(200, json.Length)]}", ex);
        }
        using (doc)
        {
            if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString();
            }

            return null;
        }
    }

    /// <summary>
    /// 拉取服务端可用模型列表（GET /models）。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>模型 Id 列表，已过滤空白项。</returns>
    /// <exception cref="HttpRequestException">当 API 请求返回非成功状态码时抛出，异常消息包含响应体。</exception>
    /// <exception cref="InvalidOperationException">当 API 返回的响应无法解析为 JSON 时抛出。</exception>
    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default)
    {
        var response = await _client.GetAsync("models", ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"API 请求失败: {response.StatusCode} - {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"API 返回了无法解析的响应: {json[..Math.Min(200, json.Length)]}", ex);
        }
        using (doc)
        {
            var models = new List<string>();
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in data.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var id))
                    {
                        models.Add(id.GetString() ?? "");
                    }
                }
            }

            return models.Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _client.Dispose();
    }
}
