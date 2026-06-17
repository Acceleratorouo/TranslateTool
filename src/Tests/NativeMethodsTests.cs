using Xunit;
using TranslateTool.Utils;

namespace TranslateTool.Tests;

public class NativeMethodsTests
{
    [Fact]
    public void GetHotKeyErrorMessage_HotkeyAlreadyRegistered_ReturnsOccupiedMessage()
    {
        var message = NativeMethods.GetHotKeyErrorMessage(NativeMethods.ERROR_HOTKEY_ALREADY_REGISTERED);

        Assert.Contains("已被其他程序占用", message);
    }

    [Fact]
    public void GetHotKeyErrorMessage_UnknownErrorCode_ReturnsSystemErrorMessage()
    {
        const uint errorCode = 12345;

        var message = NativeMethods.GetHotKeyErrorMessage(errorCode);

        Assert.Contains("注册热键失败", message);
        Assert.Contains(errorCode.ToString(), message);
    }
}
