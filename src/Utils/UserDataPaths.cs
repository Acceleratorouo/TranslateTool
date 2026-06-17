using System.IO;

namespace TranslateTool.Utils;

/// <summary>
/// 用户数据路径管理器 — 集中管理应用的用户数据目录。
///
/// 设计原则（用户数据隔离）：
/// - <see cref="SettingsFile"/>：漫游设置（API Key、主题、UI 语言）→ %APPDATA%\TranslateTool\
/// - <see cref="CacheFile"/>：非漫游缓存（翻译缓存）→ %LOCALAPPDATA%\TranslateTool\Cache\
/// - <see cref="HistoryFile"/>：非漫游历史（翻译历史）→ %LOCALAPPDATA%\TranslateTool\History\
/// - <see cref="TessDataDirectory"/>：非漫游 OCR 数据（每个用户独立下载）→ %LOCALAPPDATA%\TranslateTool\tessdata\
/// - <see cref="LogsDirectory"/>：非漫游日志 → %LOCALAPPDATA%\TranslateTool\Logs\
///
/// 所有目录在首次访问时自动创建。
/// </summary>
public static class UserDataPaths
{
    /// <summary>
    /// 应用名（用于构建子目录）
    /// </summary>
    public const string AppFolderName = "TranslateTool";

    /// <summary>
    /// 漫游用户数据根目录：%APPDATA%\TranslateTool\
    /// 适合：跨设备同步的设置（通过 OneDrive 漫游）
    /// </summary>
    public static string RoamingAppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppFolderName);

    /// <summary>
    /// 本地用户数据根目录：%LOCALAPPDATA%\TranslateTool\
    /// 适合：缓存、历史、临时数据（不漫游）
    /// </summary>
    public static string LocalAppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppFolderName);

    /// <summary>
    /// 缓存目录：%LOCALAPPDATA%\TranslateTool\Cache\
    /// </summary>
    public static string CacheDirectory => Path.Combine(LocalAppDataDirectory, "Cache");

    /// <summary>
    /// 历史目录：%LOCALAPPDATA%\TranslateTool\History\
    /// </summary>
    public static string HistoryDirectory => Path.Combine(LocalAppDataDirectory, "History");

    /// <summary>
    /// OCR 语言包目录：%LOCALAPPDATA%\TranslateTool\tessdata\
    /// </summary>
    public static string TessDataDirectory => Path.Combine(LocalAppDataDirectory, "tessdata");

    /// <summary>
    /// 日志目录：%LOCALAPPDATA%\TranslateTool\Logs\
    /// </summary>
    public static string LogsDirectory => Path.Combine(LocalAppDataDirectory, "Logs");

    /// <summary>
    /// 应用设置文件：%APPDATA%\TranslateTool\settings.json
    /// </summary>
    public static string SettingsFile => Path.Combine(RoamingAppDataDirectory, "settings.json");

    /// <summary>
    /// 翻译缓存文件：%LOCALAPPDATA%\TranslateTool\Cache\translation_cache.json
    /// </summary>
    public static string CacheFile => Path.Combine(CacheDirectory, "translation_cache.json");

    /// <summary>
    /// 翻译历史文件：%LOCALAPPDATA%\TranslateTool\History\translation_history.json
    /// </summary>
    public static string HistoryFile => Path.Combine(HistoryDirectory, "translation_history.json");

    /// <summary>
    /// 确保指定目录存在（若不存在则创建）
    /// </summary>
    public static void EnsureDirectoryExists(string directory)
    {
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// 初始化所有用户数据目录（应用启动时调用一次）
    /// </summary>
    public static void Initialize()
    {
        EnsureDirectoryExists(RoamingAppDataDirectory);
        EnsureDirectoryExists(LocalAppDataDirectory);
        EnsureDirectoryExists(CacheDirectory);
        EnsureDirectoryExists(HistoryDirectory);
        EnsureDirectoryExists(TessDataDirectory);
        EnsureDirectoryExists(LogsDirectory);
    }
}
