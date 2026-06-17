using TranslateTool.Models;
using TranslateTool.Services;
using Xunit;

namespace TranslateTool.Tests;

[Collection("UserData")]
public class AiTranslatorTests
{
    public AiTranslatorTests()
    {
        AppSettings.Reset();
    }

    [Fact]
    public async Task TranslateAsync_ReturnsError_WhenNoProvider()
    {
        var translator = new AiTranslator();

        var result = await translator.TranslateAsync("hello", "en", "zh-CN");

        Assert.False(result.IsSuccess);
        Assert.Contains("未配置", result.ErrorMessage);
    }

    [Fact]
    public async Task TranslateAsync_ReturnsError_WhenEmptyText()
    {
        var translator = new AiTranslator();

        var result = await translator.TranslateAsync("", "en", "zh-CN");

        Assert.False(result.IsSuccess);
        Assert.Contains("文本为空", result.ErrorMessage);
    }

    [Fact]
    public void Name_ReturnsAiTranslation()
    {
        var translator = new AiTranslator();
        Assert.Equal("AI 翻译", translator.Name);
    }

    [Fact]
    public async Task TranslateAsync_ReturnsError_WhenProviderDisabled()
    {
        var provider = new LlmProvider
        {
            Id = "test-disabled",
            DisplayName = "Test Disabled",
            IsEnabled = false
        };
        LlmProviderService.Providers.Add(provider);

        var translator = new AiTranslator();
        var result = await translator.TranslateAsync("hello", "en", "zh-CN");

        Assert.False(result.IsSuccess);
        Assert.Contains("未配置", result.ErrorMessage);
    }

    [Fact]
    public async Task TranslateAsync_ReturnsError_WhenNoModelSelected()
    {
        var provider = new LlmProvider
        {
            Id = "test-no-model",
            DisplayName = "Test No Model",
            IsEnabled = true,
            IsDefault = true
        };
        LlmProviderService.Providers.Add(provider);
        // DefaultModel is null, provider.Models is empty

        var translator = new AiTranslator();
        var result = await translator.TranslateAsync("hello", "en", "zh-CN");

        Assert.False(result.IsSuccess);
        Assert.Contains("未选择", result.ErrorMessage);
    }

    [Fact]
    public async Task TranslateAsync_ReturnsError_WhenGeminiProvider()
    {
        var provider = new LlmProvider
        {
            Id = "test-gemini",
            DisplayName = "Test Gemini",
            IsEnabled = true,
            IsDefault = true,
            ApiFormat = LlmApiFormat.Gemini,
            Models = new List<string> { "gemini-pro" }
        };
        LlmProviderService.Providers.Add(provider);
        LlmProviderService.Settings.DefaultModel = "gemini-pro";

        var translator = new AiTranslator();
        var result = await translator.TranslateAsync("hello", "en", "zh-CN");

        Assert.False(result.IsSuccess);
        Assert.Contains("Gemini", result.ErrorMessage);
    }

    [Fact]
    public void AiTranslator_ImplementsITranslator()
    {
        var translator = new AiTranslator();
        Assert.IsAssignableFrom<ITranslator>(translator);
    }

    [Fact]
    public async Task TranslateAsync_ReturnsError_WhenAnthropicProviderWithoutApiKey()
    {
        // Anthropic provider without API key will fail at HTTP request level
        var provider = new LlmProvider
        {
            Id = "test-anthropic",
            DisplayName = "Test Anthropic",
            IsEnabled = true,
            IsDefault = true,
            ApiFormat = LlmApiFormat.Anthropic,
            BaseUrl = "https://api.anthropic.com/v1",
            Models = new List<string> { "claude-3-5-haiku-20241022" }
        };
        LlmProviderService.Providers.Add(provider);
        LlmProviderService.Settings.DefaultModel = "claude-3-5-haiku-20241022";
        LlmProviderService.Settings.TimeoutSeconds = 5;

        var translator = new AiTranslator();
        var result = await translator.TranslateAsync("hello", "en", "zh-CN");

        // Should fail with HTTP error (no API key), not NotSupportedException
        Assert.False(result.IsSuccess);
        Assert.DoesNotContain("不支持", result.ErrorMessage);
    }
}
