using System.Windows;
using System.Windows.Forms;
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

    private static NotifyIcon? _notifyIcon;
    private static FloatingWindow? _floatingWindow;
    private static IntPtr _floatingWindowHandle = IntPtr.Zero;
    private static bool _forceExit;

    /// <summary>
    /// 是否强制退出（托盘菜单点了退出）
    /// </summary>
    public static bool IsForceExit => _forceExit;

    private const int HotKeyId = 0xF001;
    private const int ToggleWindowVirtualKey = 0x54;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 初始化用户数据目录（首次访问时自动创建）
        UserDataPaths.Initialize();

        // 加载持久化设置
        AppSettings.Load();

        // 加载翻译缓存
        TranslationCache.Load();

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

        // 首次运行引导
        if (!settings.FirstRunCompleted)
        {
            var wizard = new FirstRunWizard();
            wizard.ShowDialog();
        }

        var services = new ServiceCollection();
        services.AddSingleton<FloatingWindowViewModel>();
        services.AddSingleton<MainViewModel>();
        Services = services.BuildServiceProvider();

        _floatingWindow = new FloatingWindow
        {
            DataContext = Services.GetRequiredService<FloatingWindowViewModel>()
        };
        _floatingWindow.Show();

        _floatingWindowHandle = new WindowInteropHelper(_floatingWindow).Handle;

        var hwndSource = HwndSource.FromHwnd(_floatingWindowHandle);
        hwndSource?.AddHook(WndProcHook);

        NativeMethods.RegisterHotKey(
            _floatingWindowHandle,
            HotKeyId,
            NativeMethods.ModControl | NativeMethods.ModShift,
            ToggleWindowVirtualKey);

        // 创建托盘图标
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "TranslateTool.ico");
        System.Drawing.Icon trayIcon;
        try
        {
            trayIcon = new System.Drawing.Icon(iconPath);
        }
        catch
        {
            trayIcon = System.Drawing.SystemIcons.Application;
        }

        _notifyIcon = new NotifyIcon
        {
            Icon = trayIcon,
            Text = "翻译工具 — 双击唤出",
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => ShowFloatingWindow();

        // 创建完整的右键菜单
        var contextMenu = new ContextMenuStrip();

        // 翻译模式子菜单
        var modeMenu = new ToolStripMenuItem("翻译模式");
        modeMenu.DropDownItems.Add("📋 文本粘贴翻译", null, (_, _) =>
        {
            ShowFloatingWindow();
            var vm = Services.GetRequiredService<FloatingWindowViewModel>();
            vm.PasteTranslateCommand.Execute(null);
        });
        modeMenu.DropDownItems.Add("📄 文件翻译", null, (_, _) =>
        {
            ShowFloatingWindow();
            var vm = Services.GetRequiredService<FloatingWindowViewModel>();
            vm.FileTranslateCommand.Execute(null);
        });
        modeMenu.DropDownItems.Add("✂️ 框选翻译", null, (_, _) =>
        {
            ShowFloatingWindow();
            var vm = Services.GetRequiredService<FloatingWindowViewModel>();
            vm.RegionTranslateCommand.Execute(null);
        });
        contextMenu.Items.Add(modeMenu);

        contextMenu.Items.Add(new ToolStripSeparator());

        // 翻译引擎子菜单（带状态标识）
        var engineMenu = new ToolStripMenuItem("翻译引擎");
        var engineStatuses = EngineStatus.GetAll();
        foreach (var status in engineStatuses)
        {
            var item = new ToolStripMenuItem($"{status.GetStatusIcon()} {status.Label}")
            {
                Checked = AppSettings.Current.TranslationEngine.Equals(status.Name, StringComparison.OrdinalIgnoreCase),
                ToolTipText = status.Note,
                Tag = status.Name
            };
            item.Click += (_, _) =>
            {
                AppSettings.Current.TranslationEngine = status.Name;
                foreach (ToolStripMenuItem mi in engineMenu.DropDownItems)
                {
                    mi.Checked = mi.Tag?.ToString() == status.Name;
                }
            };
            engineMenu.DropDownItems.Add(item);
        }
        contextMenu.Items.Add(engineMenu);

        contextMenu.Items.Add(new ToolStripSeparator());

        // 设置
        contextMenu.Items.Add("⚙️ API 设置", null, (_, _) => ShowSettingsWindow());

        // 自动复制译文开关
        var autoCopyItem = new ToolStripMenuItem("📋 自动复制译文")
        {
            Checked = AppSettings.Current.AutoCopyTranslation
        };
        autoCopyItem.Click += (_, _) =>
        {
            AppSettings.Current.AutoCopyTranslation = !AppSettings.Current.AutoCopyTranslation;
            AppSettings.Current.Save();
            autoCopyItem.Checked = AppSettings.Current.AutoCopyTranslation;
        };
        contextMenu.Items.Add(autoCopyItem);

        // 深色模式切换
        var darkModeItem = new ToolStripMenuItem("🌙 深色模式")
        {
            Checked = AppSettings.Current.DarkMode
        };
        darkModeItem.Click += (_, _) =>
        {
            AppSettings.Current.DarkMode = !AppSettings.Current.DarkMode;
            AppSettings.Current.Save();
            ThemeService.SetDarkMode(AppSettings.Current.DarkMode);
            darkModeItem.Checked = AppSettings.Current.DarkMode;
        };
        contextMenu.Items.Add(darkModeItem);

        // 语言切换子菜单
        var langMenu = new ToolStripMenuItem("🌐 界面语言");
        var languages = new[] { ("zh-CN", "中文"), ("en-US", "English") };
        foreach (var (code, label) in languages)
        {
            var item = new ToolStripMenuItem(label)
            {
                Checked = AppSettings.Current.UILanguage == code,
                Tag = code
            };
            item.Click += (_, _) =>
            {
                AppSettings.Current.UILanguage = code;
                AppSettings.Current.Save();
                // 更新本地化
                Localization.LocalizationManager.Instance.SwitchLanguage(code);
                foreach (ToolStripMenuItem mi in langMenu.DropDownItems)
                {
                    mi.Checked = mi.Tag?.ToString() == code;
                }
            };
            langMenu.DropDownItems.Add(item);
        }
        contextMenu.Items.Add(langMenu);

        // 检查更新
        contextMenu.Items.Add("🔄 检查更新", null, async (_, _) =>
        {
            try
            {
                var update = await UpdateService.CheckForUpdateAsync();
                if (update != null)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"发现新版本 v{update.LatestVersion}！\n\n" +
                        $"当前版本：v{update.CurrentVersion}\n\n" +
                        $"更新内容：\n{update.ReleaseNotes}\n\n" +
                        "是否打开下载页面？",
                        "发现更新",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = update.DownloadUrl,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("当前已是最新版本！", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"检查更新失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        // 快捷键提示
        contextMenu.Items.Add(new ToolStripMenuItem("快捷键: Ctrl+Shift+T") { Enabled = false });

        contextMenu.Items.Add(new ToolStripSeparator());

        // 显示/隐藏悬浮窗
        contextMenu.Items.Add("显示悬浮窗", null, (_, _) => ShowFloatingWindow());

        // 退出
        contextMenu.Items.Add("❌ 退出", null, (_, _) =>
        {
            _forceExit = true;
            _notifyIcon!.Visible = false;
            _floatingWindow?.Close();
            Current.Shutdown();
        });

        _notifyIcon.ContextMenuStrip = contextMenu;

        base.OnStartup(e);
    }

    private static IntPtr WndProcHook(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;

        if (msg == WM_HOTKEY && wParam.ToInt32() == HotKeyId)
        {
            ShowFloatingWindow();
            handled = true;
        }

        return IntPtr.Zero;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_floatingWindowHandle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_floatingWindowHandle, HotKeyId);
        }

        // 保存翻译缓存
        TranslationCache.Save();

        _notifyIcon?.Dispose();
        base.OnExit(e);
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
