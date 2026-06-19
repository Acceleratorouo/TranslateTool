using TranslateTool.Utils;
using Xunit;

namespace TranslateTool.Tests;

public class HotkeyParserTests
{
    [Fact]
    public void Parse_CombinesModifierFlagsAndFunctionKey()
    {
        var hotkey = HotkeyParser.Parse("Ctrl+Shift+Win", "F12");

        Assert.Equal(
            (uint)(NativeMethods.ModControl | NativeMethods.ModShift | NativeMethods.ModWin),
            hotkey.Modifiers);
        Assert.Equal(0x7Bu, hotkey.VirtualKey);
    }

    [Theory]
    [InlineData("Space", 0x20)]
    [InlineData("`", 0xC0)]
    [InlineData("-", 0xBD)]
    [InlineData("=", 0xBB)]
    [InlineData("[", 0xDB)]
    [InlineData("]", 0xDD)]
    [InlineData("\\", 0xDC)]
    [InlineData(";", 0xBA)]
    [InlineData("'", 0xDE)]
    [InlineData(",", 0xBC)]
    [InlineData(".", 0xBE)]
    [InlineData("/", 0xBF)]
    public void Parse_MapsSymbolKeys(string key, uint expectedVirtualKey)
    {
        var hotkey = HotkeyParser.Parse("Alt", key);

        Assert.Equal((uint)NativeMethods.ModAlt, hotkey.Modifiers);
        Assert.Equal(expectedVirtualKey, hotkey.VirtualKey);
    }

    [Fact]
    public void Parse_UsesFallbackKeyForUnknownKey()
    {
        var hotkey = HotkeyParser.Parse("Ctrl", "Unknown");

        Assert.Equal((uint)NativeMethods.ModControl, hotkey.Modifiers);
        Assert.Equal(NativeMethods.VK_T, hotkey.VirtualKey);
    }
}
