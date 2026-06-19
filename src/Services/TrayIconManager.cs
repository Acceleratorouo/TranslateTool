using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using TranslateTool.Localization;
using TranslateTool.Models;
using TranslateTool.ViewModels;
using TranslateTool.Views;

namespace TranslateTool.Services;

/// <summary>
/// 负责创建托盘图标及其右键菜单。
/// </summary>
internal sealed class TrayIconManager : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly Action _showFloatingWindow;
    private readonly Action _showSettingsWindow;
    private readonly Action _onExitRequested;
    private NotifyIcon? _notifyIcon;

    public TrayIconManager(IServiceProvider services, Action showFloatingWindow, Action showSettingsWindow, Action onExitRequested)
    {
        _services = services;
        _showFloatingWindow = showFloatingWindow;
        _showSettingsWindow = showSettingsWindow;
        _onExitRequested = onExitRequested;
    }

    /// <summary>
    /// 创建并显示托盘图标。
    /// </summary>
    public void CreateTrayIcon(Icon icon)
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Text = "翻译工具 — 双击唤出",
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => _showFloatingWindow();

        var contextMenu = new ContextMenuStrip();

        // 翻译模式子菜单
        var modeMenu = new ToolStripMenuItem("翻译模式");
        modeMenu.DropDownItems.Add("📋 文本粘贴翻译", null, (_, _) =>
        {
            _showFloatingWindow();
            var vm = _services.GetRequiredService<FloatingWindowViewModel>();
            vm.PasteTranslateCommand.Execute(null);
        });
        modeMenu.DropDownItems.Add("📄 文件翻译", null, (_, _) =>
        {
            _showFloatingWindow();
            var vm = _services.GetRequiredService<FloatingWindowViewModel>();
            vm.FileTranslateCommand.Execute(null);
        });
        modeMenu.DropDownItems.Add("✂️ 框选翻译", null, (_, _) =>
        {
            _showFloatingWindow();
            var vm = _services.GetRequiredService<FloatingWindowViewModel>();
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
        contextMenu.Items.Add("⚙️ API 设置", null, (_, _) => _showSettingsWindow());

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
                LocalizationManager.Instance.SwitchLanguage(code);
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
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Information);

                    if (result == System.Windows.MessageBoxResult.Yes)
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
                    System.Windows.MessageBox.Show("当前已是最新版本！", "检查更新", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"检查更新失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        });

        // 快捷键提示
        var toggleHk = string.IsNullOrEmpty(AppSettings.Current.HotkeyKey) ? "未设置" : $"{AppSettings.Current.HotkeyModifiers}+{AppSettings.Current.HotkeyKey}";
        var selHk = string.IsNullOrEmpty(AppSettings.Current.SelectionTranslateHotkeyKey) ? "未设置" : $"{AppSettings.Current.SelectionTranslateHotkeyModifiers}+{AppSettings.Current.SelectionTranslateHotkeyKey}";
        var regionHk = string.IsNullOrEmpty(AppSettings.Current.RegionTranslateHotkeyKey) ? "未设置" : $"{AppSettings.Current.RegionTranslateHotkeyModifiers}+{AppSettings.Current.RegionTranslateHotkeyKey}";
        contextMenu.Items.Add(new ToolStripMenuItem($"快捷键: {toggleHk} 显示 | {selHk} 划词翻译 | {regionHk} 框选翻译") { Enabled = false });

        contextMenu.Items.Add(new ToolStripSeparator());

        // 显示/隐藏悬浮窗
        contextMenu.Items.Add("显示悬浮窗", null, (_, _) => _showFloatingWindow());

        // 退出
        contextMenu.Items.Add("❌ 退出", null, (_, _) => _onExitRequested());

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }
}
