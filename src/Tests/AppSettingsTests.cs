using Xunit;
using TranslateTool.Models;

namespace TranslateTool.Tests;

public class AppSettingsTests
{
    [Fact]
    public void Current_ReturnsSameInstance()
    {
        var settings1 = AppSettings.Current;
        var settings2 = AppSettings.Current;
        Assert.Same(settings1, settings2);
    }

    [Fact]
    public void DefaultSourceLanguage_IsAuto()
    {
        Assert.Equal("auto", AppSettings.Current.SourceLanguage);
    }

    [Fact]
    public void DefaultTargetLanguage_IsZhCN()
    {
        Assert.Equal("zh-CN", AppSettings.Current.TargetLanguage);
    }

    [Fact]
    public void DefaultTranslationEngine_IsBaidu()
    {
        Assert.Equal("baidu", AppSettings.Current.TranslationEngine);
    }

    [Fact]
    public void DefaultShowFloatingWindow_IsTrue()
    {
        Assert.True(AppSettings.Current.ShowFloatingWindow);
    }

    [Fact]
    public void DefaultFloatingWindowAlwaysOnTop_IsTrue()
    {
        Assert.True(AppSettings.Current.FloatingWindowAlwaysOnTop);
    }

    [Fact]
    public void DefaultFloatingWindowPosition_IsHundred()
    {
        Assert.Equal(100, AppSettings.Current.FloatingWindowTop);
        Assert.Equal(100, AppSettings.Current.FloatingWindowLeft);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var settings = AppSettings.Current;
        var originalEngine = settings.TranslationEngine;

        try
        {
            settings.TranslationEngine = "google";
            Assert.Equal("google", settings.TranslationEngine);

            settings.SourceLanguage = "en";
            Assert.Equal("en", settings.SourceLanguage);

            settings.TargetLanguage = "ja";
            Assert.Equal("ja", settings.TargetLanguage);
        }
        finally
        {
            // 恢复原值，避免影响其他测试
            settings.TranslationEngine = originalEngine;
            settings.SourceLanguage = "auto";
            settings.TargetLanguage = "zh-CN";
        }
    }
}
