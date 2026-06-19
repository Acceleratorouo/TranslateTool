namespace TranslateTool.Utils;

public readonly record struct ParsedHotkey(uint Modifiers, uint VirtualKey);

public static class HotkeyParser
{
    public static ParsedHotkey Parse(string? modifiersText, string? keyText)
    {
        uint modifiers = 0;
        if ((modifiersText ?? string.Empty).Contains("Ctrl", StringComparison.OrdinalIgnoreCase))
            modifiers |= NativeMethods.ModControl;
        if ((modifiersText ?? string.Empty).Contains("Shift", StringComparison.OrdinalIgnoreCase))
            modifiers |= NativeMethods.ModShift;
        if ((modifiersText ?? string.Empty).Contains("Alt", StringComparison.OrdinalIgnoreCase))
            modifiers |= NativeMethods.ModAlt;
        if ((modifiersText ?? string.Empty).Contains("Win", StringComparison.OrdinalIgnoreCase))
            modifiers |= NativeMethods.ModWin;

        var key = (keyText ?? string.Empty).Trim();
        var virtualKey = key.ToUpperInvariant() switch
        {
            var c when c.Length == 1 && c[0] >= 'A' && c[0] <= 'Z' => (uint)(c[0] - 'A' + 0x41),
            var c when c.Length == 1 && c[0] >= '0' && c[0] <= '9' => (uint)(c[0] - '0' + 0x30),
            var f when f.StartsWith('F') && int.TryParse(f[1..], out var fn) && fn >= 1 && fn <= 24 => (uint)(0x70 + fn - 1),
            "SPACE" => 0x20u,
            "`" => 0xC0u,
            "-" => 0xBDu,
            "=" => 0xBBu,
            "[" => 0xDBu,
            "]" => 0xDDu,
            "\\" => 0xDCu,
            ";" => 0xBAu,
            "'" => 0xDEu,
            "," => 0xBCu,
            "." => 0xBEu,
            "/" => 0xBFu,
            _ => NativeMethods.VK_T
        };

        return new ParsedHotkey(modifiers, virtualKey);
    }
}
