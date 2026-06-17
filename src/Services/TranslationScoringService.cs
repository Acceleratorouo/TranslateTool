using System.Text.RegularExpressions;

namespace TranslateTool.Services;

/// <summary>
/// 翻译结果评分服务
/// </summary>
public static class TranslationScoringService
{
    /// <summary>
    /// 评分维度权重
    /// </summary>
    private const double LengthWeight = 0.30;
    private const double DiversityWeight = 0.25;
    private const double FormatWeight = 0.20;
    private const double SmoothnessWeight = 0.15;
    private const double RejectionWeight = 0.10;

    /// <summary>
    /// 为翻译结果打分（0-100）
    /// </summary>
    /// <param name="sourceText">源文本</param>
    /// <param name="translatedText">翻译文本</param>
    /// <param name="allTranslations">所有引擎的翻译结果</param>
    /// <returns>评分结果</returns>
    public static ScoringResult ScoreResult(string sourceText, string translatedText,
        IReadOnlyList<string> allTranslations)
    {
        if (string.IsNullOrEmpty(translatedText))
        {
            return new ScoringResult
            {
                Score = 0,
                Reasons = new List<string> { "翻译结果为空" }
            };
        }

        var reasons = new List<string>();
        double totalScore = 0;

        // 1. 长度比评分（30%）
        double lengthScore = ScoreLength(sourceText, translatedText, reasons);
        totalScore += lengthScore * LengthWeight;

        // 2. 多样性评分（25%）
        double diversityScore = ScoreDiversity(translatedText, allTranslations, reasons);
        totalScore += diversityScore * DiversityWeight;

        // 3. 格式完整性评分（20%）
        double formatScore = ScoreFormat(translatedText, reasons);
        totalScore += formatScore * FormatWeight;

        // 4. 平滑度评分（15%）
        double smoothnessScore = ScoreSmoothness(translatedText, reasons);
        totalScore += smoothnessScore * SmoothnessWeight;

        // 5. 拒绝分检查（10%）
        double rejectionPenalty = CalculateRejectionPenalty(translatedText, reasons);
        totalScore -= rejectionPenalty * RejectionWeight;

        // 确保分数在 0-100 范围内
        totalScore = Math.Max(0, Math.Min(100, totalScore));

        return new ScoringResult
        {
            Score = totalScore,
            Reasons = reasons,
            IsRecommended = totalScore >= 80
        };
    }

    /// <summary>
    /// 长度比评分
    /// </summary>
    private static double ScoreLength(string source, string translated, List<string> reasons)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(translated))
            return 50;

        // 检测是否为 CJK 字符
        bool isCjkSource = ContainsCjk(source);
        bool isCjkTarget = ContainsCjk(translated);

        double ratio;
        if (isCjkSource && isCjkTarget)
        {
            // CJK 到 CJK：按字符数比
            ratio = (double)translated.Length / source.Length;
        }
        else if (!isCjkSource && !isCjkTarget)
        {
            // 英文到英文：按单词数估算
            int sourceWords = source.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            int targetWords = translated.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            ratio = sourceWords > 0 ? (double)targetWords / sourceWords : 1.0;
        }
        else
        {
            // 混合翻译：按字符/单词混合估算
            ratio = (double)translated.Length / (source.Length * 1.5);
        }

        // 0.6 - 1.5 是理想范围
        double score;
        if (ratio >= 0.6 && ratio <= 1.5)
        {
            score = 100;
            reasons.Add("长度合理 ✓");
        }
        else if (ratio >= 0.4 && ratio <= 2.0)
        {
            score = 70;
            reasons.Add("长度略异常");
        }
        else
        {
            score = 40;
            reasons.Add("长度异常");
        }

        return score;
    }

    /// <summary>
    /// 多样性评分：与其他引擎结果的重叠度
    /// </summary>
    private static double ScoreDiversity(string translatedText, IReadOnlyList<string> allTranslations, List<string> reasons)
    {
        if (allTranslations.Count <= 1)
        {
            reasons.Add("无对比样本");
            return 80; // 只有一个结果，给予合理分数
        }

        // 计算与所有翻译的平均相似度
        double totalSimilarity = 0;
        int comparisonCount = 0;

        foreach (var other in allTranslations)
        {
            if (other == translatedText) continue;
            totalSimilarity += CalculateSimilarity(translatedText, other);
            comparisonCount++;
        }

        if (comparisonCount == 0)
        {
            reasons.Add("无对比样本");
            return 80;
        }

        double avgSimilarity = totalSimilarity / comparisonCount;

        // 相似度越低（越独特），分数越高
        // 相似度 0.9 以上：可能互相抄袭，分数低
        // 相似度 0.5-0.7：正常
        // 相似度 0.3 以下：可能有问题
        double score;
        if (avgSimilarity >= 0.85)
        {
            score = 60; // 太高相似，可能都不好
            reasons.Add("与其他结果高度相似");
        }
        else if (avgSimilarity >= 0.6)
        {
            score = 90; // 正常相似度
            reasons.Add("多样性良好 ✓");
        }
        else if (avgSimilarity >= 0.3)
        {
            score = 80;
            reasons.Add("有一定差异");
        }
        else
        {
            score = 65; // 太不相似，可能有问题
            reasons.Add("差异过大");
        }

        return score;
    }

    /// <summary>
    /// 格式完整性评分
    /// </summary>
    private static double ScoreFormat(string text, List<string> reasons)
    {
        double score = 100;
        var issues = new List<string>();

        // 检查首字母大写（针对英文句子）
        if (HasAbnormalCapitalization(text))
        {
            score -= 10;
            issues.Add("大小写异常");
        }

        // 检查结尾标点
        if (!HasEndingPunctuation(text))
        {
            score -= 15;
            issues.Add("缺少结尾标点");
        }

        // 检查乱码
        if (HasGarbledText(text))
        {
            score -= 30;
            issues.Add("可能存在乱码");
        }

        // 无异常问题
        if (score == 100)
        {
            reasons.Add("格式规范 ✓");
        }
        else if (issues.Count > 0)
        {
            reasons.Add(string.Join("、", issues));
        }

        return score;
    }

    /// <summary>
    /// 平滑度评分
    /// </summary>
    private static double ScoreSmoothness(string text, List<string> reasons)
    {
        double score = 100;

        // 检查连续重复字符
        if (HasConsecutiveRepeats(text))
        {
            score -= 20;
            reasons.Add("存在重复字符");
        }

        // 检查异常生僻字/符号
        if (HasAbnormalCharacters(text))
        {
            score -= 15;
            reasons.Add("存在异常字符");
        }

        // 检查机械拼接痕迹
        if (HasMechanicalPatterns(text))
        {
            score -= 10;
            reasons.Add("可能为机械拼接");
        }

        if (score == 100)
        {
            reasons.Add("表达自然 ✓");
        }

        return score;
    }

    /// <summary>
    /// 计算拒绝分（发现明显错误标记时直接扣分）
    /// </summary>
    private static double CalculateRejectionPenalty(string text, List<string> reasons)
    {
        double penalty = 0;

        // 空翻译
        if (string.IsNullOrWhiteSpace(text))
        {
            penalty += 50;
            reasons.Add("空翻译");
        }

        // 数字错误（如日期、数字明显错误）
        if (HasNumberErrors(text))
        {
            penalty += 20;
            reasons.Add("数字可能错误");
        }

        // 未翻译（与原文高度相似）
        if (IsUntranslated(text))
        {
            penalty += 30;
            reasons.Add("可能未翻译");
        }

        // 包含明显的翻译错误标记
        if (HasTranslationErrorMarkers(text))
        {
            penalty += 15;
            reasons.Add("包含错误标记");
        }

        return penalty;
    }

    /// <summary>
    /// 检测是否包含中日韩字符
    /// </summary>
    private static bool ContainsCjk(string text)
    {
        return Regex.IsMatch(text, @"[\u4e00-\u9fff\u3040-\u309f\u30a0-\u30ff\uac00-\ud7af]"); // CJK统一汉字、平假名、片假名、朝鲜文
    }

    /// <summary>
    /// 计算两个字符串的相似度（基于 n-gram 重叠）
    /// </summary>
    private static double CalculateSimilarity(string text1, string text2)
    {
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0;

        // 使用字符级 bigram 计算相似度
        var bigrams1 = GetBigrams(text1);
        var bigrams2 = GetBigrams(text2);

        if (bigrams1.Count == 0 || bigrams2.Count == 0)
            return 0;

        var intersection = bigrams1.Intersect(bigrams2).Count();
        var union = bigrams1.Union(bigrams2).Count();

        return (double)intersection / union;
    }

    /// <summary>
    /// 获取字符 bigram 集合
    /// </summary>
    private static HashSet<string> GetBigrams(string text)
    {
        var bigrams = new HashSet<string>();
        for (int i = 0; i < text.Length - 1; i++)
        {
            bigrams.Add(text.Substring(i, 2));
        }
        return bigrams;
    }

    /// <summary>
    /// 检测异常大小写
    /// </summary>
    private static bool HasAbnormalCapitalization(string text)
    {
        // 如果全是小写或全是大写（超过3个字符的单词），认为是异常
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int abnormalCount = 0;
        foreach (var word in words)
        {
            if (word.Length > 2)
            {
                if (word == word.ToLower() || word == word.ToUpper())
                    abnormalCount++;
            }
        }
        return words.Length > 3 && abnormalCount > words.Length * 0.5;
    }

    /// <summary>
    /// 检测是否有结尾标点
    /// </summary>
    private static bool HasEndingPunctuation(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var trimmed = text.TrimEnd();
        if (trimmed.Length == 0) return false;
        char lastChar = trimmed[trimmed.Length - 1];
        return char.IsPunctuation(lastChar) || char.IsWhiteSpace(lastChar);
    }

    /// <summary>
    /// 检测乱码
    /// </summary>
    private static bool HasGarbledText(string text)
    {
        // 检测大量连续无意义字符
        return Regex.IsMatch(text, @"[▀▁▂▃▄▅▆▇█]{5,}") || // 方块字符
               Regex.IsMatch(text, @"[�]{3,}"); // 替换字符
    }

    /// <summary>
    /// 检测连续重复字符
    /// </summary>
    private static bool HasConsecutiveRepeats(string text)
    {
        // 3个或以上相同字符连续出现
        return Regex.IsMatch(text, @"(.)\1{2,}");
    }

    /// <summary>
    /// 检测异常生僻字/符号
    /// </summary>
    private static bool HasAbnormalCharacters(string text)
    {
        // 检测奇怪的符号组合
        return Regex.IsMatch(text, @"[※¶§®©™®©]{2,}") ||
               Regex.IsMatch(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F]{1,}");
    }

    /// <summary>
    /// 检测机械拼接模式
    /// </summary>
    private static bool HasMechanicalPatterns(string text)
    {
        // 检测机械的重复模式
        return Regex.IsMatch(text, @"\.{3,}") || // 太多省略号
               Regex.IsMatch(text, @"(.{2,}?)\1{3,}"); // 重复短语
    }

    /// <summary>
    /// 检测数字错误
    /// </summary>
    private static bool HasNumberErrors(string text)
    {
        // 检测明显不合理的数字（如年份超过当前年份）
        var yearMatches = Regex.Matches(text, @"(19|20)\d{2}");
        int currentYear = DateTime.Now.Year;
        foreach (Match match in yearMatches)
        {
            if (int.TryParse(match.Value, out int year) && year > currentYear + 1)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检测是否未翻译
    /// </summary>
    private static bool IsUntranslated(string text)
    {
        // 如果文本与源文本高度相似，认为未翻译
        // 这里简化处理：只检查是否包含相同的连续词
        return text.Length > 10 && ContainsCjk(text) && Regex.IsMatch(text, @"[a-zA-Z]{10,}");
    }

    /// <summary>
    /// 检测翻译错误标记
    /// </summary>
    private static bool HasTranslationErrorMarkers(string text)
    {
        return text.Contains("???") ||
               text.Contains("###") ||
               text.Contains("【ERROR】") ||
               text.Contains("[ERROR]") ||
               text.Contains("翻译失败") ||
               text.Contains("translation failed");
    }
}

/// <summary>
/// 评分结果
/// </summary>
public class ScoringResult
{
    public double Score { get; set; }
    public List<string> Reasons { get; set; } = new();
    public bool IsRecommended { get; set; }
}