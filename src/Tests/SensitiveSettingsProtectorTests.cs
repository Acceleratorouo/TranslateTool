using TranslateTool.Models;
using TranslateTool.Utils;
using Xunit;

namespace TranslateTool.Tests;

public class SensitiveSettingsProtectorTests
{
    [Fact]
    public void ProtectForStorage_EncryptsSensitiveFieldsWithoutMutatingCurrentSettings()
    {
        var settings = new AppSettings
        {
            BaiduAppId = "baidu-app",
            BaiduSecretKey = "baidu-secret",
            DeepLApiKey = "deepl-key",
            LlmProviders =
            [
                new LlmProvider
                {
                    DisplayName = "OpenAI",
                    ApiKey = "llm-key"
                }
            ]
        };

        var protectedSettings = SensitiveSettingsProtector.ProtectForStorage(settings);

        Assert.Equal("baidu-app", protectedSettings.BaiduAppId);
        Assert.NotEqual("baidu-secret", protectedSettings.BaiduSecretKey);
        Assert.NotEqual("deepl-key", protectedSettings.DeepLApiKey);
        Assert.NotEqual("llm-key", protectedSettings.LlmProviders[0].ApiKey);
        Assert.StartsWith(SensitiveSettingsProtector.ProtectedValuePrefix, protectedSettings.BaiduSecretKey);
        Assert.StartsWith(SensitiveSettingsProtector.ProtectedValuePrefix, protectedSettings.DeepLApiKey);
        Assert.StartsWith(SensitiveSettingsProtector.ProtectedValuePrefix, protectedSettings.LlmProviders[0].ApiKey);

        Assert.Equal("baidu-secret", settings.BaiduSecretKey);
        Assert.Equal("deepl-key", settings.DeepLApiKey);
        Assert.Equal("llm-key", settings.LlmProviders[0].ApiKey);
    }

    [Fact]
    public void UnprotectLoadedSettings_DecryptsProtectedSensitiveFields()
    {
        var original = new AppSettings
        {
            BaiduSecretKey = "baidu-secret",
            DeepLApiKey = "deepl-key",
            LlmProviders =
            [
                new LlmProvider
                {
                    DisplayName = "OpenAI",
                    ApiKey = "llm-key"
                }
            ]
        };
        var protectedSettings = SensitiveSettingsProtector.ProtectForStorage(original);

        SensitiveSettingsProtector.UnprotectLoadedSettings(protectedSettings);

        Assert.Equal("baidu-secret", protectedSettings.BaiduSecretKey);
        Assert.Equal("deepl-key", protectedSettings.DeepLApiKey);
        Assert.Equal("llm-key", protectedSettings.LlmProviders[0].ApiKey);
    }

    [Fact]
    public void UnprotectLoadedSettings_LeavesLegacyPlaintextValuesUsable()
    {
        var settings = new AppSettings
        {
            BaiduSecretKey = "legacy-baidu-secret",
            DeepLApiKey = "legacy-deepl-key",
            LlmProviders =
            [
                new LlmProvider
                {
                    DisplayName = "OpenAI",
                    ApiKey = "legacy-llm-key"
                }
            ]
        };

        SensitiveSettingsProtector.UnprotectLoadedSettings(settings);

        Assert.Equal("legacy-baidu-secret", settings.BaiduSecretKey);
        Assert.Equal("legacy-deepl-key", settings.DeepLApiKey);
        Assert.Equal("legacy-llm-key", settings.LlmProviders[0].ApiKey);
    }
}
