using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using TranslateTool.Localization;
using TranslateTool.Models;
using TranslateTool.Services;
using TranslateTool.Utils;
using TranslateTool.ViewModels;
using TranslateTool.Views;

namespace TranslateTool;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private static FloatingWindow? _floatingWindow;
    private static IntPtr _floatingWindowHandle = IntPtr.Zero;
    private static bool _forceExit;

    private static SingleInstanceManager? _singleInstanceManager;
    private static TrayIconManager? _trayIconManager;

    private const string SingleInstanceMutexName = "TranslateTool_SingleInstance_Mutex_3F7A2E";
    private const string SingleInstanceWindowMessageName = "TranslateTool_Wakeup_ShowInstance";
    private const string MainWindowTitle = "翻译工具";

    /// <summary>
    /// 是否强制退出（托盘菜单点了退出）
    /// </summary>
    public static bool IsForceExit => _forceExit;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 单实例检测：如果已有实例运行，唤醒已有实例到前台后退出
        _singleInstanceManager = new SingleInstanceManager(SingleInstanceMutexName, SingleInstanceWindowMessageName, MainWindowTitle);
        if (!_singleInstanceManager.TryAcquireOwnership())
        {
            _singleInstanceManager.WakeupExistingInstance();
            _singleInstanceManager.Dispose();
            _singleInstanceManager = null;
            Shutdown();
            return;
        }

        // 初始化用户数据目录（首次访问时自动创建）
        UserDataPaths.Initialize();

        // 加载持久化设置
        AppSettings.Load();

        // 加载翻译缓存
        TranslationCache.Load();

        // 加载历史反馈数据（用于引擎声誉评分）
        TranslationFeedbackService.Load();

        // 应用已保存的 API 密钥
        var settings = AppSettings.Current;
        if (!string.IsNullOrEmpty(settings.BaiduAppId) && !string.IsNullOrEmpty(settings.BaiduSecretKey))
        {
            BaiduTranslator.SetCredentials(settings.BaiduAppId, settings.BaiduSecretKey);
        }
        if (!string.IsNullOrEmpty(settings.DeepLApiKey))
        {
            DeepLTranslator.SetApiKey(settings.DeepLApiKey);
        }

        // 应用深色模式
        if (settings.DarkMode)
        {
            ThemeService.SetDarkMode(true);
        }

        // 应用语言设置
        if (!string.IsNullOrEmpty(settings.UILanguage))
        {
            LocalizationManager.Instance.SwitchLanguage(settings.UILanguage);
        }

        // 预先创建主窗口（即使首次运行也先创建，避免引导窗口关闭后应用退出）
        var services = new ServiceCollection();
        services.AddSingleton<IClipboardService, WpfClipboardService>();
        services.AddSingleton<IFilePickerService, WpfFilePickerService>();
        services.AddSingleton<IRegionSelectionService, WpfRegionSelectionService>();
        services.AddSingleton<ITextToSpeechService, WpfTextToSpeechService>();
        services.AddSingleton<INotificationService, WpfNotificationService>();
        services.AddSingleton<ITimerService, WpfTimerService>();
        services.AddSingleton<FloatingWindowViewModel>();
        services.AddSingleton<MainViewModel>();
        Services = services.BuildServiceProvider();

        _floatingWindow = new FloatingWindow
        {
            DataContext = Services.GetRequiredService<FloatingWindowViewModel>(),
            ShowInTaskbar = false,
            Visibility = Visibility.Hidden
        };

        // 首次运行引导
        if (!settings.FirstRunCompleted)
        {
            var wizard = new FirstRunWizard();
            wizard.ShowDialog();
        }

        // 引导完成后显示主窗口
        _floatingWindow.ShowInTaskbar = true;
        _floatingWindow.Visibility = Visibility.Visible;
        _floatingWindow.Show();
        _floatingWindow.Activate();

        // 窗口显示后获取真实句柄并挂载消息钩子
        _floatingWindowHandle = new WindowInteropHelper(_floatingWindow).Handle;
        var hwndSource = HwndSource.FromHwnd(_floatingWindowHandle);
        hwndSource?.AddHook(WndProcHook);

        // 注册所有热键
        HotkeyManager.RegisterAllHotkeys(_floatingWindowHandle);

        // 创建托盘图标
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "TranslateTool.ico");
        System.Drawing.Icon trayIcon;
        try
        {
            trayIcon = new System.Drawing.Icon(iconPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"托盘图标加载失败: {ex.Message}");
            trayIcon = System.Drawing.SystemIcons.Application;
        }

        _trayIconManager = new TrayIconManager(Services, ShowFloatingWindow, ShowSettingsWindow, OnTrayExitRequested);
        _trayIconManager.CreateTrayIcon(trayIcon);

        base.OnStartup(e);
    }

    private static IntPtr WndProcHook(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        // 处理第二实例发来的唤醒消息
        var windowMessage = _singleInstanceManager?.WindowMessage ?? 0;
        if (windowMessage != 0 && msg == windowMessage)
        {
            ShowFloatingWindow();
            handled = true;
            return IntPtr.Zero;
        }

        const int WM_HOTKEY = 0x0312;

        if (msg == WM_HOTKEY)
        {
            var hotkeyId = wParam.ToInt32();

            if (hotkeyId == NativeMethods.HOTKEY_TOGGLE_WINDOW)
            {
                ShowFloatingWindow();
                handled = true;
            }
            else if (hotkeyId == NativeMethods.HOTKEY_SELECTION_TRANSLATE)
            {
                // 划词翻译热键
                var vm = Services.GetRequiredService<FloatingWindowViewModel>();
                vm.SelectionTranslate();
                handled = true;
            }
            else if (hotkeyId == NativeMethods.HOTKEY_REGION_TRANSLATE)
            {
                // 框选翻译热键
                ShowFloatingWindow();
                var vm = Services.GetRequiredService<FloatingWindowViewModel>();
                vm.RegionTranslateCommand.Execute(null);
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        HotkeyManager.UnregisterAllHotkeys(_floatingWindowHandle);

        // 保存翻译缓存
        TranslationCache.Save();

        _trayIconManager?.Dispose();
        _singleInstanceManager?.Dispose();

        base.OnExit(e);
    }

    /// <summary>
    /// 重新注册所有热键（先注销旧的，再注册新的）。
    /// 供设置页面在用户修改快捷键后立即调用，无需重启应用。
    /// </summary>
    public static void RegisterAllHotkeys()
    {
        HotkeyManager.RegisterAllHotkeys(_floatingWindowHandle);
    }

    private static void OnTrayExitRequested()
    {
        _forceExit = true;
        _floatingWindow?.Close();
        Current.Shutdown();
    }

    private static void ShowFloatingWindow()
    {
        var fw = _floatingWindow;
        if (fw is null)
        {
            return;
        }

        fw.Show();
        fw.WindowState = WindowState.Normal;
        fw.Topmost = true;
        fw.Activate();
        fw.Topmost = false;
    }

    private static void ShowSettingsWindow()
    {
        var settingsWindow = new SettingsWindow
        {
            DataContext = new SettingsViewModel()
        };
        settingsWindow.Show();
    }
}
