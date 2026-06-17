using Xunit;
using TranslateTool.Utils;
using SysFlowDirection = System.Windows.FlowDirection;
using SysTextAlignment = System.Windows.TextAlignment;

namespace TranslateTool.Tests;

public class TextDirectionHelperTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("Hello", false)]
    [InlineData("你好世界", false)]
    [InlineData("こんにちは", false)]
    [InlineData("Привет", false)]
    public void IsRtlText_LtrText_ReturnsFalse(string? text, bool expected)
    {
        Assert.Equal(expected, TextDirectionHelper.IsRtlText(text));
    }

    [Theory]
    [InlineData("مرحبا", true)]              // Arabic
    [InlineData("שלום", true)]               // Hebrew
    [InlineData("السلام عليكم", true)]       // Arabic phrase
    [InlineData("שלום עולם", true)]          // Hebrew phrase
    [InlineData("سلام", true)]               // Persian/Urdu
    public void IsRtlText_RtlText_ReturnsTrue(string text, bool expected)
    {
        Assert.Equal(expected, TextDirectionHelper.IsRtlText(text));
    }

    [Theory]
    [InlineData("  مرحبا", true)]            // Leading space
    [InlineData("123 مرحبا", true)]          // Leading digits
    [InlineData("...مرحبا", true)]           // Leading punctuation
    public void IsRtlText_IgnoresPunctuationAndDigits(string text, bool expected)
    {
        Assert.Equal(expected, TextDirectionHelper.IsRtlText(text));
    }

    [Fact]
    public void IsRtlText_MixedLtrFirstChar_ReturnsFalse()
    {
        // 当第一个强方向字符是 LTR 时，整体判定为 LTR
        Assert.False(TextDirectionHelper.IsRtlText("Hello مرحبا"));
    }

    [Fact]
    public void IsRtlText_MixedRtlFirstChar_ReturnsTrue()
    {
        // 当第一个强方向字符是 RTL 时，整体判定为 RTL（即使后面有 LTR）
        Assert.True(TextDirectionHelper.IsRtlText("مرحبا Hello"));
    }

    [Theory]
    [InlineData(0x0590, true)]  // Hebrew
    [InlineData(0x05FF, true)]
    [InlineData(0x0600, true)]  // Arabic
    [InlineData(0x06FF, true)]
    [InlineData(0x0700, true)]  // Syriac
    [InlineData(0x0780, true)]  // Thaana
    [InlineData(0x0020, false)] // Space
    [InlineData(0x0041, false)] // A
    [InlineData(0x4E2D, false)] // 中
    public void IsRtlCodePoint_KnownCodepoints(int codePoint, bool expected)
    {
        Assert.Equal(expected, TextDirectionHelper.IsRtlCodePoint(codePoint));
    }

    [Fact]
    public void GetFlowDirection_EmptyString_ReturnsLtr()
    {
        Assert.Equal(SysFlowDirection.LeftToRight, TextDirectionHelper.GetFlowDirection(""));
    }

    [Fact]
    public void GetFlowDirection_ArabicText_ReturnsRtl()
    {
        Assert.Equal(SysFlowDirection.RightToLeft, TextDirectionHelper.GetFlowDirection("مرحبا"));
    }

    [Fact]
    public void GetFlowDirection_EnglishText_ReturnsLtr()
    {
        Assert.Equal(SysFlowDirection.LeftToRight, TextDirectionHelper.GetFlowDirection("Hello"));
    }

    [Fact]
    public void TextToFlowDirectionConverter_ConvertsCorrectly()
    {
        var conv = new TextToFlowDirectionConverter();
        Assert.Equal(SysFlowDirection.RightToLeft, conv.Convert("مرحبا", typeof(SysFlowDirection), null, System.Globalization.CultureInfo.CurrentCulture));
        Assert.Equal(SysFlowDirection.LeftToRight, conv.Convert("Hello", typeof(SysFlowDirection), null, System.Globalization.CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TextToAlignmentConverter_RtlText_ReturnsRight()
    {
        var conv = new TextToAlignmentConverter();
        Assert.Equal(SysTextAlignment.Right, conv.Convert("مرحبا", typeof(SysTextAlignment), null, System.Globalization.CultureInfo.CurrentCulture));
        Assert.Equal(SysTextAlignment.Left, conv.Convert("Hello", typeof(SysTextAlignment), null, System.Globalization.CultureInfo.CurrentCulture));
    }

    [Fact]
    public void TextToAlignmentConverter_ReverseParameter_FlipsResult()
    {
        var conv = new TextToAlignmentConverter();
        Assert.Equal(SysTextAlignment.Left, conv.Convert("مرحبا", typeof(SysTextAlignment), "reverse", System.Globalization.CultureInfo.CurrentCulture));
        Assert.Equal(SysTextAlignment.Right, conv.Convert("Hello", typeof(SysTextAlignment), "reverse", System.Globalization.CultureInfo.CurrentCulture));
    }
}
