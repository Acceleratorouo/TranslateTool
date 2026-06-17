using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using TranslateTool.Models;
using TranslateTool.ViewModels;

namespace TranslateTool.Views;

public partial class FloatingWindow : Window
{
    private const int ResizeBorder = 6; // 边缘可拖拽区域像素

    public FloatingWindow()
    {
        InitializeComponent();
        AllowDrop = true;
        Drop += FloatingWindow_Drop;
        DragEnter += FloatingWindow_DragEnter;
        Closing += FloatingWindow_Closing;
        Closed += FloatingWindow_Closed;
        SourceInitialized += (_, _) =>
        {
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);
        };
    }

    /// <summary>
    /// 处理窗口边缘拖拽调整大小
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084;
        const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

        if (msg == WM_NCHITTEST)
        {
            // 只在最大化时跳过边缘检测
            if (WindowState == WindowState.Maximized) return IntPtr.Zero;

            var mousePos = PointFromScreen(new System.Windows.Point(
                (short)(lParam.ToInt32() & 0xFFFF),
                (short)(lParam.ToInt32() >> 16)));

            var w = ActualWidth;
            var h = ActualHeight;
            var b = ResizeBorder;

            // 左上
            if (mousePos.X <= b && mousePos.Y <= b) { handled = true; return new IntPtr(HTTOPLEFT); }
            // 右上
            if (mousePos.X >= w - b && mousePos.Y <= b) { handled = true; return new IntPtr(HTTOPRIGHT); }
            // 左下
            if (mousePos.X <= b && mousePos.Y >= h - b) { handled = true; return new IntPtr(HTBOTTOMLEFT); }
            // 右下
            if (mousePos.X >= w - b && mousePos.Y >= h - b) { handled = true; return new IntPtr(HTBOTTOMRIGHT); }
            // 左
            if (mousePos.X <= b) { handled = true; return new IntPtr(HTLEFT); }
            // 右
            if (mousePos.X >= w - b) { handled = true; return new IntPtr(HTRIGHT); }
            // 上
            if (mousePos.Y <= b) { handled = true; return new IntPtr(HTTOP); }
            // 下
            if (mousePos.Y >= h - b) { handled = true; return new IntPtr(HTBOTTOM); }
        }

        return IntPtr.Zero;
    }

    private void FloatingWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (App.IsForceExit) return;
        e.Cancel = true;
        Hide();
    }

    private void FloatingWindow_Closed(object? sender, EventArgs e)
    {
        if (DataContext is FloatingWindowViewModel vm)
        {
            vm.Cleanup();
        }
    }

    private void FloatingWindow_DragEnter(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void FloatingWindow_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                var filePath = files[0];
                if (DataContext is FloatingWindowViewModel vm)
                {
                    await vm.TranslateFileCommand.ExecuteAsync(filePath);
                }
            }
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 只在非边缘区域触发拖动
        var pos = e.GetPosition(this);
        var b = ResizeBorder;
        if (pos.X > b && pos.X < ActualWidth - b && pos.Y > b && pos.Y < ActualHeight - b)
        {
            DragMove();
        }
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Allow DragMove to be triggered by the actual title bar area
    }

    private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var menu = new ContextMenu();

        var pasteItem = new MenuItem { Header = "文本粘贴翻译" };
        pasteItem.Click += (_, _) =>
        {
            if (DataContext is FloatingWindowViewModel vm)
            {
                vm.PasteTranslateCommand.Execute(null);
            }
            menu.IsOpen = false;
        };

        var fileItem = new MenuItem { Header = "文件翻译" };
        fileItem.Click += (_, _) =>
        {
            if (DataContext is FloatingWindowViewModel vm)
            {
                vm.FileTranslateCommand.Execute(null);
            }
            menu.IsOpen = false;
        };

        var regionItem = new MenuItem { Header = "截图框选翻译" };
        regionItem.Click += (_, _) =>
        {
            if (DataContext is FloatingWindowViewModel vm)
            {
                vm.RegionTranslateCommand.Execute(null);
            }
            menu.IsOpen = false;
        };

        var historyItem = new MenuItem { Header = "翻译历史" };
        historyItem.Click += (_, _) =>
        {
            if (DataContext is FloatingWindowViewModel vm)
            {
                vm.ShowHistoryCommand.Execute(null);
            }
            menu.IsOpen = false;
        };

        var settingsItem = new MenuItem { Header = "翻译引擎" };
        var engines = new[] { ("baidu", "百度翻译"), ("google", "Google 翻译"), ("microsoft", "微软翻译"), ("deepl", "DeepL 翻译") };
        foreach (var (key, label) in engines)
        {
            var item = new MenuItem
            {
                Header = label,
                IsChecked = AppSettings.Current.TranslationEngine.Equals(key, StringComparison.OrdinalIgnoreCase),
                Tag = key
            };
            item.Click += (_, _) =>
            {
                AppSettings.Current.TranslationEngine = key;
                foreach (MenuItem mi in settingsItem.Items)
                {
                    mi.IsChecked = mi.Tag?.ToString() == key;
                }
            };
            settingsItem.Items.Add(item);
        }

        menu.Items.Add(pasteItem);
        menu.Items.Add(fileItem);
        menu.Items.Add(regionItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(historyItem);
        menu.Items.Add(settingsItem);

        // API 设置
        var apiSettingsItem = new MenuItem { Header = "API 设置" };
        apiSettingsItem.Click += (_, _) =>
        {
            var settingsWindow = new SettingsWindow
            {
                DataContext = new ViewModels.SettingsViewModel()
            };
            settingsWindow.Show();
            menu.IsOpen = false;
        };
        menu.Items.Add(apiSettingsItem);

        menu.IsOpen = true;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        if (sender is System.Windows.Controls.Button pinBtn)
        {
            pinBtn.Background = Topmost
                ? (System.Windows.Media.Brush)FindResource("ClaudePrimaryMuted")
                : System.Windows.Media.Brushes.Transparent;
            pinBtn.ToolTip = Topmost ? "取消置顶" : "始终置顶";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
        else if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.C)
        {
            if (DataContext is FloatingWindowViewModel vm && !string.IsNullOrEmpty(vm.TranslatedText))
            {
                System.Windows.Clipboard.SetText(vm.TranslatedText);
                vm.ResultText = "✅ 译文已复制到剪贴板";
                e.Handled = true;
            }
        }
    }

    private void SourceLangBorder_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Space)
        {
            SourceLangBorder_MouseLeftButtonDown(sender, null!);
            e.Handled = true;
        }
    }

    private void TargetLangBorder_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Space)
        {
            TargetLangBorder_MouseLeftButtonDown(sender, null!);
            e.Handled = true;
        }
    }

    private void SourceTextBorder_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Space)
        {
            SourceText_MouseLeftButtonDown(sender, null!);
            e.Handled = true;
        }
    }

    private void TranslatedTextBorder_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Space)
        {
            TranslatedText_MouseLeftButtonDown(sender, null!);
            e.Handled = true;
        }
    }

    private void SourceLangBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is FloatingWindowViewModel vm)
        {
            vm.SwitchSourceLangCommand.Execute(null);
        }
        e.Handled = true;
    }

    private void TargetLangBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is FloatingWindowViewModel vm)
        {
            vm.SwitchTargetLangCommand.Execute(null);
        }
        e.Handled = true;
    }

    private void SourceText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is FloatingWindowViewModel vm && !string.IsNullOrEmpty(vm.SourceText))
        {
            try
            {
                System.Windows.Clipboard.SetText(vm.SourceText);
                // 显示复制成功提示
                vm.ResultText = "✅ 原文已复制到剪贴板";
            }
            catch { }
        }
        e.Handled = true;
    }

    private void TranslatedText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is FloatingWindowViewModel vm && !string.IsNullOrEmpty(vm.TranslatedText))
        {
            try
            {
                System.Windows.Clipboard.SetText(vm.TranslatedText);
                // 显示复制成功提示
                vm.ResultText = "✅ 译文已复制到剪贴板";
            }
            catch { }
        }
        e.Handled = true;
    }
}
