using TranslateTool.Utils;

namespace TranslateTool.Services;

/// <summary>
/// 管理应用单实例：获取互斥锁并负责唤醒已有实例。
/// </summary>
internal sealed class SingleInstanceManager : IDisposable
{
    private readonly string _mutexName;
    private readonly string _messageName;
    private readonly string _windowTitle;
    private Mutex? _mutex;
    private uint _windowMessage;

    public SingleInstanceManager(string mutexName, string messageName, string windowTitle)
    {
        _mutexName = mutexName;
        _messageName = messageName;
        _windowTitle = windowTitle;
    }

    /// <summary>
    /// 是否成功获取单实例所有权。
    /// </summary>
    public bool IsOwned => _mutex != null;

    /// <summary>
    /// 用于唤醒已有实例的自定义窗口消息。
    /// </summary>
    public uint WindowMessage => _windowMessage;

    /// <summary>
    /// 尝试获取单实例所有权。若已有实例运行则返回 false。
    /// </summary>
    public bool TryAcquireOwnership()
    {
        _mutex = new Mutex(true, _mutexName, out bool createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            return false;
        }

        _windowMessage = NativeMethods.RegisterWindowMessage(_messageName);
        return true;
    }

    /// <summary>
    /// 唤醒已有实例到前台。
    /// </summary>
    public void WakeupExistingInstance()
    {
        if (_windowMessage == 0)
        {
            _windowMessage = NativeMethods.RegisterWindowMessage(_messageName);
        }

        var hwnd = NativeMethods.FindWindow(null, _windowTitle);
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        if (NativeMethods.IsIconic(hwnd))
        {
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
        }
        else
        {
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
        }

        NativeMethods.SetForegroundWindow(hwnd);

        if (_windowMessage != 0)
        {
            NativeMethods.SendMessage(hwnd, _windowMessage, IntPtr.Zero, IntPtr.Zero);
        }
    }

    public void Dispose()
    {
        try
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
        catch
        {
        }

        _mutex = null;
    }
}
