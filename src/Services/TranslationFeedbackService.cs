using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranslateTool.Utils;

namespace TranslateTool.Services;

/// <summary>
/// 用户反馈条目
/// </summary>
public class TranslationFeedbackEntry
{
    [JsonPropertyName("sourceHash")]
    public string SourceHash { get; set; } = "";

    [JsonPropertyName("engine")]
    public string Engine { get; set; } = "";

    [JsonPropertyName("translation")]
    public string Translation { get; set; } = "";

    [JsonPropertyName("vote")]
    public int Vote { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 反馈数据容器
/// </summary>
public class TranslationFeedbackStore
{
    [JsonPropertyName("entries")]
    public List<TranslationFeedbackEntry> Entries { get; set; } = new();
}

/// <summary>
/// 多引擎翻译用户反馈服务
/// </summary>
public static class TranslationFeedbackService
{
    private static readonly object _fileLock = new();

    /// <summary>
    /// 反馈文件目录：%LOCALAPPDATA%\TranslateTool\Feedback\
    /// </summary>
    public static string FeedbackDirectory => Path.Combine(
        UserDataPaths.LocalAppDataDirectory,
        "Feedback");

    /// <summary>
    /// 反馈文件路径：%LOCALAPPDATA%\TranslateTool\Feedback\translation_feedback.json
    /// </summary>
    public static string FeedbackFile => Path.Combine(
        FeedbackDirectory,
        "translation_feedback.json");

    /// <summary>
    /// 引擎声誉分数（引擎名 -> 历史投票净和）
    /// </summary>
    public static IReadOnlyDictionary<string, int> EngineReputation => _engineReputation;

    private static readonly Dictionary<string, int> _engineReputation = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// 加载历史反馈并计算引擎声誉
    /// </summary>
    public static void Load()
    {
        lock (_fileLock)
        {
            _engineReputation.Clear();

            try
            {
                UserDataPaths.EnsureDirectoryExists(FeedbackDirectory);
                if (!File.Exists(FeedbackFile))
                {
                    return;
                }

                var json = File.ReadAllText(FeedbackFile);
                var store = JsonSerializer.Deserialize<TranslationFeedbackStore>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                if (store?.Entries is null)
                {
                    return;
                }

                RebuildReputation(store.Entries);
            }
            catch
            {
                // 加载失败静默忽略， reputations 保持为空
            }
        }
    }

    /// <summary>
    /// 记录一次赞/踩反馈
    /// </summary>
    public static void SaveFeedback(TranslationFeedbackEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Engine))
        {
            return;
        }

        lock (_fileLock)
        {
            try
            {
                UserDataPaths.EnsureDirectoryExists(FeedbackDirectory);

                var store = new TranslationFeedbackStore();
                if (File.Exists(FeedbackFile))
                {
                    try
                    {
                        var json = File.ReadAllText(FeedbackFile);
                        var existing = JsonSerializer.Deserialize<TranslationFeedbackStore>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true
                        });
                        if (existing?.Entries is not null)
                        {
                            store.Entries.AddRange(existing.Entries);
                        }
                    }
                    catch { }
                }

                store.Entries.Add(entry);

                var newJson = JsonSerializer.Serialize(store, _jsonOptions);
                File.WriteAllText(FeedbackFile, newJson);

                RebuildReputation(store.Entries);
            }
            catch
            {
                // 保存失败静默忽略
            }
        }
    }

    /// <summary>
    /// 计算源文本的 SHA256 哈希
    /// </summary>
    public static string ComputeSourceHash(string sourceText)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(sourceText);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static void RebuildReputation(List<TranslationFeedbackEntry> entries)
    {
        _engineReputation.Clear();

        foreach (var group in entries.GroupBy(e => e.Engine.ToLowerInvariant()))
        {
            var net = group.Sum(e => e.Vote);
            _engineReputation[group.Key] = net;
        }
    }
}
