using System.IO;
using System.IO.Compression;
using Xunit;
using TranslateTool.Models;
using TranslateTool.Services;
using TranslateTool.Utils;

namespace TranslateTool.Tests;

[Collection("UserData")]
public class UserDataServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public UserDataServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "UserDataServiceTest_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try { Directory.Delete(_tempRoot, recursive: true); } catch { }
        }
    }

    [Fact]
    public void GetDataSummary_ReportsExistingFiles()
    {
        var summary = UserDataService.GetDataSummary();
        // 即便文件不存在或无权限访问，方法也不应抛异常
        Assert.False(string.IsNullOrEmpty(summary.RoamingDirectory));
        Assert.False(string.IsNullOrEmpty(summary.LocalDirectory));
        Assert.True(summary.SettingsBytes >= 0);
        Assert.True(summary.CacheBytes >= 0);
        Assert.True(summary.HistoryBytes >= 0);
    }

    [Fact]
    public async Task ExportAllAsync_CreatesZipFile()
    {
        var targetPath = Path.Combine(_tempRoot, "export.zip");
        await UserDataService.ExportAllAsync(targetPath, UserDataService.ExportFormat.Zip);

        Assert.True(File.Exists(targetPath));
        Assert.True(new FileInfo(targetPath).Length > 0);

        // 验证 ZIP 内含 metadata.json
        using var archive = ZipFile.OpenRead(targetPath);
        Assert.Contains(archive.Entries, e => e.FullName == "metadata.json");
    }

    [Fact]
    public async Task ExportAllAsync_CreatesJsonFile()
    {
        var targetPath = Path.Combine(_tempRoot, "export.json");
        await UserDataService.ExportAllAsync(targetPath, UserDataService.ExportFormat.Json);

        Assert.True(File.Exists(targetPath));
        var content = await File.ReadAllTextAsync(targetPath);
        Assert.Contains("exportedAt", content);
    }

    [Fact]
    public async Task ExportAllAsync_ThrowsOnEmptyPath()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            UserDataService.ExportAllAsync("", UserDataService.ExportFormat.Json));
    }

    [Fact(Skip = "需要独占访问 %APPDATA% 下的 settings.json，CI 环境中可能已被其他进程锁定")]
    public void DeleteAll_RemovesExistingFiles()
    {
        // 准备：写入临时文件
        UserDataPaths.EnsureDirectoryExists(UserDataPaths.RoamingAppDataDirectory);
        UserDataPaths.EnsureDirectoryExists(UserDataPaths.CacheDirectory);
        UserDataPaths.EnsureDirectoryExists(UserDataPaths.HistoryDirectory);

        File.WriteAllText(UserDataPaths.SettingsFile, "{}");
        File.WriteAllText(UserDataPaths.CacheFile, "{}");
        File.WriteAllText(UserDataPaths.HistoryFile, "[]");

        Assert.True(File.Exists(UserDataPaths.SettingsFile));
        Assert.True(File.Exists(UserDataPaths.CacheFile));
        Assert.True(File.Exists(UserDataPaths.HistoryFile));

        var deleted = UserDataService.DeleteAll(keepTessData: true);

        Assert.True(deleted >= 3, $"Should delete at least 3 files, deleted {deleted}");
        Assert.False(File.Exists(UserDataPaths.SettingsFile));
        Assert.False(File.Exists(UserDataPaths.CacheFile));
        Assert.False(File.Exists(UserDataPaths.HistoryFile));
    }

    [Fact]
    public void AppSettings_Reset_RestoresDefaults()
    {
        // 备份原值
        var original = AppSettings.Current;
        var originalSource = original.SourceLanguage;

        try
        {
            // 修改当前设置
            original.SourceLanguage = "fr";
            Assert.Equal("fr", original.SourceLanguage);

            // 重置
            AppSettings.Reset();
            Assert.Equal("auto", AppSettings.Current.SourceLanguage);
        }
        finally
        {
            // 恢复，避免影响其他测试
            AppSettings.Reset();
            AppSettings.Current.SourceLanguage = originalSource;
        }
    }

    [Fact]
    public void AppInfo_Version_IsNotEmpty()
    {
        Assert.False(string.IsNullOrEmpty(AppInfo.Version));
    }
}
