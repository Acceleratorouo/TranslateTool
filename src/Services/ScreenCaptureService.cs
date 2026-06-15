using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TranslateTool.Services;

public static class ScreenCaptureService
{
    // Win32 constants needed for non-WinForms screen bounds
    private static class User32
    {
        [DllImport("user32.dll")]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private class MONITORINFO
    {
        public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
        public RECT rcMonitor = new RECT();
    }

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    /// <summary>
    /// 获取主监视器的工作区域（替代 System.Windows.Forms.Screen）
    /// </summary>
    private static Rectangle GetPrimaryScreenBounds()
    {
        IntPtr hMonitor = User32.MonitorFromWindow(IntPtr.Zero, MONITOR_DEFAULTTONEAREST);
        var mi = new MONITORINFO();
        if (User32.GetMonitorInfo(hMonitor, mi))
        {
            return new Rectangle(mi.rcMonitor.Left, mi.rcMonitor.Top,
                                 mi.rcMonitor.Right - mi.rcMonitor.Left,
                                 mi.rcMonitor.Bottom - mi.rcMonitor.Top);
        }
        // Fallback to full desktop bounds
        return new Rectangle(
            (int)System.Windows.SystemParameters.VirtualScreenLeft,
            (int)System.Windows.SystemParameters.VirtualScreenTop,
            (int)System.Windows.SystemParameters.VirtualScreenWidth,
            (int)System.Windows.SystemParameters.VirtualScreenHeight);
    }

    /// <summary>
    /// 截取整个屏幕
    /// </summary>
    public static Bitmap CaptureScreen()
    {
        var bounds = GetPrimaryScreenBounds();
        return CaptureRectangle(bounds);
    }

    /// <summary>
    /// 截取指定矩形区域
    /// </summary>
    public static Bitmap CaptureRectangle(Rectangle bounds)
    {
        var screenshot = new Bitmap(
            bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

        using var g = Graphics.FromImage(screenshot);
        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

        return screenshot;
    }

    /// <summary>
    /// 将 Bitmap 转为字节数组（PNG 格式）
    /// </summary>
    public static byte[] BitmapToBytes(Bitmap bitmap)
    {
        using var ms = new System.IO.MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}
