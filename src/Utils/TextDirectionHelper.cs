using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace TranslateTool.Utils;

/// <summary>
/// 文本方向工具 — 检测文本是否为从右到左（RTL）书写
///
/// 支持检测的语言：
/// - 阿拉伯文 (Arabic, U+0600-U+06FF, U+0750-U+077F, U+08A0-U+08FF, U+FB50-U+FDFF, U+FE70-U+FEFF)
/// - 希伯来文 (Hebrew, U+0590-U+05FF, U+FB1D-U+FB4F)
/// - 波斯文 (Farsi, 与阿拉伯文同码位)
/// - 乌尔都文 (Urdu, 与阿拉伯文同码位)
/// - 也门文/叙利亚文/塔纳文 等其他 RTL 脚本
/// </summary>
public static class TextDirectionHelper
{
    /// <summary>
    /// 判断字符串是否包含 RTL（从右到左）字符
    /// </summary>
    /// <remarks>
    /// 使用 Unicode 双向算法（Bidi）的简化版：扫描第一个强方向字符。
    /// 数字和标点符号会忽略，只看字母/表意文字。
    /// </remarks>
    public static bool IsRtlText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        foreach (var rune in text.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            // 跳过空白、标点、数字、控制字符
            if (category is UnicodeCategory.SpaceSeparator
                or UnicodeCategory.LineSeparator
                or UnicodeCategory.ParagraphSeparator
                or UnicodeCategory.OtherPunctuation
                or UnicodeCategory.DashPunctuation
                or UnicodeCategory.OpenPunctuation
                or UnicodeCategory.ClosePunctuation
                or UnicodeCategory.ConnectorPunctuation
                or UnicodeCategory.InitialQuotePunctuation
                or UnicodeCategory.FinalQuotePunctuation
                or UnicodeCategory.OtherSymbol
                or UnicodeCategory.MathSymbol
                or UnicodeCategory.CurrencySymbol
                or UnicodeCategory.ModifierSymbol
                or UnicodeCategory.OtherNotAssigned
                or UnicodeCategory.Control
                or UnicodeCategory.Format
                or UnicodeCategory.Surrogate
                or UnicodeCategory.PrivateUse
                or UnicodeCategory.DecimalDigitNumber
                or UnicodeCategory.LetterNumber
                or UnicodeCategory.OtherNumber)
            {
                continue;
            }

            // 找到第一个强方向字符
            return IsRtlCodePoint(rune.Value);
        }
        return false;
    }

    /// <summary>
    /// 判断单个码位是否属于 RTL 脚本
    /// </summary>
    public static bool IsRtlCodePoint(int codePoint) => codePoint switch
    {
        // 希伯来文
        >= 0x0590 and <= 0x05FF => true,
        >= 0xFB1D and <= 0xFB4F => true,
        // 阿拉伯文
        >= 0x0600 and <= 0x06FF => true,
        >= 0x0750 and <= 0x077F => true,
        >= 0x08A0 and <= 0x08FF => true,
        >= 0xFB50 and <= 0xFDFF => true,
        >= 0xFE70 and <= 0xFEFF => true,
        // 叙利亚文
        >= 0x0700 and <= 0x074F => true,
        // 塔纳文（马尔代夫）
        >= 0x0780 and <= 0x07BF => true,
        // 西非书面文（N'Ko）
        >= 0x07C0 and <= 0x07FF => true,
        // 提非纳文（柏柏尔）
        >= 0x2D30 and <= 0x2D7F => true,
        // 曼达文
        >= 0x0840 and <= 0x085F => true,
        _ => false
    };

    /// <summary>
    /// 获取 FlowDirection 枚举
    /// </summary>
    public static System.Windows.FlowDirection GetFlowDirection(string? text)
        => IsRtlText(text) ? System.Windows.FlowDirection.RightToLeft : System.Windows.FlowDirection.LeftToRight;
}

/// <summary>
/// XAML 值转换器：string → FlowDirection
/// 用法：FlowDirection="{Binding SourceText, Converter={StaticResource TextToFlowDirectionConverter}}"
/// </summary>
public class TextToFlowDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return TextDirectionHelper.GetFlowDirection(value as string);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
}

/// <summary>
/// XAML 值转换器：string → TextAlignment（RTL 时右对齐，LTR 时左对齐）
/// </summary>
public class TextToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isRtl = TextDirectionHelper.IsRtlText(value as string);
        // 当 parameter 为 "reverse" 时反转结果
        if (parameter is string s && s.Equals("reverse", StringComparison.OrdinalIgnoreCase))
        {
            return isRtl ? TextAlignment.Left : TextAlignment.Right;
        }
        return isRtl ? TextAlignment.Right : TextAlignment.Left;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
}
