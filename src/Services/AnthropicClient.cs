using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using TranslateTool.Models;

namespace TranslateTool.Services;

/// <summary>
/// Anthropic Messages API 原生客户端实现
///
/// 提供的核心能力：
/// - <see cref="ChatCompletionAsync"/>：发起 /v1/messages 请求并返回文本结果
/// - <see cref="ListModelsAsync"/>：返回 Anthropic 当前可用模型列表（Anthropic 不提供 /models 端点）
///
/// 构造时会根据 <see cref="LlmProvider"/> 配置设置 BaseAddress 与 x-api-key 鉴权头。
/// </summary>
public class AnthropicClient : ILlmClient, IDisposable
{
    private const string AnthropicVersion = "2023-06-01";

    private static readonly string[] KnownModels =
    {
        "claude-opus-4-0-20250514",
        "claude-sonnet-4-0-20250514",
        "claude-3-7-sonnet-20250219",
        "claude-3-5-sonnet-20241022",
        "claude-3-5-haiku-20241022",
        "claude-3-opus-20240229",
        "claude-3-haiku-20240307"
    };

    private readonly HttpClient _client;

    /// <summary>
    /// 构造客户端实例。
    /// </summary>
    /// <param name="provider">LLM 提供商配置，用于设置 BaseAddress 与 x-api-key 鉴权头。</param>
    /// <param name="timeout">HTTP 请求超时时间，默认 60 秒。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="provider"/> 为 null 时抛出。</exception>
    public AnthropicClient(LlmProvider provider, TimeSpan? timeout = null)
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

        // Anthropic 使用 x-api-key 头，不使用 Bearer 前缀
        if (!string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            _client.DefaultRequestHeaders.Add("x-api-key", provider.ApiKey.Trim());
        }

        // 必需的版本头
        _client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
    }

    /// <summary>
    /// 调用 Anthropic Messages API（POST /v1/messages），返回模型生成的文本内容。
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
            max_tokens = maxTokens,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            },
            temperature
        };

        var response = await _client.PostAsJsonAsync("messages", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Anthropic API 请求失败: {response.StatusCode} - {errorBody}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Anthropic API 返回了无法解析的响应: {json[..Math.Min(200, json.Length)]}", ex);
        }
        using (doc)
        {
            if (doc.RootElement.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.Array &&
                content.GetArrayLength() > 0)
            {
                // 拼接所有 text 类型的内容块
                var sb = new System.Text.StringBuilder();
                foreach (var block in content.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var type) &&
                        type.GetString() == "text" &&
                        block.TryGetProperty("text", out var text))
                    {
                        sb.Append(text.GetString());
                    }
                }
                return sb.Length > 0 ? sb.ToString() : null;
            }

            return null;
        }
    }

    /// <summary>
    /// 返回 Anthropic 当前可用模型列表。
    /// 注意：Anthropic 不提供 /models 端点，这里返回硬编码的已知模型列表。
    /// </summary>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已知模型 Id 列表。</returns>
    public Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<string> models = KnownModels;
        return Task.FromResult(models);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _client.Dispose();
    }
}
