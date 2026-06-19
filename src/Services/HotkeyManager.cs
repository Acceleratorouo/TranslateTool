using System.Runtime.InteropServices;
using TranslateTool.Models;
using TranslateTool.Utils;

namespace TranslateTool.Services;

/// <summary>
/// 负责注册/注销全局热键。
/// </summary>
internal static class HotkeyManager
{
    public static void RegisterAllHotkeys(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        // 先注销所有热键
        UnregisterAllHotkeys(hWnd);

        // 注册显示/隐藏悬浮窗热键
        if (!string.IsNullOrEmpty(AppSettings.Current.HotkeyKey))
        {
            var toggleHotkey = HotkeyParser.Parse(
                AppSettings.Current.HotkeyModifiers,
                AppSettings.Current.HotkeyKey);
            RegisterHotKeyWithErrorHandling(
                hWnd,
                NativeMethods.HOTKEY_TOGGLE_WINDOW,
                toggleHotkey.Modifiers,
                toggleHotkey.VirtualKey,
                $"{AppSettings.Current.HotkeyModifiers}+{AppSettings.Current.HotkeyKey}（显示/隐藏悬浮窗）");
        }

        // 注册划词翻译热键
        if (!string.IsNullOrEmpty(AppSettings.Current.SelectionTranslateHotkeyKey))
        {
            var selectionHotkey = HotkeyParser.Parse(
                AppSettings.Current.SelectionTranslateHotkeyModifiers,
                AppSettings.Current.SelectionTranslateHotkeyKey);
            RegisterHotKeyWithErrorHandling(
                hWnd,
                NativeMethods.HOTKEY_SELECTION_TRANSLATE,
                selectionHotkey.Modifiers,
                selectionHotkey.VirtualKey,
                $"{AppSettings.Current.SelectionTranslateHotkeyModifiers}+{AppSettings.Current.SelectionTranslateHotkeyKey}（划词翻译）");
        }

        // 注册框选翻译热键
        if (!string.IsNullOrEmpty(AppSettings.Current.RegionTranslateHotkeyKey))
        {
            var regionHotkey = HotkeyParser.Parse(
                AppSettings.Current.RegionTranslateHotkeyModifiers,
                AppSettings.Current.RegionTranslateHotkeyKey);
            RegisterHotKeyWithErrorHandling(
                hWnd,
                NativeMethods.HOTKEY_REGION_TRANSLATE,
                regionHotkey.Modifiers,
                regionHotkey.VirtualKey,
                $"{AppSettings.Current.RegionTranslateHotkeyModifiers}+{AppSettings.Current.RegionTranslateHotkeyKey}（框选翻译）");
        }
    }

    public static void UnregisterAllHotkeys(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(hWnd, NativeMethods.HOTKEY_TOGGLE_WINDOW);
        NativeMethods.UnregisterHotKey(hWnd, NativeMethods.HOTKEY_SELECTION_TRANSLATE);
        NativeMethods.UnregisterHotKey(hWnd, NativeMethods.HOTKEY_REGION_TRANSLATE);
    }

    private static void RegisterHotKeyWithErrorHandling(
        IntPtr hWnd,
        int id,
        uint modifiers,
        uint vk,
        string hotkeyDisplayName)
    {
        if (NativeMethods.RegisterHotKey(hWnd, id, modifiers, vk))
        {
            return;
        }

        var errorCode = (uint)Marshal.GetLastWin32Error();
        var errorMessage = NativeMethods.GetHotKeyErrorMessage(errorCode);
        var fullMessage = $"{hotkeyDisplayName}注册失败：{errorMessage}\n\n请在设置中更换热键。";

        System.Diagnostics.Debug.WriteLine(
            $"RegisterHotKey failed for {hotkeyDisplayName}. Error code: {errorCode}");

        System.Windows.MessageBox.Show(
            fullMessage,
            "热键注册失败",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Warning);
    }
}
