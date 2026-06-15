using System.Runtime.InteropServices;

namespace TranslateTool.Utils;

public static class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id,
        UInt32 fsModifiers, UInt32 vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const int ModControl = 0x0002;
    public const int ModShift = 0x0004;
}
