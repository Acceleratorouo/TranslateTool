using System.IO;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace TranslateTool.Services;

public static class FileTranslationService
{
    /// <summary>
    /// Extract text from a file (.txt, .docx, .pdf)
    /// </summary>
    public static string ExtractText(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".txt" => File.ReadAllText(filePath, Encoding.UTF8),
            ".docx" => ExtractDocxText(filePath),
            ".pdf" => ExtractPdfText(filePath),
            _ => throw new NotSupportedException($"不支持的文件格式: {ext}")
        };
    }

    private static string ExtractDocxText(string filePath)
    {
        using var doc = WordprocessingDocument.Open(filePath, false);
        var sb = new StringBuilder();
        foreach (var para in doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>())
        {
            sb.AppendLine(para.InnerText);
        }
        return sb.ToString().Trim();
    }

    private static string ExtractPdfText(string filePath)
    {
        using var doc = PdfDocument.Open(filePath);
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString().Trim();
    }
}
