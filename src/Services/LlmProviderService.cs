using TranslateTool.Models;

namespace TranslateTool.Services;

/// <summary>
/// LLM 提供商管理服务
///
/// 提供的核心能力：
/// - <see cref="EnsureDefaultTemplate"/>：首次启动时确保存在默认 Ollama 模板
/// - <see cref="GetBuiltInTemplates"/>：获取内置提供商模板列表
/// - <see cref="SetDefaultProvider"/>：设置默认提供商
/// - <see cref="GetDefaultProvider"/>：获取当前默认或第一个启用的提供商
/// - <see cref="SaveProviders"/>：持久化提供商配置
///
/// 所有状态都存储在 <see cref="AppSettings.Current"/> 单例中。
/// </summary>
public static class LlmProviderService
{
    /// <summary>
    /// 当前已配置的 LLM 提供商列表（来自 <see cref="AppSettings.Current"/>）。
    /// </summary>
    public static List<LlmProvider> Providers => AppSettings.Current.LlmProviders;

    /// <summary>
    /// AI 翻译相关设置（来自 <see cref="AppSettings.Current"/>）。
    /// </summary>
    public static AiTranslationSettings Settings => AppSettings.Current.AiTranslationSettings;

    /// <summary>
    /// 当提供商列表为空时，添加一个默认的 Ollama 模板（默认禁用）并保存配置。
    /// </summary>
    public static void EnsureDefaultTemplate()
    {
        if (Providers.Count == 0)
        {
            Providers.Add(CreateOllamaTemplate(enabled: false));
            AppSettings.Current.Save();
        }
    }

    /// <summary>
    /// 创建一个 Ollama 本地服务提供商模板。
    /// </summary>
    /// <param name="enabled">是否启用该模板。注意：与 <see cref="LlmProvider.IsDefault"/> 相互独立，模板默认不是默认提供商。</param>
    /// <returns>未保存到配置的 Ollama 提供商实例。</returns>
    public static LlmProvider CreateOllamaTemplate(bool enabled = false)
    {
        return new LlmProvider
        {
            DisplayName = "Ollama (Local)",
            Notes = "本地 Ollama 服务，默认部署 Gemma 4 E4B",
            HomepageUrl = "https://ollama.com",
            BaseUrl = "http://localhost:11434/v1",
            ApiFormat = LlmApiFormat.OpenAiCompatible,
            IsEnabled = enabled,
            IsDefault = false
        };
    }

    /// <summary>
    /// 获取内置的提供商模板列表（Ollama、OpenAI、OpenRouter、Gemini、DeepSeek、SiliconFlow 等）。
    /// </summary>
    /// <returns>内置模板的新列表，调用方可自由修改。</returns>
    public static List<LlmProvider> GetBuiltInTemplates()
    {
        return new List<LlmProvider>
        {
            CreateOllamaTemplate(enabled: false),
            new LlmProvider
            {
                DisplayName = "OpenAI",
                HomepageUrl = "https://platform.openai.com",
                BaseUrl = "https://api.openai.com/v1",
                ApiFormat = LlmApiFormat.OpenAiCompatible
            },
            new LlmProvider
            {
                DisplayName = "OpenRouter",
                HomepageUrl = "https://openrouter.ai",
                BaseUrl = "https://openrouter.ai/api/v1",
                ApiFormat = LlmApiFormat.OpenAiCompatible
            },
            new LlmProvider
            {
                DisplayName = "Google Gemini",
                HomepageUrl = "https://aistudio.google.com",
                BaseUrl = "https://generativelanguage.googleapis.com/v1beta",
                ApiFormat = LlmApiFormat.Gemini
            },
            new LlmProvider
            {
                DisplayName = "Anthropic Claude",
                Notes = "Anthropic Messages 原生 API，需 x-api-key 鉴权",
                HomepageUrl = "https://console.anthropic.com",
                BaseUrl = "https://api.anthropic.com/v1",
                ApiFormat = LlmApiFormat.Anthropic,
                AuthHeader = "x-api-key",
                AuthPrefix = ""
            },
            new LlmProvider
            {
                DisplayName = "DeepSeek",
                HomepageUrl = "https://platform.deepseek.com",
                BaseUrl = "https://api.deepseek.com/v1",
                ApiFormat = LlmApiFormat.OpenAiCompatible
            },
            new LlmProvider
            {
                DisplayName = "SiliconFlow",
                HomepageUrl = "https://siliconflow.cn",
                BaseUrl = "https://api.siliconflow.cn/v1",
                ApiFormat = LlmApiFormat.OpenAiCompatible
            },
            new LlmProvider
            {
                DisplayName = "智谱 GLM (Zhipu)",
                Notes = "智谱 AI 开放平台，GLM-4 系列模型",
                HomepageUrl = "https://open.bigmodel.cn",
                BaseUrl = "https://open.bigmodel.cn/api/paas/v4",
                ApiFormat = LlmApiFormat.OpenAiCompatible
            }
        };
    }

    /// <summary>
    /// 将指定提供商标记为默认，并清除其他提供商的默认标记，最后持久化配置。
    /// </summary>
    /// <param name="providerId">要设为默认的提供商 Id。</param>
    public static void SetDefaultProvider(string providerId)
    {
        foreach (var provider in Providers)
        {
            provider.IsDefault = provider.Id == providerId;
        }

        Settings.DefaultProviderId = providerId;
        AppSettings.Current.Save();
    }

    /// <summary>
    /// 删除指定提供商，并在删除默认提供商时清理默认提供商配置。
    /// </summary>
    /// <param name="provider">要删除的提供商。</param>
    public static void DeleteProvider(LlmProvider provider)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        var wasDefault = provider.IsDefault || Settings.DefaultProviderId == provider.Id;
        Providers.Remove(provider);

        if (wasDefault)
        {
            Settings.DefaultProviderId = null;
        }

        AppSettings.Current.Save();
    }

    /// <summary>
    /// 获取默认提供商：优先返回 <see cref="LlmProvider.IsDefault"/> 为 true 的提供商；
    /// 若不存在，则返回第一个 <see cref="LlmProvider.IsEnabled"/> 为 true 的提供商；否则返回 null。
    /// </summary>
    /// <returns>默认或第一个启用的提供商；若都没有则返回 null。</returns>
    public static LlmProvider? GetDefaultProvider()
    {
        return Providers.FirstOrDefault(p => p.IsDefault)
            ?? Providers.FirstOrDefault(p => p.IsEnabled);
    }

    /// <summary>
    /// 持久化当前提供商配置到 <see cref="AppSettings"/>。
    /// </summary>
    public static void SaveProviders()
    {
        AppSettings.Current.Save();
    }

    /// <summary>
    /// 测试供应商连接并获取模型列表
    /// </summary>
    /// <param name="provider">要测试的供应商</param>
    /// <param name="ct">取消令牌</param>
    public static async Task TestProviderAsync(LlmProvider provider, CancellationToken ct = default)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (provider.ApiFormat == LlmApiFormat.Gemini)
        {
            throw new NotSupportedException("Gemini API 格式暂不支持测试连接，请使用 OpenAI-compatible 格式。");
        }

        ILlmClient client = provider.ApiFormat == LlmApiFormat.Anthropic
            ? new AnthropicClient(provider, TimeSpan.FromSeconds(15))
            : new OpenAiCompatibleClient(provider, TimeSpan.FromSeconds(15));

        using (client)
        {
            var models = await client.ListModelsAsync(ct);
            provider.Models = models.ToList();
        }
    }
}
