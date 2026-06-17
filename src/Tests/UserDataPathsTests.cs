using System.IO;
using Xunit;
using TranslateTool.Utils;

namespace TranslateTool.Tests;

public class UserDataPathsTests
{
    [Fact]
    public void RoamingAppDataDirectory_ContainsAppFolderName()
    {
        var path = UserDataPaths.RoamingAppDataDirectory;
        Assert.Contains(UserDataPaths.AppFolderName, path);
        Assert.True(Path.IsPathRooted(path), "Should be an absolute path");
    }

    [Fact]
    public void LocalAppDataDirectory_ContainsAppFolderName()
    {
        var path = UserDataPaths.LocalAppDataDirectory;
        Assert.Contains(UserDataPaths.AppFolderName, path);
        Assert.True(Path.IsPathRooted(path), "Should be an absolute path");
    }

    [Fact]
    public void SettingsFile_IsUnderRoamingDirectory()
    {
        Assert.StartsWith(UserDataPaths.RoamingAppDataDirectory, UserDataPaths.SettingsFile);
    }

    [Fact]
    public void CacheFile_IsUnderLocalAppData()
    {
        Assert.StartsWith(UserDataPaths.LocalAppDataDirectory, UserDataPaths.CacheFile);
    }

    [Fact]
    public void HistoryFile_IsUnderLocalAppData()
    {
        Assert.StartsWith(UserDataPaths.LocalAppDataDirectory, UserDataPaths.HistoryFile);
    }

    [Fact]
    public void TessDataDirectory_IsUnderLocalAppData()
    {
        Assert.StartsWith(UserDataPaths.LocalAppDataDirectory, UserDataPaths.TessDataDirectory);
    }

    [Fact]
    public void LogsDirectory_IsUnderLocalAppData()
    {
        Assert.StartsWith(UserDataPaths.LocalAppDataDirectory, UserDataPaths.LogsDirectory);
    }

    [Fact]
    public void EnsureDirectoryExists_CreatesMissingDirectory()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "TranslateToolTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.False(Directory.Exists(tempRoot));
            UserDataPaths.EnsureDirectoryExists(tempRoot);
            Assert.True(Directory.Exists(tempRoot));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void EnsureDirectoryExists_DoesNotThrow_OnExistingDirectory()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "TranslateToolTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempRoot);
            // Should not throw on existing directory
            UserDataPaths.EnsureDirectoryExists(tempRoot);
            Assert.True(Directory.Exists(tempRoot));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void EnsureDirectoryExists_DoesNotThrow_OnEmptyString()
    {
        // Defensive: should not throw on empty/null input
        UserDataPaths.EnsureDirectoryExists("");
        UserDataPaths.EnsureDirectoryExists(null!);
    }
}
