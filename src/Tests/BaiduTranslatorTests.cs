using Xunit;
using System.Reflection;
using TranslateTool.Services;

namespace TranslateTool.Tests;

public class BaiduTranslatorTests
{
    [Fact]
    public void Name_ReturnsBaiduTranslate()
    {
        var translator = new BaiduTranslator();
        Assert.Equal("百度翻译", translator.Name);
    }

    [Fact]
    public void ImplementsITranslator()
    {
        var translator = new BaiduTranslator();
        Assert.IsAssignableFrom<ITranslator>(translator);
    }

    [Fact]
    public void HasCredentials_WithoutSetting_ReturnsFalse()
    {
        // 重置凭据（通过反射）
        ResetCredentials();
        Assert.False(BaiduTranslator.HasCredentials);
    }

    [Fact]
    public void SetCredentials_WithValidValues_SetsHasCredentials()
    {
        try
        {
            BaiduTranslator.SetCredentials("test-app-id", "test-secret");
            Assert.True(BaiduTranslator.HasCredentials);
        }
        finally
        {
            ResetCredentials();
        }
    }

    [Fact]
    public void SetCredentials_WithEmptyValues_SetsHasCredentialsFalse()
    {
        try
        {
            BaiduTranslator.SetCredentials("", "");
            Assert.False(BaiduTranslator.HasCredentials);
        }
        finally
        {
            ResetCredentials();
        }
    }

    [Fact]
    public void MapLanguageCode_Auto_ReturnsAuto()
    {
        var result = InvokeMapLanguageCode("auto");
        Assert.Equal("auto", result);
    }

    [Fact]
    public void MapLanguageCode_AutoDetect_ReturnsAuto()
    {
        var result = InvokeMapLanguageCode("auto-detect");
        Assert.Equal("auto", result);
    }

    [Theory]
    [InlineData("zh-CN", "zh")]
    [InlineData("zh", "zh")]
    [InlineData("ZH", "zh")]
    [InlineData("chinese", "zh")]
    [InlineData("zh-TW", "cht")]
    [InlineData("zh-Hant", "cht")]
    public void MapLanguageCode_Chinese_ReturnsCorrectCode(string input, string expected)
    {
        Assert.Equal(expected, InvokeMapLanguageCode(input));
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("EN", "en")]
    [InlineData("english", "en")]
    public void MapLanguageCode_English_ReturnsEn(string input, string expected)
    {
        Assert.Equal(expected, InvokeMapLanguageCode(input));
    }

    [Theory]
    [InlineData("ja", "jp")]
    [InlineData("japanese", "jp")]
    [InlineData("ko", "kor")]
    [InlineData("korean", "kor")]
    [InlineData("fr", "fra")]
    [InlineData("french", "fra")]
    [InlineData("de", "de")]
    [InlineData("german", "de")]
    [InlineData("es", "spa")]
    [InlineData("spanish", "spa")]
    [InlineData("it", "it")]
    [InlineData("italian", "it")]
    [InlineData("pt", "pt")]
    [InlineData("portuguese", "pt")]
    [InlineData("ru", "ru")]
    [InlineData("russian", "ru")]
    [InlineData("ar", "ara")]
    [InlineData("arabic", "ara")]
    [InlineData("th", "th")]
    [InlineData("thai", "th")]
    [InlineData("vi", "vie")]
    [InlineData("vietnamese", "vie")]
    public void MapLanguageCode_OtherLanguages_ReturnsCorrectCode(string input, string expected)
    {
        Assert.Equal(expected, InvokeMapLanguageCode(input));
    }

    [Fact]
    public void MapLanguageCode_Unknown_ReturnsLowercase()
    {
        Assert.Equal("xyz", InvokeMapLanguageCode("XYZ"));
        Assert.Equal("unknown-lang", InvokeMapLanguageCode("unknown-lang"));
    }

    /// <summary>
    /// 通过反射调用私有静态方法 MapLanguageCode
    /// </summary>
    private static string InvokeMapLanguageCode(string code)
    {
        var method = typeof(BaiduTranslator).GetMethod("MapLanguageCode",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, [code])!;
    }

    /// <summary>
    /// 重置静态凭据字段
    /// </summary>
    private static void ResetCredentials()
    {
        BaiduTranslator.SetCredentials("", "");
    }
}
