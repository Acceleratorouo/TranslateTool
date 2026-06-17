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
}
