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
}
