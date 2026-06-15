using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace TranslateTool.Services;

/// <summary>
/// 翻译缓存服务 — 避免相同文本重复请求翻译 API
/// </summary>
public static class TranslationCache
{
    private static readonly string CacheFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "translation_cache.json");

    private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromDays(7); // 缓存 7 天
    private static bool _loaded;

    /// <summary>
    /// 缓存条目
    /// </summary>
    private class CacheEntry
    {
        public string TranslatedText { get; set; } = "";
        public string Engine { get; set; } = "";
        public DateTime CachedAt { get; set; }
        public bool IsExpired => DateTime.Now - CachedAt > CacheExpiry;
    }

    /// <summary>
    /// 加载缓存
    /// </summary>
    public static void Load()
    {
        if (_loaded) return;

        try
        {
            if (File.Exists(CacheFilePath))
            {
                var json = File.ReadAllText(CacheFilePath);
                var entries = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(json);
                if (entries != null)
                {
                    foreach (var (key, entry) in entries)
                    {
                        if (!entry.IsExpired)
                        {
                            _cache[key] = entry;
                        }
                    }
                }
            }
        }
        catch { }

        _loaded = true;
    }

    /// <summary>
    /// 保存缓存到文件
    /// </summary>
    public static void Save()
    {
        try
        {
            // 清除过期条目
            var expiredKeys = _cache.Where(x => x.Value.IsExpired).Select(x => x.Key).ToList();
            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }

            var json = JsonSerializer.Serialize(_cache.ToDictionary(x => x.Key, x => x.Value), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(CacheFilePath, json);
        }
        catch { }
    }

    /// <summary>
    /// 生成缓存键
    /// </summary>
    private static string GenerateKey(string text, string sourceLang, string targetLang, string engine)
    {
        return $"{engine}|{sourceLang}|{targetLang}|{text}";
    }

    /// <summary>
    /// 尝试从缓存获取翻译结果
    /// </summary>
    public static bool TryGet(string text, string sourceLang, string targetLang, string engine, out string? translatedText)
    {
        translatedText = null;

        if (!_loaded) Load();

        var key = GenerateKey(text, sourceLang, targetLang, engine);
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            translatedText = entry.TranslatedText;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 添加翻译结果到缓存
    /// </summary>
    public static void Add(string text, string sourceLang, string targetLang, string engine, string translatedText)
    {
        if (!_loaded) Load();

        var key = GenerateKey(text, sourceLang, targetLang, engine);
        _cache[key] = new CacheEntry
        {
            TranslatedText = translatedText,
            Engine = engine,
            CachedAt = DateTime.Now
        };

        // 限制缓存大小（最多 1000 条）
        if (_cache.Count > 1000)
        {
            var oldest = _cache.OrderBy(x => x.Value.CachedAt).Take(100).Select(x => x.Key).ToList();
            foreach (var k in oldest)
            {
                _cache.TryRemove(k, out _);
            }
        }
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public static void Clear()
    {
        _cache.Clear();
        Save();
    }

    /// <summary>
    /// 获取缓存统计
    /// </summary>
    public static (int count, int expired) GetStats()
    {
        var expired = _cache.Count(x => x.Value.IsExpired);
        return (_cache.Count, expired);
    }
}
