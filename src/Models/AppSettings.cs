using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranslateTool.Utils;

namespace TranslateTool.Models;

public class AppSettings
{
    public static AppSettings Current { get; private set; } = new();

    private static readonly string SettingsFilePath = UserDataPaths.SettingsFile;

    public string SourceLanguage { get; set; } = "auto";
    public string TargetLanguage { get; set; } = "zh-CN";
    public string TranslationEngine { get; set; } = "baidu";
    public string? ApiKey { get; set; }
    public string? ApiEndpoint { get; set; }
    public bool ShowFloatingWindow { get; set; } = true;
    public double FloatingWindowTop { get; set; } = 100;
    public double FloatingWindowLeft { get; set; } = 100;
    public bool FloatingWindowAlwaysOnTop { get; set; } = true;

    // API 密钥配置
    public string? BaiduAppId { get; set; }
    public string? BaiduSecretKey { get; set; }
    public string? DeepLApiKey { get; set; }

    // 翻译结果自动复制
    public bool AutoCopyTranslation { get; set; } = false;

    // 界面语言
    public string UILanguage { get; set; } = "zh-CN";

    // 深色模式
    public bool DarkMode { get; set; } = false;

    // 自定义快捷键
    public string HotkeyModifiers { get; set; } = "Ctrl+Shift"; // Ctrl, Ctrl+Shift, Alt, Win
    public string HotkeyKey { get; set; } = "T"; // A-Z, 0-9, F1-F12

    // 首次运行引导
    public bool FirstRunCompleted { get; set; } = false;

    // Toast 提示设置（悬浮窗未显示时）
    public bool ShowToastOnTranslate { get; set; } = true;

    // 划词翻译开关（默认开启）
    public bool EnableSelectionTranslate { get; set; } = true;

    // 贴边隐藏开关（默认开启）
    public bool EnableDockHide { get; set; } = true;

    /// <summary>
    /// 从 JSON 文件加载设置
    /// </summary>
    public static void Load()
    {
        try
        {
            UserDataPaths.EnsureDirectoryExists(UserDataPaths.RoamingAppDataDirectory);
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
                if (settings != null)
                {
                    Current = settings;
                }
            }
        }
        catch
        {
            // 加载失败使用默认设置
            Current = new AppSettings();
        }
    }

    /// <summary>
    /// 保存当前设置到 JSON 文件
    /// </summary>
    public void Save()
    {
        try
        {
            UserDataPaths.EnsureDirectoryExists(UserDataPaths.RoamingAppDataDirectory);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // 保存失败静默忽略
        }
    }

    /// <summary>
    /// 重置为默认设置（仅影响内存中的 <see cref="Current"/>，不删除磁盘文件）
    /// </summary>
    public static void Reset()
    {
        Current = new AppSettings();
    }
}
