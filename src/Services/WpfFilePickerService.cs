namespace TranslateTool.Services;

public sealed class WpfFilePickerService : IFilePickerService
{
    public string? PickFileForTranslation()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "支持的文件|*.txt;*.docx;*.pdf|文本文件|*.txt|Word 文档|*.docx|PDF 文件|*.pdf",
            Title = "选择要翻译的文件"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
