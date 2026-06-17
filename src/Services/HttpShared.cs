using System.Net.Http;

namespace TranslateTool.Services;

/// <summary>
/// 全局共享的 HttpClient 实例，避免 socket 耗尽
/// </summary>
public static class HttpShared
{
    private static readonly Lazy<HttpClient> _lazy = new(() =>
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            MaxConnectionsPerServer = 8
        };
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        return client;
    });

    public static HttpClient Client => _lazy.Value;
}

/// <summary>
/// 翻译引擎状态信息
/// </summary>
public class EngineStatus
{
    public string Name { get; init; } = "";
    public string Label { get; init; } = "";
    public bool IsStable { get; init; }
    public string? Note { get; init; }

    /// <summary>
    /// 获取所有引擎的状态
    /// </summary>
    public static EngineStatus[] GetAll()
    {
        return
        [
            new EngineStatus
            {
                Name = "baidu",
                Label = "百度翻译",
                IsStable = BaiduTranslator.HasCredentials,
                Note = BaiduTranslator.HasCredentials ? "已配置API密钥，稳定可用" : "免费Web接口（推荐配置API密钥获得稳定服务）"
            },
            new EngineStatus
            {
                Name = "google",
                Label = "Google 翻译",
                IsStable = false,
                Note = "免费接口有频率限制，可能返回429错误"
            },
            new EngineStatus
            {
                Name = "microsoft",
                Label = "微软翻译",
                IsStable = false,
                Note = "免费接口不稳定，可能返回HTML或403错误"
            },
            new EngineStatus
            {
                Name = "deepl",
                Label = "DeepL 翻译",
                IsStable = DeepLTranslator.HasApiKey,
                Note = DeepLTranslator.HasApiKey ? "已配置API Key，翻译质量优秀" : "需要配置API Key（免费版500,000字符/月）"
            },
            new EngineStatus
            {
                Name = "ai",
                Label = "AI 翻译",
                IsStable = LlmProviderService.GetDefaultProvider()?.IsEnabled == true,
                Note = LlmProviderService.GetDefaultProvider() is { } p
                    ? $"默认供应商: {p.DisplayName}"
                    : "需先在设置中配置 AI 翻译供应商"
            }
        ];
    }

    /// <summary>
    /// 获取引擎状态的显示文本
    /// </summary>
    public string GetStatusIcon() => IsStable ? "✅" : "⚠️";

    public override string ToString() => $"{GetStatusIcon()} {Label} — {Note}";
}
