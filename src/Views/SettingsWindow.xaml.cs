using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TranslateTool.ViewModels;

namespace TranslateTool.Views;

public partial class SettingsWindow : Window
{
    /// <summary>
    /// 当前正在录制的目标（null 表示未在录制）
    /// </summary>
    private string? _recordingTarget;

    public SettingsWindow()
    {
        InitializeComponent();
        LoadIcon();
        // 在 Window 级别监听按键，确保录制时能捕获所有按键
        PreviewKeyDown += SettingsWindow_PreviewKeyDown;
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
    /// 点击 Border 进入录制模式
    /// </summary>
    private void HotkeyBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border) return;
        _recordingTarget = border.Tag?.ToString();
        border.Focus();
        UpdateRecordingVisual(border, isRecording: true);
        e.Handled = true;
    }

    /// <summary>
    /// 清除快捷键按钮
    /// </summary>
    private void ClearHotkeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;
        if (DataContext is not SettingsViewModel vm) return;

        switch (btn.Tag?.ToString())
        {
            case "ToggleWindow":
                vm.HotkeyModifiers = "";
                vm.HotkeyKey = "";
                break;
            case "SelectionTranslate":
                vm.SelectionTranslateHotkeyModifiers = "";
                vm.SelectionTranslateHotkeyKey = "";
                break;
            case "RegionTranslate":
                vm.RegionTranslateHotkeyModifiers = "";
                vm.RegionTranslateHotkeyKey = "";
                break;
        }

        // 取消录制状态
        _recordingTarget = null;
    }

    /// <summary>
    /// Border 获得焦点时，如果正在录制则更新视觉
    /// </summary>
    private void HotkeyBorder_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not Border border) return;
        if (_recordingTarget == border.Tag?.ToString())
        {
            UpdateRecordingVisual(border, isRecording: true);
        }
    }

    /// <summary>
    /// Border 失去焦点时恢复显示
    /// </summary>
    private void HotkeyBorder_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not Border border) return;
        if (_recordingTarget == border.Tag?.ToString())
        {
            _recordingTarget = null;
        }
        UpdateRecordingVisual(border, isRecording: false);
    }

    /// <summary>
    /// Window 级别按键拦截：录制时捕获按键
    /// </summary>
    private void SettingsWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (string.IsNullOrEmpty(_recordingTarget)) return;

        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // 忽略单独的修饰键
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin
            or Key.Capital or Key.NumLock or Key.Scroll)
        {
            return;
        }

        // 至少需要一个修饰键
        var mods = Keyboard.Modifiers;
        if (mods == ModifierKeys.None)
        {
            return; // 等待用户按下修饰键组合
        }

        // 解析修饰键字符串
        var modStr = "";
        if (mods.HasFlag(ModifierKeys.Control)) modStr += "Ctrl+";
        if (mods.HasFlag(ModifierKeys.Shift)) modStr += "Shift+";
        if (mods.HasFlag(ModifierKeys.Alt)) modStr += "Alt+";
        if (mods.HasFlag(ModifierKeys.Windows)) modStr += "Win+";
        modStr = modStr.TrimEnd('+');

        // 解析主键
        var keyStr = KeyToString(key);
        if (keyStr == null) return;

        // 更新 ViewModel
        if (DataContext is SettingsViewModel vm)
        {
            switch (_recordingTarget)
            {
                case "ToggleWindow":
                    vm.HotkeyModifiers = modStr;
                    vm.HotkeyKey = keyStr;
                    break;
                case "SelectionTranslate":
                    vm.SelectionTranslateHotkeyModifiers = modStr;
                    vm.SelectionTranslateHotkeyKey = keyStr;
                    break;
                case "RegionTranslate":
                    vm.RegionTranslateHotkeyModifiers = modStr;
                    vm.RegionTranslateHotkeyKey = keyStr;
                    break;
            }
        }

        // 找到当前录制的 Border 并更新显示
        var targetBorder = _recordingTarget switch
        {
            "ToggleWindow" => ToggleWindowHotkeyBox,
            "SelectionTranslate" => SelectionTranslateHotkeyBox,
            "RegionTranslate" => RegionTranslateHotkeyBox,
            _ => null
        };

        if (targetBorder != null)
        {
            if (targetBorder.Child is TextBlock tb)
            {
                tb.Text = $"✅ {modStr}+{keyStr}";
            }
            // 高亮边框
            targetBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("ClaudePrimary");
        }

        // 结束录制
        _recordingTarget = null;
        Keyboard.ClearFocus();
    }

    /// <summary>
    /// 更新录制状态的视觉效果
    /// </summary>
    private void UpdateRecordingVisual(Border border, bool isRecording)
    {
        if (border.Child is not TextBlock tb) return;
        if (isRecording)
        {
            tb.Text = "🎹 请按下快捷键…";
            border.BorderBrush = (System.Windows.Media.Brush)FindResource("ClaudePrimary");
        }
        else
        {
            // 恢复绑定显示
            if (DataContext is SettingsViewModel vm)
            {
                tb.Text = border.Tag?.ToString() switch
                {
                    "ToggleWindow" => vm.ToggleWindowHotkeyDisplay,
                    "SelectionTranslate" => vm.SelectionTranslateHotkeyDisplay,
                    "RegionTranslate" => vm.RegionTranslateHotkeyDisplay,
                    _ => tb.Text
                };
            }
            border.BorderBrush = (System.Windows.Media.Brush)FindResource("ClaudeBorder");
        }
    }

    /// <summary>
    /// 将 WPF Key 转换为可读字符串
    /// </summary>
    private static string? KeyToString(Key key)
    {
        // A-Z
        if (key >= Key.A && key <= Key.Z)
            return key.ToString();

        // 0-9
        if (key >= Key.D0 && key <= Key.D9)
            return key.ToString().Substring(1); // D0 -> 0

        // NumPad 0-9
        if (key >= Key.NumPad0 && key <= Key.NumPad9)
            return key.ToString().Substring("NumPad".Length);

        // F1-F24
        if (key >= Key.F1 && key <= Key.F24)
            return key.ToString();

        return key switch
        {
            Key.Space => "Space",
            Key.OemTilde => "`",
            Key.OemMinus => "-",
            Key.OemPlus => "=",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe => "\\",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion => "/",
            _ => null
        };
    }
}
