namespace TranslateTool.Models;

public class TranslationResult
{
    public string SourceText { get; set; } = "";
    public string TranslatedText { get; set; } = "";
    public string SourceLanguage { get; set; } = "";
    public string TargetLanguage { get; set; } = "";
    public string Engine { get; set; } = "";
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    // 评分相关属性
    public double Score { get; set; }
    public bool IsRecommended => Score >= 80;
    public string ScoreReason { get; set; } = "";
}
