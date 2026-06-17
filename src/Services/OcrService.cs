using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Tesseract;
using TranslateTool.Utils;

namespace TranslateTool.Services;

/// <summary>
/// OCR 服务 — 使用 Tesseract 引擎识别图片中的文字（支持自动下载语言包）
/// </summary>
public static class OcrService
{
    private static readonly string TessDataPath = UserDataPaths.TessDataDirectory;

    private static bool _initialized;
    private static bool _downloading;
    private static string? _initError;

    // Tesseract 语言包下载地址（GitHub mirror）
    private static readonly Dictionary<string, string> LanguagePackUrls = new()
    {
        { "chi_sim.traineddata", "https://github.com/tesseract-ocr/tessdata/raw/main/chi_sim.traineddata" },
        { "eng.traineddata", "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata" }
    };

    /// <summary>
    /// 初始化状态信息
    /// </summary>
    public static string? InitError => _initError;
    public static bool IsReady => _initialized && string.IsNullOrEmpty(_initError);
    public static bool IsDownloading => _downloading;

    /// <summary>
    /// 检查并初始化 Tesseract 引擎（自动下载缺失的语言包）
    /// </summary>
    public static async Task InitializeAsync(IProgress<string>? progress = null)
    {
        if (_initialized) return;

        try
        {
            // 确保 tessdata 目录存在
            if (!Directory.Exists(TessDataPath))
            {
                Directory.CreateDirectory(TessDataPath);
            }

            // 检查缺失的语言包
            var missingPacks = new List<string>();
            foreach (var (fileName, url) in LanguagePackUrls)
            {
                var filePath = Path.Combine(TessDataPath, fileName);
                if (!File.Exists(filePath))
                {
                    missingPacks.Add(fileName);
                }
            }

            // 自动下载缺失的语言包
            if (missingPacks.Count > 0)
            {
                _downloading = true;
                progress?.Report($"正在下载 OCR 语言包 ({missingPacks.Count} 个)...");

                foreach (var fileName in missingPacks)
                {
                    var filePath = Path.Combine(TessDataPath, fileName);
                    var url = LanguagePackUrls[fileName];

                    progress?.Report($"正在下载 {fileName}...");
                    await DownloadFileAsync(url, filePath);
                    progress?.Report($"{fileName} 下载完成");
                }

                _downloading = false;
            }

            // 验证语言包
            var hasChinese = File.Exists(Path.Combine(TessDataPath, "chi_sim.traineddata"));
            var hasEnglish = File.Exists(Path.Combine(TessDataPath, "eng.traineddata"));

            if (!hasChinese && !hasEnglish)
            {
                _initError = "语言包下载失败，请手动下载。";
                return;
            }

            _initialized = true;
            _initError = null;
            progress?.Report("OCR 引擎就绪");
        }
        catch (Exception ex)
        {
            _downloading = false;
            _initError = $"初始化 OCR 引擎失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    private static async Task DownloadFileAsync(string url, string filePath)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    /// <summary>
    /// 从 Bitmap 图片中识别文字
    /// </summary>
    public static async Task<string> RecognizeTextAsync(Bitmap image, string? language = null, IProgress<string>? progress = null)
    {
        if (!_initialized)
            await InitializeAsync(progress);

        if (!string.IsNullOrEmpty(_initError))
            return $"[OCR 未就绪] {_initError}";

        return await Task.Run(() =>
        {
            try
            {
                var lang = language ?? GetBestLanguage();

                using var engine = new TesseractEngine(TessDataPath, lang, EngineMode.Default);
                using var ms = new MemoryStream();
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;

                using var pix = Pix.LoadFromMemory(ms.ToArray());
                using var page = engine.Process(pix);

                var text = page.GetText();

                if (string.IsNullOrWhiteSpace(text))
                    return "[未识别到文字]";

                return text.Trim();
            }
            catch (Exception ex)
            {
                return $"[OCR 识别失败] {ex.Message}";
            }
        });
    }

    /// <summary>
    /// 获取最佳可用语言
    /// </summary>
    private static string GetBestLanguage()
    {
        var hasChinese = File.Exists(Path.Combine(TessDataPath, "chi_sim.traineddata"));
        var hasEnglish = File.Exists(Path.Combine(TessDataPath, "eng.traineddata"));

        if (hasChinese && hasEnglish)
            return "chi_sim+eng";
        if (hasChinese)
            return "chi_sim";
        return "eng";
    }
}
