using System.Collections.ObjectModel;
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
    private string _hotkeyModifiers = "";

    [ObservableProperty]
    private string _hotkeyKey = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _showToastOnTranslate = true;

    [ObservableProperty]
    private bool _enableSelectionTranslate = true;

    [ObservableProperty]
    private bool _enableDockHide = true;

    [ObservableProperty]
    private string _sourceLanguage = "自动";

    [ObservableProperty]
    private string _targetLanguage = "中文";

    [ObservableProperty]
    private string _regionTranslateHotkeyModifiers = "";

    [ObservableProperty]
    private string _regionTranslateHotkeyKey = "";

    [ObservableProperty]
    private string _selectionTranslateHotkeyModifiers = "";

    [ObservableProperty]
    private string _selectionTranslateHotkeyKey = "";

    /// <summary>
    /// 显示用：悬浮窗快捷键完整字符串
    /// </summary>
    public string ToggleWindowHotkeyDisplay =>
        string.IsNullOrEmpty(HotkeyKey) ? "未设置（点击录制）" : $"{HotkeyModifiers}+{HotkeyKey}";

    /// <summary>
    /// 显示用：划词翻译快捷键完整字符串
    /// </summary>
    public string SelectionTranslateHotkeyDisplay =>
        string.IsNullOrEmpty(SelectionTranslateHotkeyKey) ? "未设置（点击录制）" : $"{SelectionTranslateHotkeyModifiers}+{SelectionTranslateHotkeyKey}";

    /// <summary>
    /// 显示用：框选翻译快捷键完整字符串
    /// </summary>
    public string RegionTranslateHotkeyDisplay =>
        string.IsNullOrEmpty(RegionTranslateHotkeyKey) ? "未设置（点击录制）" : $"{RegionTranslateHotkeyModifiers}+{RegionTranslateHotkeyKey}";

    /// <summary>
    /// 可选语言列表（与悬浮窗一致）
    /// </summary>
    public ObservableCollection<string> AvailableLanguages { get; } = new()
    {
        "自动", "中文", "英文", "日文", "韩文", "法文", "德文", "西班牙文", "俄文"
    };

    public SettingsViewModel()
    {
        _settings = AppSettings.Current;

        // 从当前设置加载值
        _baiduAppId = _settings.BaiduAppId ?? "";
        _baiduSecretKey = _settings.BaiduSecretKey ?? "";
        _deepLApiKey = _settings.DeepLApiKey ?? "";
        _hotkeyModifiers = _settings.HotkeyModifiers ?? "";
        _hotkeyKey = _settings.HotkeyKey ?? "";
        _showToastOnTranslate = _settings.ShowToastOnTranslate;
        _enableSelectionTranslate = _settings.EnableSelectionTranslate;
        _enableDockHide = _settings.EnableDockHide;
        _sourceLanguage = MapCodeToLanguage(_settings.SourceLanguage);
        _targetLanguage = MapCodeToLanguage(_settings.TargetLanguage);
        _regionTranslateHotkeyModifiers = _settings.RegionTranslateHotkeyModifiers ?? "";
        _regionTranslateHotkeyKey = _settings.RegionTranslateHotkeyKey ?? "";
        _selectionTranslateHotkeyModifiers = _settings.SelectionTranslateHotkeyModifiers ?? "";
        _selectionTranslateHotkeyKey = _settings.SelectionTranslateHotkeyKey ?? "";
    }

    partial void OnHotkeyModifiersChanged(string value)
    {
        OnPropertyChanged(nameof(ToggleWindowHotkeyDisplay));
        ApplyHotkeyChange();
    }
    partial void OnHotkeyKeyChanged(string value)
    {
        OnPropertyChanged(nameof(ToggleWindowHotkeyDisplay));
        ApplyHotkeyChange();
    }
    partial void OnSelectionTranslateHotkeyModifiersChanged(string value)
    {
        OnPropertyChanged(nameof(SelectionTranslateHotkeyDisplay));
        ApplyHotkeyChange();
    }
    partial void OnSelectionTranslateHotkeyKeyChanged(string value)
    {
        OnPropertyChanged(nameof(SelectionTranslateHotkeyDisplay));
        ApplyHotkeyChange();
    }
    partial void OnRegionTranslateHotkeyModifiersChanged(string value)
    {
        OnPropertyChanged(nameof(RegionTranslateHotkeyDisplay));
        ApplyHotkeyChange();
    }
    partial void OnRegionTranslateHotkeyKeyChanged(string value)
    {
        OnPropertyChanged(nameof(RegionTranslateHotkeyDisplay));
        ApplyHotkeyChange();
    }

    /// <summary>
    /// 快捷键变更后立即保存并重新注册热键（无需重启应用）
    /// </summary>
    private void ApplyHotkeyChange()
    {
        _settings.HotkeyModifiers = HotkeyModifiers;
        _settings.HotkeyKey = HotkeyKey;
        _settings.SelectionTranslateHotkeyModifiers = SelectionTranslateHotkeyModifiers;
        _settings.SelectionTranslateHotkeyKey = SelectionTranslateHotkeyKey;
        _settings.RegionTranslateHotkeyModifiers = RegionTranslateHotkeyModifiers;
        _settings.RegionTranslateHotkeyKey = RegionTranslateHotkeyKey;
        _settings.Save();

        // 立即重新注册热键
        try
        {
            App.RegisterAllHotkeys();
        }
        catch
        {
            // 忽略重新注册失败（例如设置窗口未关闭时）
        }
    }

    partial void OnShowToastOnTranslateChanged(bool value)
    {
        _settings.ShowToastOnTranslate = value;
        _settings.Save();
    }

    partial void OnEnableSelectionTranslateChanged(bool value)
    {
        _settings.EnableSelectionTranslate = value;
        _settings.Save();
    }

    partial void OnEnableDockHideChanged(bool value)
    {
        _settings.EnableDockHide = value;
        _settings.Save();
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
        _settings.SourceLanguage = MapLanguageToCode(SourceLanguage);
        _settings.TargetLanguage = MapLanguageToCode(TargetLanguage);
        _settings.RegionTranslateHotkeyModifiers = RegionTranslateHotkeyModifiers;
        _settings.RegionTranslateHotkeyKey = RegionTranslateHotkeyKey;
        _settings.SelectionTranslateHotkeyModifiers = SelectionTranslateHotkeyModifiers;
        _settings.SelectionTranslateHotkeyKey = SelectionTranslateHotkeyKey;

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

    private static string MapLanguageToCode(string lang)
    {
        return lang switch
        {
            "自动" => "auto",
            "中文" => "zh-CN",
            "英文" => "en",
            "日文" => "ja",
            "韩文" => "ko",
            "法文" => "fr",
            "德文" => "de",
            "西班牙文" => "es",
            "俄文" => "ru",
            _ => "auto"
        };
    }

    private static string MapCodeToLanguage(string code)
    {
        return code switch
        {
            "auto" => "自动",
            "zh-CN" => "中文",
            "en" => "英文",
            "ja" => "日文",
            "ko" => "韩文",
            "fr" => "法文",
            "de" => "德文",
            "es" => "西班牙文",
            "ru" => "俄文",
            _ => "自动"
        };
    }
}
