using System.Runtime.InteropServices;

namespace TranslateTool.Utils;

public static class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id,
        UInt32 fsModifiers, UInt32 vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public const int ModControl = 0x0002;
    public const int ModShift = 0x0004;

    // 热键 ID
    public const int HOTKEY_TOGGLE_WINDOW = 0xF001;
    public const int HOTKEY_SELECTION_TRANSLATE = 0xF002;
    public const int HOTKEY_REGION_TRANSLATE = 0xF003;

    // 虚拟键码
    public const uint VK_X = 0x58;
    public const uint VK_T = 0x54;
    public const uint VK_R = 0x52;

    // 系统错误码
    public const uint ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

    public static string GetHotKeyErrorMessage(uint errorCode)
    {
        return errorCode switch
        {
            ERROR_HOTKEY_ALREADY_REGISTERED => "该热键已被其他程序占用（如输入法、截图工具等）。",
            _ => $"注册热键失败，系统错误码：{errorCode}"
        };
    }
}
