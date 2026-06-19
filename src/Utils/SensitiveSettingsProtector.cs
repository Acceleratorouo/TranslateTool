using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TranslateTool.Models;

namespace TranslateTool.Utils;

/// <summary>
/// Protects sensitive settings before they are written to disk and unprotects them after load.
/// </summary>
public static class SensitiveSettingsProtector
{
    public const string ProtectedValuePrefix = "dpapi:v1:";

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("TranslateTool.Settings.v1");

    public static AppSettings ProtectForStorage(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var json = JsonSerializer.Serialize(settings);
        var protectedSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        ProtectSensitiveFields(protectedSettings);
        return protectedSettings;
    }

    public static void UnprotectLoadedSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.BaiduSecretKey = UnprotectValue(settings.BaiduSecretKey);
        settings.DeepLApiKey = UnprotectValue(settings.DeepLApiKey);

        foreach (var provider in settings.LlmProviders)
        {
            provider.ApiKey = UnprotectValue(provider.ApiKey);
        }
    }

    private static void ProtectSensitiveFields(AppSettings settings)
    {
        settings.BaiduSecretKey = ProtectValue(settings.BaiduSecretKey);
        settings.DeepLApiKey = ProtectValue(settings.DeepLApiKey);

        foreach (var provider in settings.LlmProviders)
        {
            provider.ApiKey = ProtectValue(provider.ApiKey);
        }
    }

    private static string? ProtectValue(string? value)
    {
        if (string.IsNullOrEmpty(value) || IsProtected(value))
        {
            return value;
        }

        var plaintext = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(
            plaintext,
            Entropy,
            DataProtectionScope.CurrentUser);
        return ProtectedValuePrefix + Convert.ToBase64String(protectedBytes);
    }

    private static string? UnprotectValue(string? value)
    {
        if (string.IsNullOrEmpty(value) || !IsProtected(value))
        {
            return value;
        }

        var payload = value[ProtectedValuePrefix.Length..];
        var protectedBytes = Convert.FromBase64String(payload);
        var plaintext = ProtectedData.Unprotect(
            protectedBytes,
            Entropy,
            DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(plaintext);
    }

    private static bool IsProtected(string value)
    {
        return value.StartsWith(ProtectedValuePrefix, StringComparison.Ordinal);
    }
}
