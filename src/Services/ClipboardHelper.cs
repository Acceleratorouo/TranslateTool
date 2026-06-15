using System.Windows;

namespace TranslateTool.Services;

public static class ClipboardHelper
{
    public static string GetClipboardText()
    {
        try
        {
            if (System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.Text))
                return System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.Text);
            return "";
        }
        catch
        {
            return "";
        }
    }

    public static void SetClipboardText(string text)
    {
        try
        {
            System.Windows.Clipboard.SetText(text, System.Windows.TextDataFormat.Text);
        }
        catch { /* Ignore clipboard errors */ }
    }
}
