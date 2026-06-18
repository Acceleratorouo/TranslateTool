using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TranslateTool.Models;
using TranslateTool.Services;
using TranslateTool.ViewModels;

namespace TranslateTool.Views;

public partial class FloatingWindow : Window
{
    private const int ResizeBorder = 6; // 边缘可拖拽区域像素

    // 贴边隐藏相关
    private enum DockState { Normal, DockedLeft, DockedRight, DockedTop, DockedBottom }
    private DockState _currentDockState = DockState.Normal;
    private double _normalWidth = 320;
    private double _normalHeight = 400;
    private bool _isDragging = false;
    private bool _isMouseOver = false;
    private bool _isAnimating = false;
    private System.Windows.Point _dockSnapThreshold = new(10, 10); // 贴边阈值

    public FloatingWindow()
    {
        InitializeComponent();
        LoadIcon();
        AllowDrop = true;
        Drop += FloatingWindow_Drop;
        DragEnter += FloatingWindow_DragEnter;
        Closing += FloatingWindow_Closing;
        Closed += FloatingWindow_Closed;
        LocationChanged += FloatingWindow_LocationChanged;
        MouseEnter += FloatingWindow_MouseEnter;
        MouseLeave += FloatingWindow_MouseLeave;
        SourceInitialized += (_, _) =>
        {
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);
        };
        // 初始化正常尺寸
        _normalWidth = Width;
        _normalHeight = Height;
    }

    private void LoadIcon()
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "TranslateTool.ico");
        if (File.Exists(iconPath))
        {
            Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
        }
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
            _isDragging = true;
            DragMove();
            _isDragging = false;
            // 拖动结束后检测是否需要贴边
            CheckAndDock();
        }
    }

    private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Allow DragMove to be triggered by the actual title bar area
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
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
        var engines = EngineStatus.GetAll();
        foreach (var status in engines)
        {
            var item = new MenuItem
            {
                Header = $"{status.GetStatusIcon()} {status.Label}",
                IsChecked = AppSettings.Current.TranslationEngine.Equals(status.Name, StringComparison.OrdinalIgnoreCase),
                Tag = status.Name
            };
            item.Click += (_, _) =>
            {
                AppSettings.Current.TranslationEngine = status.Name;
                foreach (MenuItem mi in settingsItem.Items)
                {
                    mi.IsChecked = mi.Tag?.ToString() == status.Name;
                }
            };
            settingsItem.Items.Add(item);
        }

        // 源语言子菜单
        var sourceLangItem = new MenuItem { Header = "源语言" };
        if (DataContext is FloatingWindowViewModel vm)
        {
            foreach (var lang in vm.Languages)
            {
                var item = new MenuItem
                {
                    Header = lang,
                    IsChecked = lang == vm.SourceLanguage,
                    Tag = lang
                };
                item.Click += (_, _) =>
                {
                    vm.SourceLanguage = lang;
                    foreach (MenuItem mi in sourceLangItem.Items)
                    {
                        mi.IsChecked = mi.Tag?.ToString() == lang;
                    }
                };
                sourceLangItem.Items.Add(item);
            }

            // 目标语言子菜单
            var targetLangItem = new MenuItem { Header = "目标语言" };
            foreach (var lang in vm.Languages)
            {
                var item = new MenuItem
                {
                    Header = lang,
                    IsChecked = lang == vm.TargetLanguage,
                    Tag = lang
                };
                item.Click += (_, _) =>
                {
                    vm.TargetLanguage = lang;
                    foreach (MenuItem mi in targetLangItem.Items)
                    {
                        mi.IsChecked = mi.Tag?.ToString() == lang;
                    }
                };
                targetLangItem.Items.Add(item);
            }

            // 交换语言
            var swapItem = new MenuItem { Header = "⇄ 交换语言" };
            swapItem.Click += (_, _) =>
            {
                vm.SwapLanguagesCommand.Execute(null);
            };

            menu.Items.Add(pasteItem);
            menu.Items.Add(fileItem);
            menu.Items.Add(regionItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(historyItem);
            menu.Items.Add(settingsItem);
            menu.Items.Add(sourceLangItem);
            menu.Items.Add(targetLangItem);
            menu.Items.Add(swapItem);
        }
        else
        {
            menu.Items.Add(pasteItem);
            menu.Items.Add(fileItem);
            menu.Items.Add(regionItem);
            menu.Items.Add(new Separator());
            menu.Items.Add(historyItem);
            menu.Items.Add(settingsItem);
        }

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
            ShowLanguageMenu(vm, isSource: true);
        }
        e.Handled = true;
    }

    private void TargetLangBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is FloatingWindowViewModel vm)
        {
            ShowLanguageMenu(vm, isSource: false);
        }
        e.Handled = true;
    }

    /// <summary>
    /// 弹出语言选择菜单
    /// </summary>
    private void ShowLanguageMenu(FloatingWindowViewModel vm, bool isSource)
    {
        var menu = new ContextMenu();
        var currentLang = isSource ? vm.SourceLanguage : vm.TargetLanguage;

        foreach (var lang in vm.Languages)
        {
            var item = new MenuItem
            {
                Header = lang,
                IsChecked = lang == currentLang,
                Tag = lang
            };
            item.Click += (_, _) =>
            {
                if (isSource)
                    vm.SourceLanguage = lang;
                else
                    vm.TargetLanguage = lang;
            };
            menu.Items.Add(item);
        }

        menu.IsOpen = true;
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

    #region 贴边隐藏功能

    /// <summary>
    /// 位置改变时检测贴边
    /// </summary>
    private void FloatingWindow_LocationChanged(object? sender, EventArgs e)
    {
        // 仅在用户拖动时检测
        if (_isDragging && !_isAnimating)
        {
            CheckAndDock();
        }
    }

    /// <summary>
    /// 鼠标进入窗口时展开
    /// </summary>
    private void FloatingWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isMouseOver = true;
        if (_currentDockState != DockState.Normal && !_isAnimating)
        {
            Undock();
        }
    }

    /// <summary>
    /// 鼠标离开窗口时收起
    /// </summary>
    private void FloatingWindow_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isMouseOver = false;
    }

    /// <summary>
    /// 检测并执行贴边收起
    /// </summary>
    private void CheckAndDock()
    {
        if (!AppSettings.Current.EnableDockHide) return;
        if (_currentDockState != DockState.Normal) return;
        if (_isAnimating) return;
        if (WindowState != WindowState.Normal) return;

        var workArea = SystemParameters.WorkArea;
        var threshold = _dockSnapThreshold.X;

        // 记录当前尺寸
        _normalWidth = ActualWidth;
        _normalHeight = ActualHeight;

        // 左侧贴边
        if (Left < threshold)
        {
            DockToLeft();
        }
        // 右侧贴边
        else if (Left + ActualWidth > workArea.Right - threshold)
        {
            DockToRight();
        }
        // 底部贴边
        else if (Top + ActualHeight > workArea.Bottom - threshold)
        {
            DockToBottom();
        }
        // 顶部贴边（较少用）
        else if (Top < threshold)
        {
            DockToTop();
        }
    }

    /// <summary>
    /// 贴边到左侧
    /// </summary>
    private void DockToLeft()
    {
        _currentDockState = DockState.DockedLeft;
        SavePosition();
        Left = 0;
        AnimateToSize(8, _normalHeight);
    }

    /// <summary>
    /// 贴边到右侧
    /// </summary>
    private void DockToRight()
    {
        _currentDockState = DockState.DockedRight;
        SavePosition();
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - 8;
        AnimateToSize(8, _normalHeight);
    }

    /// <summary>
    /// 贴边到顶部
    /// </summary>
    private void DockToTop()
    {
        _currentDockState = DockState.DockedTop;
        SavePosition();
        AnimateToSize(_normalWidth, 8);
    }

    /// <summary>
    /// 贴边到底部
    /// </summary>
    private void DockToBottom()
    {
        _currentDockState = DockState.DockedBottom;
        SavePosition();
        var workArea = SystemParameters.WorkArea;
        Top = workArea.Bottom - 8;
        AnimateToSize(_normalWidth, 8);
    }

    /// <summary>
    /// 从贴边状态展开
    /// </summary>
    private void Undock()
    {
        if (_currentDockState == DockState.Normal) return;

        var workArea = SystemParameters.WorkArea;

        switch (_currentDockState)
        {
            case DockState.DockedLeft:
                Left = 0;
                AnimateToSize(_normalWidth, _normalHeight);
                break;
            case DockState.DockedRight:
                Left = workArea.Right - _normalWidth;
                AnimateToSize(_normalWidth, _normalHeight);
                break;
            case DockState.DockedTop:
                Top = 0;
                AnimateToSize(_normalWidth, _normalHeight);
                break;
            case DockState.DockedBottom:
                Top = workArea.Bottom - _normalHeight;
                AnimateToSize(_normalWidth, _normalHeight);
                break;
        }

        _currentDockState = DockState.Normal;
    }

    /// <summary>
    /// 动画改变窗口尺寸
    /// </summary>
    private void AnimateToSize(double targetWidth, double targetHeight)
    {
        _isAnimating = true;
        var duration = TimeSpan.FromMilliseconds(200);

        var widthAnimation = new DoubleAnimation(targetWidth, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        var heightAnimation = new DoubleAnimation(targetHeight, duration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        widthAnimation.Completed += (_, _) => _isAnimating = false;

        BeginAnimation(WidthProperty, widthAnimation);
        BeginAnimation(HeightProperty, heightAnimation);
    }

    /// <summary>
    /// 保存位置到设置
    /// </summary>
    private void SavePosition()
    {
        AppSettings.Current.FloatingWindowTop = Top;
        AppSettings.Current.FloatingWindowLeft = Left;
        AppSettings.Current.Save();
    }

    #endregion
}
