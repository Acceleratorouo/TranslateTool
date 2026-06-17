using Xunit;
using TranslateTool.Services;

namespace TranslateTool.Tests;

public class TranslatorFactoryTests
{
    [Fact]
    public void Create_WithBaidu_ReturnsBaiduTranslator()
    {
        var translator = TranslatorFactory.Create("baidu");
        Assert.NotNull(translator);
        Assert.IsType<BaiduTranslator>(translator);
        Assert.Equal("百度翻译", translator.Name);
    }

    [Fact]
    public void Create_WithGoogle_ReturnsGoogleTranslator()
    {
        var translator = TranslatorFactory.Create("google");
        Assert.NotNull(translator);
        Assert.IsType<GoogleTranslator>(translator);
    }

    [Fact]
    public void Create_WithMicrosoft_ReturnsMicrosoftTranslator()
    {
        var translator = TranslatorFactory.Create("microsoft");
        Assert.NotNull(translator);
        Assert.IsType<MicrosoftTranslator>(translator);
    }

    [Fact]
    public void Create_WithDeepL_ReturnsDeepLTranslator()
    {
        var translator = TranslatorFactory.Create("deepl");
        Assert.NotNull(translator);
        Assert.IsType<DeepLTranslator>(translator);
    }

    [Theory]
    [InlineData("BAIDU")]
    [InlineData("Baidu")]
    [InlineData(" baidu ")]
    [InlineData("百度翻译")]
    public void Create_WithCaseVariations_ReturnsBaiduTranslator(string engineName)
    {
        var translator = TranslatorFactory.Create(engineName);
        Assert.IsType<BaiduTranslator>(translator);
    }

    [Theory]
    [InlineData("GOOGLE")]
    [InlineData("Google Translate")]
    [InlineData("GOOGLE TRANSLATE")]
    public void Create_WithGoogleVariations_ReturnsGoogleTranslator(string engineName)
    {
        var translator = TranslatorFactory.Create(engineName);
        Assert.IsType<GoogleTranslator>(translator);
    }

    [Theory]
    [InlineData("MICROSOFT")]
    [InlineData("微软翻译")]
    public void Create_WithMicrosoftVariations_ReturnsMicrosoftTranslator(string engineName)
    {
        var translator = TranslatorFactory.Create(engineName);
        Assert.IsType<MicrosoftTranslator>(translator);
    }

    [Fact]
    public void Create_WithUnsupportedEngine_ThrowsNotSupportedException()
    {
        var ex = Assert.Throws<NotSupportedException>(() => TranslatorFactory.Create("unknown"));
        Assert.Contains("暂不支持", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhiteSpace_ThrowsArgumentException(string? engineName)
    {
        Assert.ThrowsAny<ArgumentException>(() => TranslatorFactory.Create(engineName!));
    }

    [Fact]
    public void GetAvailableEngines_ReturnsFourEngines()
    {
        var engines = TranslatorFactory.GetAvailableEngines();
        Assert.Equal(4, engines.Length);
        Assert.Contains("baidu", engines);
        Assert.Contains("google", engines);
        Assert.Contains("microsoft", engines);
        Assert.Contains("deepl", engines);
    }

    [Fact]
    public void Create_AllEngines_ImplementITranslator()
    {
        foreach (var engine in TranslatorFactory.GetAvailableEngines())
        {
            var translator = TranslatorFactory.Create(engine);
            Assert.NotNull(translator);
            Assert.False(string.IsNullOrWhiteSpace(translator.Name));
        }
    }
}
