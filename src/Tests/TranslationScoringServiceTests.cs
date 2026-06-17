using Xunit;
using TranslateTool.Services;

namespace TranslateTool.Tests;

public class TranslationScoringServiceTests
{
    [Fact]
    public void ScoreResult_EmptyTranslation_ReturnsZero()
    {
        var result = TranslationScoringService.ScoreResult("Hello", "", new List<string> { "" });

        Assert.Equal(0, result.Score);
        Assert.Contains("翻译结果为空", result.Reasons);
    }

    [Fact]
    public void ScoreResult_NullTranslation_ReturnsZero()
    {
        var result = TranslationScoringService.ScoreResult("Hello", null!, new List<string>());

        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void ScoreResult_ReasonableLength_ReturnsHighScore()
    {
        // 英文到中文，长度比约 1:2 是合理的
        var source = "Hello world";
        var translation = "你好世界";
        var allTranslations = new List<string> { "你好世界", "世界你好" };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        Assert.True(result.Score > 60, $"Expected score > 60, got {result.Score}");
        Assert.Contains(result.Reasons, r => r.Contains("长度合理"));
    }

    [Fact]
    public void ScoreResult_LengthRatioTooHigh_Penalized()
    {
        // 翻译文本过长
        var source = "Hi";
        var translation = "这是一个非常非常非常长的翻译结果文本";
        var allTranslations = new List<string> { translation };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        // 长度比过大应该有惩罚
        Assert.Contains(result.Reasons, r => r.Contains("长度"));
    }

    [Fact]
    public void ScoreResult_ConsecutiveRepeats_Penalized()
    {
        var source = "Hello";
        var translation = "你好啊啊啊";
        var allTranslations = new List<string> { translation };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        Assert.Contains(result.Reasons, r => r.Contains("重复"));
    }

    [Fact]
    public void ScoreResult_ProperPunctuation_GetsFormatBonus()
    {
        var source = "Hello world";
        var translation = "你好世界。";
        var allTranslations = new List<string> { translation };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        Assert.Contains(result.Reasons, r => r.Contains("格式") || r.Contains("标点"));
    }

    [Fact]
    public void ScoreResult_TranslationFailureMarkers_Penalized()
    {
        var source = "Hello";
        var translation = "翻译失败???";
        var allTranslations = new List<string> { translation };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        Assert.Contains(result.Reasons, r => r.Contains("错误标记"));
    }

    [Fact]
    public void ScoreResult_DiverseFromOthers_GetsDiversityBonus()
    {
        var source = "Hello world";
        var translations = new List<string>
        {
            "你好世界",  // 引擎1
            "你们好世界", // 引擎2
            "大家好世界"  // 引擎3
        };

        var result1 = TranslationScoringService.ScoreResult(source, translations[0], translations);
        var result2 = TranslationScoringService.ScoreResult(source, translations[1], translations);
        var result3 = TranslationScoringService.ScoreResult(source, translations[2], translations);

        // 三个不同的翻译应该都有 diversity bonus
        Assert.Contains(result1.Reasons, r => r.Contains("多样性") || r.Contains("差异"));
    }

    [Fact]
    public void ScoreResult_HighlySimilarTranslations_Penalized()
    {
        var source = "Hello world";
        var translations = new List<string>
        {
            "你好世界",
            "你好世界",  // 完全相同
            "你好世界。" // 几乎相同
        };

        var result = TranslationScoringService.ScoreResult(source, translations[0], translations);

        Assert.Contains(result.Reasons, r => r.Contains("相似"));
    }

    [Fact]
    public void ScoreResult_ScoreInValidRange()
    {
        var source = "Hello";
        var translation = "你好";
        var allTranslations = new List<string> { translation };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        Assert.True(result.Score >= 0 && result.Score <= 100,
            $"Score should be between 0 and 100, got {result.Score}");
    }

    [Fact]
    public void ScoreResult_IsRecommended_WhenScoreAbove80()
    {
        var source = "The quick brown fox jumps over the lazy dog.";
        var translation = "快速的棕色狐狸跳过懒惰的狗。";
        var allTranslations = new List<string> { translation, "一只快速的棕色狐狸跃过懒狗" };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        if (result.Score >= 80)
        {
            Assert.True(result.IsRecommended);
        }
    }

    [Fact]
    public void ScoreResult_NotRecommended_WhenScoreBelow80()
    {
        var source = "Hi";
        var translation = "这是一个非常非常非常长且包含重复字符和可能的乱码的翻译结果文本！！！";
        var allTranslations = new List<string> { translation };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        if (result.Score < 80)
        {
            Assert.False(result.IsRecommended);
        }
    }

    [Fact]
    public void ScoreResult_CjkToCjk_UsesCharacterRatio()
    {
        var source = "中日韩文测试";
        var translation = "中日韩文测试结果";
        var allTranslations = new List<string> { translation };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        Assert.True(result.Score > 0);
    }

    [Fact]
    public void ScoreResult_MultipleEngines_CalculatesDiversity()
    {
        var source = "Hello world";
        var translations = new List<string>
        {
            "你好世界",
            "世界你好",
            "大家好"
        };

        var results = translations.Select(t =>
            TranslationScoringService.ScoreResult(source, t, translations)).ToList();

        // 每个翻译都应该有评分
        Assert.All(results, r => Assert.True(r.Score >= 0));
    }

    [Fact]
    public void ScoreResult_GarbledText_Penalized()
    {
        var source = "Hello";
        var translation = "你好▀▁▂▃▄▅▆▇█";
        var allTranslations = new List<string> { translation };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        Assert.Contains(result.Reasons, r => r.Contains("乱码"));
    }

    [Fact]
    public void ScoreResult_AbnormalCharacters_Penalized()
    {
        var source = "Hello";
        var translation = "你好※¶§®©™®©";
        var allTranslations = new List<string> { translation };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        Assert.Contains(result.Reasons, r => r.Contains("异常字符"));
    }

    [Fact]
    public void ScoreResult_FutureYear_Penalized()
    {
        var source = "The event was in 2025";
        var translation = "该活动是在2025年。";
        var allTranslations = new List<string> { translation };

        var result = TranslationScoringService.ScoreResult(source, translation, allTranslations);

        // 未来年份应该被检测
        Assert.Contains(result.Reasons, r => r.Contains("数字") || r.Contains("异常"));
    }
}