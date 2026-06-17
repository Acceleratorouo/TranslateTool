using System.Net.Http;
using TranslateTool.Models;
using TranslateTool.Services;
using Xunit;

namespace TranslateTool.Tests;

public class OpenAiCompatibleClientTests
{
    [Fact]
    public void Constructor_WithValidProvider_CreatesClient()
    {
        var provider = new LlmProvider
        {
            DisplayName = "Test",
            BaseUrl = "http://localhost:11434/v1",
            ApiKey = "test-key"
        };

        using var client = new OpenAiCompatibleClient(provider);
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithNullProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OpenAiCompatibleClient(null!));
    }

    [Fact]
    public void Constructor_WithoutApiKey_DoesNotAddAuthHeader()
    {
        var provider = new LlmProvider
        {
            DisplayName = "Test",
            BaseUrl = "http://localhost:11434/v1",
            ApiKey = null
        };

        using var client = new OpenAiCompatibleClient(provider);
        // Use reflection to verify no auth header was added
        var httpClientField = typeof(OpenAiCompatibleClient).GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var httpClient = (HttpClient)httpClientField!.GetValue(client)!;
        Assert.False(httpClient.DefaultRequestHeaders.Contains("Authorization"));
    }

    [Fact]
    public void Constructor_WithApiKey_AddsAuthHeader()
    {
        var provider = new LlmProvider
        {
            DisplayName = "Test",
            BaseUrl = "http://localhost:11434/v1",
            ApiKey = "test-key",
            AuthHeader = "Authorization",
            AuthPrefix = "Bearer"
        };

        using var client = new OpenAiCompatibleClient(provider);
        var httpClientField = typeof(OpenAiCompatibleClient).GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var httpClient = (HttpClient)httpClientField!.GetValue(client)!;
        Assert.True(httpClient.DefaultRequestHeaders.Contains("Authorization"));
        var headerValue = httpClient.DefaultRequestHeaders.GetValues("Authorization").First();
        Assert.Equal("Bearer test-key", headerValue);
    }
}
