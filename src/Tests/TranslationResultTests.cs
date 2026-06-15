using Xunit;
using TranslateTool.Models;

namespace TranslateTool.Tests;

public class TranslationResultTests
{
    [Fact]
    public void DefaultValues_AreEmpty()
    {
        var result = new TranslationResult();
        Assert.Equal("", result.SourceText);
        Assert.Equal("", result.TranslatedText);
        Assert.Equal("", result.SourceLanguage);
        Assert.Equal("", result.TargetLanguage);
        Assert.Equal("", result.Engine);
        Assert.False(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var result = new TranslationResult
        {
            SourceText = "Hello",
            TranslatedText = "你好",
            SourceLanguage = "en",
            TargetLanguage = "zh-CN",
            Engine = "百度翻译",
            IsSuccess = true,
            ErrorMessage = null
        };

        Assert.Equal("Hello", result.SourceText);
        Assert.Equal("你好", result.TranslatedText);
        Assert.Equal("en", result.SourceLanguage);
        Assert.Equal("zh-CN", result.TargetLanguage);
        Assert.Equal("百度翻译", result.Engine);
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void FailedResult_HasErrorMessage()
    {
        var result = new TranslationResult
        {
            IsSuccess = false,
            ErrorMessage = "网络超时"
        };

        Assert.False(result.IsSuccess);
        Assert.Equal("网络超时", result.ErrorMessage);
    }
}
