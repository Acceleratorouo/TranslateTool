using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TranslateTool.Models;
using TranslateTool.Utils;

namespace TranslateTool.Services;

/// <summary>
/// 用户数据管理服务 — GDPR 数据主体权利（DSR）实现
///
/// 提供的核心能力：
/// - <see cref="ExportAllAsync"/>：导出所有用户数据（数据可携权，Art. 20）
/// - <see cref="DeleteAll"/>：删除所有用户数据（被遗忘权，Art. 17）
/// - <see cref="GetDataSummary"/>：查看当前存储的数据摘要（知情权，Art. 15）
///
/// 所有操作都在 <see cref="UserDataPaths"/> 定义的目录内执行。
/// </summary>
public static class UserDataService
{
    /// <summary>
    /// 导出格式：JSON 或 ZIP（含 JSON 摘要 + 原始文件）
    /// </summary>
    public enum ExportFormat
    {
        Json,
        Zip
    }

    /// <summary>
    /// 用户数据摘要
    /// </summary>
    public record DataSummary(
        bool SettingsExists,
        bool CacheExists,
        bool HistoryExists,
        int TessDataFileCount,
        long SettingsBytes,
        long CacheBytes,
        long HistoryBytes,
        long TessDataBytes,
        string RoamingDirectory,
        string LocalDirectory);

    /// <summary>
    /// 收集所有用户数据到一个字典中，便于导出
    /// </summary>
    private static Dictionary<string, object?> CollectAllData()
    {
        var data = new Dictionary<string, object?>
        {
            ["exportedAt"] = DateTime.UtcNow.ToString("o"),
            ["appVersion"] = AppInfo.Version,
            ["settings"] = AppSettings.Current,
        };

        // 缓存（从静态实例读取）
        var cacheStats = TranslationCache.GetStats();
        data["cacheStats"] = new { cacheStats.count, cacheStats.expired };

        return data;
    }

    /// <summary>
    /// 导出所有用户数据到指定文件
    /// </summary>
    /// <param name="targetFilePath">目标文件路径（.json 或 .zip）</param>
    /// <param name="format">导出格式</param>
    public static async Task ExportAllAsync(string targetFilePath, ExportFormat format = ExportFormat.Zip)
    {
        if (string.IsNullOrWhiteSpace(targetFilePath))
            throw new ArgumentException("目标路径不能为空", nameof(targetFilePath));

        // 确保目标目录存在
        var targetDir = Path.GetDirectoryName(targetFilePath);
        if (!string.IsNullOrEmpty(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // 确保在导出前保存最新状态
        AppSettings.Current.Save();
        TranslationCache.Save();

        var exportData = CollectAllData();
        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        if (format == ExportFormat.Json)
        {
            await File.WriteAllTextAsync(targetFilePath, json, Encoding.UTF8);
            return;
        }

        // ZIP：包含 metadata.json + 各原始文件副本
        if (File.Exists(targetFilePath))
            File.Delete(targetFilePath);

        await using var fs = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

        // 元数据
        var metaEntry = archive.CreateEntry("metadata.json", CompressionLevel.Optimal);
        await using (var metaStream = metaEntry.Open())
        await using (var writer = new StreamWriter(metaStream, Encoding.UTF8))
        {
            await writer.WriteAsync(json);
        }

        // 原始文件
        await AddFileToArchive(archive, "settings.json", UserDataPaths.SettingsFile);
        await AddFileToArchive(archive, "translation_cache.json", UserDataPaths.CacheFile);
        await AddFileToArchive(archive, "translation_history.json", UserDataPaths.HistoryFile);
    }

    /// <summary>
    /// 删除所有用户数据（被遗忘权）
    /// </summary>
    /// <param name="keepTessData">是否保留 OCR 语言包（避免每次卸载/重装都重新下载）</param>
    /// <returns>实际删除的文件/目录数量</returns>
    public static int DeleteAll(bool keepTessData = true)
    {
        int deleted = 0;

        deleted += TryDeleteFile(UserDataPaths.SettingsFile);
        deleted += TryDeleteFile(UserDataPaths.CacheFile);
        deleted += TryDeleteFile(UserDataPaths.HistoryFile);

        // 清空历史内存
        TranslationCache.Clear();

        // 重置 AppSettings 为默认值（仅在内存中）
        AppSettings.Reset();

        if (!keepTessData && Directory.Exists(UserDataPaths.TessDataDirectory))
        {
            try
            {
                Directory.Delete(UserDataPaths.TessDataDirectory, recursive: true);
                deleted++;
            }
            catch { /* 忽略 */ }
        }

        return deleted;
    }

    /// <summary>
    /// 获取用户数据摘要
    /// </summary>
    public static DataSummary GetDataSummary()
    {
        long SizeOf(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

        int tessCount = 0;
        long tessBytes = 0;
        if (Directory.Exists(UserDataPaths.TessDataDirectory))
        {
            var files = Directory.GetFiles(UserDataPaths.TessDataDirectory);
            tessCount = files.Length;
            tessBytes = files.Sum(f => new FileInfo(f).Length);
        }

        return new DataSummary(
            SettingsExists: File.Exists(UserDataPaths.SettingsFile),
            CacheExists: File.Exists(UserDataPaths.CacheFile),
            HistoryExists: File.Exists(UserDataPaths.HistoryFile),
            TessDataFileCount: tessCount,
            SettingsBytes: SizeOf(UserDataPaths.SettingsFile),
            CacheBytes: SizeOf(UserDataPaths.CacheFile),
            HistoryBytes: SizeOf(UserDataPaths.HistoryFile),
            TessDataBytes: tessBytes,
            RoamingDirectory: UserDataPaths.RoamingAppDataDirectory,
            LocalDirectory: UserDataPaths.LocalAppDataDirectory);
    }

    private static async Task AddFileToArchive(ZipArchive archive, string entryName, string filePath)
    {
        if (!File.Exists(filePath)) return;
        try
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await using var fileStream = File.OpenRead(filePath);
            await fileStream.CopyToAsync(entryStream);
        }
        catch { /* 跳过不可读文件 */ }
    }

    private static int TryDeleteFile(string path)
    {
        if (!File.Exists(path)) return 0;
        try
        {
            File.Delete(path);
            return 1;
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// 应用信息（用于数据导出中的版本标记）
/// </summary>
public static class AppInfo
{
    /// <summary>
    /// 应用版本（从程序集读取）
    /// </summary>
    public static string Version
    {
        get
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
