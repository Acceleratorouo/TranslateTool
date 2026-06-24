using TranslateTool.Models;
using TranslateTool.Services;
using Xunit;

namespace TranslateTool.Tests;

[Collection("UserData")]
public class LlmProviderServiceTests
{
    public LlmProviderServiceTests()
    {
        AppSettings.Reset();
    }

    [Fact]
    public void EnsureDefaultTemplate_AddsOllamaTemplate_WhenEmpty()
    {
        LlmProviderService.EnsureDefaultTemplate();

        Assert.Single(LlmProviderService.Providers);
        Assert.Equal("Ollama (Local)", LlmProviderService.Providers[0].DisplayName);
        Assert.False(LlmProviderService.Providers[0].IsEnabled);
    }

    [Fact]
    public void GetBuiltInTemplates_ContainsCommonProviders()
    {
        var templates = LlmProviderService.GetBuiltInTemplates();

        Assert.Contains(templates, t => t.DisplayName == "Ollama (Local)");
        Assert.Contains(templates, t => t.DisplayName == "OpenAI");
        Assert.Contains(templates, t => t.DisplayName == "OpenRouter");
        Assert.Contains(templates, t => t.DisplayName == "Anthropic Claude");
        Assert.Contains(templates, t => t.ApiFormat == LlmApiFormat.Anthropic);
        Assert.Contains(templates, t => t.DisplayName == "智谱 GLM (Zhipu)");
    }

    [Fact]
    public void SetDefaultProvider_UpdatesDefaultFlag()
    {
        var p1 = new LlmProvider { Id = "p1", DisplayName = "P1" };
        var p2 = new LlmProvider { Id = "p2", DisplayName = "P2" };
        LlmProviderService.Providers.Add(p1);
        LlmProviderService.Providers.Add(p2);

        LlmProviderService.SetDefaultProvider("p2");

        Assert.False(p1.IsDefault);
        Assert.True(p2.IsDefault);
        Assert.Equal("p2", LlmProviderService.Settings.DefaultProviderId);
    }

    [Fact]
    public void GetDefaultProvider_ReturnsDefaultOrFirstEnabled()
    {
        var p1 = new LlmProvider { Id = "p1", DisplayName = "P1", IsEnabled = true };
        LlmProviderService.Providers.Add(p1);

        var result = LlmProviderService.GetDefaultProvider();

        Assert.Equal("p1", result?.Id);
    }

    [Fact]
    public void GetDefaultProvider_PrefersDefaultOverEnabled()
    {
        var p1 = new LlmProvider { Id = "p1", DisplayName = "P1", IsEnabled = true };
        var p2 = new LlmProvider { Id = "p2", DisplayName = "P2", IsEnabled = true, IsDefault = true };
        LlmProviderService.Providers.Add(p1);
        LlmProviderService.Providers.Add(p2);

        var result = LlmProviderService.GetDefaultProvider();

        Assert.Equal("p2", result?.Id);
    }

    [Fact]
    public void DeleteProvider_ClearsDefaultProviderId_WhenDeletingDefault()
    {
        var provider = new LlmProvider { Id = "p1", DisplayName = "P1", IsDefault = true };
        LlmProviderService.Providers.Add(provider);
        LlmProviderService.Settings.DefaultProviderId = provider.Id;

        LlmProviderService.DeleteProvider(provider);

        Assert.Empty(LlmProviderService.Providers);
        Assert.Null(LlmProviderService.Settings.DefaultProviderId);
    }

    [Fact]
    public void DeleteProvider_KeepsDefaultProviderId_WhenDeletingNonDefault()
    {
        var defaultProvider = new LlmProvider { Id = "default", DisplayName = "Default", IsDefault = true };
        var otherProvider = new LlmProvider { Id = "other", DisplayName = "Other" };
        LlmProviderService.Providers.Add(defaultProvider);
        LlmProviderService.Providers.Add(otherProvider);
        LlmProviderService.Settings.DefaultProviderId = defaultProvider.Id;

        LlmProviderService.DeleteProvider(otherProvider);

        Assert.Single(LlmProviderService.Providers);
        Assert.Equal(defaultProvider.Id, LlmProviderService.Settings.DefaultProviderId);
    }
}
