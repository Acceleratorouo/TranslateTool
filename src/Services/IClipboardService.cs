namespace TranslateTool.Services;

public interface IClipboardService
{
    string GetText();

    void SetText(string text);
}
