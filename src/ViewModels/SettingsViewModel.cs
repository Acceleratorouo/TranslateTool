using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateTool.Models;
using TranslateTool.Services;

namespace TranslateTool.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;

    [ObservableProperty]
    private string _baiduAppId = "";

    [ObservableProperty]
    private string _baiduSecretKey = "";

    [ObservableProperty]
    private string _deepLApiKey = "";

    [ObservableProperty]
    private string _hotkeyModifiers = "Ctrl+Shift";

    [ObservableProperty]
    private string _hotkeyKey = "T";

    [ObservableProperty]
    private string _statusMessage = "";

    public SettingsViewModel()
    {
        _settings = AppSettings.Current;

        // 从当前设置加载值
        _baiduAppId = _settings.BaiduAppId ?? "";
        _baiduSecretKey = _settings.BaiduSecretKey ?? "";
        _deepLApiKey = _settings.DeepLApiKey ?? "";
        _hotkeyModifiers = _settings.HotkeyModifiers ?? "Ctrl+Shift";
        _hotkeyKey = _settings.HotkeyKey ?? "T";
    }

    [RelayCommand]
    private void Save()
    {
        // 保存到设置
        _settings.BaiduAppId = string.IsNullOrWhiteSpace(BaiduAppId) ? null : BaiduAppId.Trim();
        _settings.BaiduSecretKey = string.IsNullOrWhiteSpace(BaiduSecretKey) ? null : BaiduSecretKey.Trim();
        _settings.DeepLApiKey = string.IsNullOrWhiteSpace(DeepLApiKey) ? null : DeepLApiKey.Trim();
        _settings.HotkeyModifiers = HotkeyModifiers;
        _settings.HotkeyKey = HotkeyKey;

        // 应用到翻译引擎
        if (_settings.BaiduAppId != null && _settings.BaiduSecretKey != null)
        {
            BaiduTranslator.SetCredentials(_settings.BaiduAppId, _settings.BaiduSecretKey);
        }

        if (_settings.DeepLApiKey != null)
        {
            DeepLTranslator.SetApiKey(_settings.DeepLApiKey);
        }

        // 持久化保存
        _settings.Save();

        StatusMessage = "✅ 设置已保存";

        // 延迟关闭窗口
        Task.Run(async () =>
        {
            await Task.Delay(800);
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // 找到并关闭设置窗口
                foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                {
                    if (window is Views.SettingsWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            });
        });
    }

    [RelayCommand]
    private void Cancel()
    {
        // 找到并关闭设置窗口
        foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
        {
            if (window is Views.SettingsWindow)
            {
                window.Close();
                break;
            }
        }
    }

    [RelayCommand]
    private void ClearBaiduCredentials()
    {
        BaiduAppId = "";
        BaiduSecretKey = "";
        _settings.BaiduAppId = null;
        _settings.BaiduSecretKey = null;
        BaiduTranslator.SetCredentials("", "");
        _settings.Save();
        StatusMessage = "百度翻译 API 密钥已清除";
    }

    [RelayCommand]
    private void ClearDeepLKey()
    {
        DeepLApiKey = "";
        _settings.DeepLApiKey = null;
        DeepLTranslator.SetApiKey("");
        _settings.Save();
        StatusMessage = "DeepL API Key 已清除";
    }

    // —— GDPR 数据主体权利 (DSR) ——

    /// <summary>
    /// 导出所有用户数据（数据可携权，GDPR Art. 20）
    /// </summary>
    [RelayCommand]
    private async Task ExportUserDataAsync()
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "导出用户数据",
                FileName = $"TranslateTool-export-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
                Filter = "ZIP 压缩包 (*.zip)|*.zip|JSON 文件 (*.json)|*.json",
                DefaultExt = ".zip"
            };
            if (dlg.ShowDialog() != true)
            {
                StatusMessage = "已取消导出";
                return;
            }

            var format = dlg.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? UserDataService.ExportFormat.Json
                : UserDataService.ExportFormat.Zip;

            await UserDataService.ExportAllAsync(dlg.FileName, format);
            StatusMessage = $"✅ 用户数据已导出到 {dlg.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 导出失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 删除所有用户数据（被遗忘权，GDPR Art. 17）
    /// </summary>
    [RelayCommand]
    private void DeleteUserData()
    {
        var result = System.Windows.MessageBox.Show(
            "此操作将删除所有本地保存的设置、翻译历史和缓存，无法撤销。\n\n" +
            "是否同时删除已下载的 OCR 语言包？\n" +
            "（删除后下次使用 OCR 时需重新下载）",
            "删除用户数据",
            System.Windows.MessageBoxButton.YesNoCancel,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Cancel)
        {
            StatusMessage = "已取消删除";
            return;
        }

        var keepTessData = result == System.Windows.MessageBoxResult.No;
        var deleted = UserDataService.DeleteAll(keepTessData);

        // 同步清理 UI 状态
        BaiduAppId = "";
        BaiduSecretKey = "";
        DeepLApiKey = "";
        BaiduTranslator.SetCredentials("", "");
        DeepLTranslator.SetApiKey("");

        StatusMessage = $"✅ 已删除 {deleted} 项本地数据";
    }

    /// <summary>
    /// 显示当前存储的数据摘要（知情权，GDPR Art. 15）
    /// </summary>
    [RelayCommand]
    private void ShowDataSummary()
    {
        var s = UserDataService.GetDataSummary();
        var lines = new[]
        {
            "📊 本地存储数据摘要：",
            "",
            $"设置:    {(s.SettingsExists ? $"{FormatSize(s.SettingsBytes)}" : "无")}",
            $"缓存:    {(s.CacheExists ? FormatSize(s.CacheBytes) : "无")}",
            $"历史:    {(s.HistoryExists ? FormatSize(s.HistoryBytes) : "无")}",
            $"OCR包:   {s.TessDataFileCount} 个文件 ({FormatSize(s.TessDataBytes)})",
            "",
            $"漫游: {s.RoamingDirectory}",
            $"本地: {s.LocalDirectory}"
        };
        System.Windows.MessageBox.Show(string.Join("\n", lines), "数据摘要",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        StatusMessage = "已显示数据摘要";
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
