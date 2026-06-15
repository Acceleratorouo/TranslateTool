using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
// OpenFileDialog
using TranslateTool.Localization;
using TranslateTool.Models;
using TranslateTool.Services;

namespace TranslateTool.ViewModels;

/// <summary>
/// 翻译历史条目
/// </summary>
public class HistoryEntry
{
    public DateTime Timestamp { get; set; }
    public string SourceText { get; set; } = "";
    public string TranslatedText { get; set; } = "";
    public string SourceLang { get; set; } = "";
    public string TargetLang { get; set; } = "";
    public string Engine { get; set; } = "";

    public string DisplayText => $"[{Timestamp:HH:mm:ss}] {SourceText[..Math.Min(30, SourceText.Length)]}...";
}

public partial class FloatingWindowViewModel : ObservableObject
{
    private const string IdleMessage = "复制文本自动翻译";
    private static readonly string HistoryFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "translation_history.json");

    // 剪贴板监听
    private DispatcherTimer? _clipboardTimer;
    private string _lastClipboardText = "";
    private bool _isAutoTranslateEnabled = true;

    [ObservableProperty]
    private string _resultText = IdleMessage;

    [ObservableProperty]
    private string _sourceText = "";

    [ObservableProperty]
    private string _translatedText = "";

    [ObservableProperty]
    private string _engineInfo = "";

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _sourceLanguage = "自动";

    [ObservableProperty]
    private string _targetLanguage = "中文";

    [ObservableProperty]
    private bool _showSourcePicker;

    [ObservableProperty]
    private bool _showTargetPicker;

    [ObservableProperty]
    private bool _showComparison;

    public AppSettings Settings { get; } = AppSettings.Current;

    /// <summary>
    /// 可用语言列表
    /// </summary>
    public ObservableCollection<string> Languages { get; } = new()
    {
        "自动", "中文", "英文", "日文", "韩文", "法文", "德文", "西班牙文", "俄文"
    };

    /// <summary>
    /// 本地化管理器
    /// </summary>
    public LocalizationManager Localization { get; } = LocalizationManager.Instance;

    /// <summary>
    /// 翻译历史
    /// </summary>
    public ObservableCollection<HistoryEntry> History { get; } = new();

    /// <summary>
    /// 多引擎对比结果
    /// </summary>
    public ObservableCollection<EngineComparisonResult> ComparisonResults { get; } = new();

    public ICommand PasteTranslateCommand { get; }
    public IAsyncRelayCommand<string> FileTranslateCommand { get; }
    public IAsyncRelayCommand<string> TranslateFileCommand { get; }
    public IAsyncRelayCommand RegionTranslateCommand { get; }
    public ICommand CopyResultCommand { get; }
    public ICommand SwitchSourceLangCommand { get; }
    public ICommand SwitchTargetLangCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand ShowHistoryCommand { get; }
    public IRelayCommand SpeakTranslatedCommand { get; }
    public IRelayCommand SpeakSourceCommand { get; }
    public IAsyncRelayCommand CompareEnginesCommand { get; }
    public IRelayCommand ToggleComparisonCommand { get; }
    public IRelayCommand SwapLanguagesCommand { get; }
    public IRelayCommand<string> SwitchLanguageCommand { get; }

    public FloatingWindowViewModel()
    {
        PasteTranslateCommand = new RelayCommand(ExecutePasteTranslate);
        FileTranslateCommand = new AsyncRelayCommand<string>(ExecuteFileTranslate);
        TranslateFileCommand = new AsyncRelayCommand<string>(ExecuteTranslateFile);
        RegionTranslateCommand = new AsyncRelayCommand(ExecuteRegionTranslate);
        CopyResultCommand = new RelayCommand(ExecuteCopyResult);
        SwitchSourceLangCommand = new RelayCommand(ExecuteSwitchSourceLang);
        SwitchTargetLangCommand = new RelayCommand(ExecuteSwitchTargetLang);
        ClearHistoryCommand = new RelayCommand(ExecuteClearHistory);
        ShowHistoryCommand = new RelayCommand(ExecuteShowHistory);
        SpeakTranslatedCommand = new RelayCommand(ExecuteSpeakTranslated);
        SpeakSourceCommand = new RelayCommand(ExecuteSpeakSource);
        CompareEnginesCommand = new AsyncRelayCommand(ExecuteCompareEngines);
        ToggleComparisonCommand = new RelayCommand(() => ShowComparison = !ShowComparison);
        SwapLanguagesCommand = new RelayCommand(ExecuteSwapLanguages);
        SwitchLanguageCommand = new RelayCommand<string>(ExecuteSwitchLanguage);

        LoadHistory();
        StartClipboardMonitor();
    }

    /// <summary>
    /// 启动剪贴板监听，复制文本自动翻译
    /// </summary>
    private void StartClipboardMonitor()
    {
        _clipboardTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _clipboardTimer.Tick += ClipboardTimer_Tick;
        _clipboardTimer.Start();
    }

    private void ClipboardTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isAutoTranslateEnabled || IsBusy) return;

        try
        {
            var currentText = ClipboardHelper.GetClipboardText();
            if (string.IsNullOrWhiteSpace(currentText)) return;
            if (currentText == _lastClipboardText) return;

            // 过滤掉过短的文本（避免误触）和过长的文本
            if (currentText.Length < 2 || currentText.Length > 5000) return;

            _lastClipboardText = currentText;
            _ = DoTranslate(currentText);
        }
        catch
        {
            // 剪贴板可能被其他程序锁定，忽略
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Cleanup()
    {
        _clipboardTimer?.Stop();
        _clipboardTimer = null;
    }

    partial void OnSourceLanguageChanged(string value)
    {
        Settings.SourceLanguage = MapLanguageToCode(value);
    }

    partial void OnTargetLanguageChanged(string value)
    {
        Settings.TargetLanguage = MapLanguageToCode(value);
    }

    private void ExecuteSwitchSourceLang()
    {
        ShowSourcePicker = !ShowSourcePicker;
        ShowTargetPicker = false;
    }

    private void ExecuteSwitchTargetLang()
    {
        ShowTargetPicker = !ShowTargetPicker;
        ShowSourcePicker = false;
    }

    private void ExecuteSwapLanguages()
    {
        var tempLang = SourceLanguage;
        SourceLanguage = TargetLanguage;
        TargetLanguage = tempLang;
    }

    private void ExecuteSwitchLanguage(string? languageCode)
    {
        if (string.IsNullOrEmpty(languageCode)) return;

        Localization.SwitchLanguage(languageCode);
        Settings.UILanguage = languageCode;
        Settings.Save();
    }

    private static string MapLanguageToCode(string lang)
    {
        return lang switch
        {
            "自动" => "auto",
            "中文" => "zh-CN",
            "英文" => "en",
            "日文" => "ja",
            "韩文" => "ko",
            "法文" => "fr",
            "德文" => "de",
            "西班牙文" => "es",
            "俄文" => "ru",
            _ => "auto"
        };
    }

    private void ExecutePasteTranslate()
    {
        var text = ClipboardHelper.GetClipboardText();
        if (string.IsNullOrWhiteSpace(text))
        {
            ResultText = "剪贴板为空";
            return;
        }

        _ = DoTranslate(text);
    }

    private async Task ExecuteFileTranslate(string? _)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "支持的文件|*.txt;*.docx;*.pdf|文本文件|*.txt|Word 文档|*.docx|PDF 文件|*.pdf",
            Title = "选择要翻译的文件"
        };

        if (dialog.ShowDialog() == true)
        {
            await ExecuteTranslateFile(dialog.FileName);
        }
    }

    private async Task ExecuteTranslateFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            ResultText = "文件不存在或路径无效";
            return;
        }

        IsBusy = true;
        try
        {
            var text = await Task.Run(() => FileTranslationService.ExtractText(filePath));
            if (string.IsNullOrWhiteSpace(text))
            {
                ResultText = "文件内容为空或无法提取文本";
                return;
            }

            ResultText = $"正在翻译文件: {Path.GetFileName(filePath)}...";
            await DoTranslate(text);
        }
        catch (Exception ex)
        {
            ResultText = $"文件翻译出错: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteRegionTranslate()
    {
        IsBusy = true;
        try
        {
            // 显示区域选择遮罩
            var overlay = new Views.RegionSelectorOverlay();
            overlay.ShowDialog();

            if (!overlay.IsCompleted || overlay.SelectedRegion == null)
            {
                IsBusy = false;
                return;
            }

            var region = overlay.SelectedRegion.Value;

            // 显示 OCR 下载进度
            var progress = new Progress<string>(msg => ResultText = msg);
            
            // 确保 OCR 就绪（自动下载语言包）
            if (!OcrService.IsReady)
            {
                ResultText = "正在准备 OCR 引擎...";
                await OcrService.InitializeAsync(progress);
            }

            // 截取选中区域
            var rect = new System.Drawing.Rectangle(
                (int)region.X,
                (int)region.Y,
                (int)region.Width,
                (int)region.Height);

            using var bitmap = ScreenCaptureService.CaptureRectangle(rect);
            ResultText = "正在识别文字...";
            var text = await OcrService.RecognizeTextAsync(bitmap, progress: progress);
            await DoTranslate(text);
        }
        catch (Exception ex)
        {
            ResultText = $"框选翻译出错: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DoTranslate(string text)
    {
        IsBusy = true;
        try
        {
            // 检查缓存
            if (TranslationCache.TryGet(text, Settings.SourceLanguage, Settings.TargetLanguage, Settings.TranslationEngine, out var cachedText))
            {
                var sourcePreview = text.Length > 100 ? text[..100] + "..." : text;
                SourceText = sourcePreview;
                TranslatedText = cachedText!;
                EngineInfo = Settings.TranslationEngine + " (缓存)";
                HasResult = true;

                // 自动复制译文到剪贴板
                if (Settings.AutoCopyTranslation)
                {
                    try { ClipboardHelper.SetClipboardText(cachedText!); } catch { }
                }

                AddToHistory(text, cachedText!, Settings.TranslationEngine + " (缓存)");
                IsBusy = false;
                return;
            }

            var engine = TranslatorFactory.Create(Settings.TranslationEngine);
            var result = await engine.TranslateAsync(
                text,
                Settings.SourceLanguage,
                Settings.TargetLanguage);

            if (result.IsSuccess)
            {
                var sourcePreview = text.Length > 100 ? text[..100] + "..." : text;
                
                // 分离原文和译文用于富文本显示
                SourceText = sourcePreview;
                TranslatedText = result.TranslatedText;
                EngineInfo = result.Engine;
                HasResult = true;

                // 添加到缓存
                TranslationCache.Add(text, Settings.SourceLanguage, Settings.TargetLanguage, Settings.TranslationEngine, result.TranslatedText);

                // 自动复制译文到剪贴板
                if (Settings.AutoCopyTranslation && !string.IsNullOrEmpty(result.TranslatedText))
                {
                    try
                    {
                        ClipboardHelper.SetClipboardText(result.TranslatedText);
                    }
                    catch { }
                }

                AddToHistory(text, result.TranslatedText, result.Engine);
            }
            else
            {
                ResultText = FormatError(result.ErrorMessage, Settings.TranslationEngine);
                SourceText = "";
                TranslatedText = "";
                EngineInfo = "";
                HasResult = false;
            }
        }
        catch (NotSupportedException ex)
        {
            ResultText = $"引擎错误: {ex.Message}";
        }
        catch (Exception ex)
        {
            ResultText = FormatError(ex.Message, Settings.TranslationEngine);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AddToHistory(string source, string translated, string engine)
    {
        var entry = new HistoryEntry
        {
            Timestamp = DateTime.Now,
            SourceText = source,
            TranslatedText = translated,
            SourceLang = SourceLanguage,
            TargetLang = TargetLanguage,
            Engine = engine
        };

        History.Insert(0, entry);

        while (History.Count > 100)
            History.RemoveAt(History.Count - 1);

        SaveHistory();
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(HistoryFilePath))
            {
                var json = File.ReadAllText(HistoryFilePath);
                var entries = JsonSerializer.Deserialize<List<HistoryEntry>>(json);
                if (entries != null)
                {
                    foreach (var entry in entries.Take(100))
                        History.Add(entry);
                }
            }
        }
        catch { }
    }

    private void SaveHistory()
    {
        try
        {
            var json = JsonSerializer.Serialize(History.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(HistoryFilePath, json);
        }
        catch { }
    }

    private void ExecuteClearHistory()
    {
        History.Clear();
        SaveHistory();
    }

    private void ExecuteShowHistory()
    {
        var window = new Views.HistoryWindow
        {
            DataContext = this
        };
        window.Show();
    }

    private static string FormatError(string? errorMessage, string engine)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return "翻译失败: 未知错误";

        if (errorMessage.Contains("429") || errorMessage.Contains("TooManyRequests") || errorMessage.Contains("受限"))
            return $"⚠️ {engine} 请求过于频繁，请稍后再试或切换其他引擎";

        if (errorMessage.Contains("403") || errorMessage.Contains("Forbidden"))
            return $"⚠️ {engine} 访问被拒绝，建议切换到百度翻译引擎";

        if (errorMessage.Contains("timeout", StringComparison.OrdinalIgnoreCase) || errorMessage.Contains("超时"))
            return "⚠️ 网络超时，请检查网络连接后重试";

        if (errorMessage.Contains("SSL") || errorMessage.Contains("AuthenticationException"))
            return "⚠️ 网络连接安全错误，请尝试其他引擎";

        if (errorMessage.Contains("格式异常") || errorMessage.Contains("非JSON"))
            return $"⚠️ {engine} 接口返回异常，建议切换到百度翻译引擎";

        return $"翻译失败: {errorMessage}";
    }

    private void ExecuteCopyResult()
    {
        if (!string.IsNullOrWhiteSpace(ResultText) && ResultText != IdleMessage)
        {
            var textToCopy = ResultText;
            var separator = "\n\n【";
            var separatorIndex = ResultText.IndexOf(separator, StringComparison.Ordinal);
            if (separatorIndex > 0 && ResultText.StartsWith("【原文】"))
            {
                var marker = "】";
                var transStart = ResultText.IndexOf(marker, separatorIndex + separator.Length, StringComparison.Ordinal);
                if (transStart > 0)
                {
                    textToCopy = ResultText[(transStart + marker.Length)..];
                }
            }
            ClipboardHelper.SetClipboardText(textToCopy);
        }
    }

    /// <summary>
    /// 朗读译文
    /// </summary>
    private void ExecuteSpeakTranslated()
    {
        if (!string.IsNullOrEmpty(TranslatedText))
        {
            SpeakText(TranslatedText);
        }
    }

    /// <summary>
    /// 朗读原文
    /// </summary>
    private void ExecuteSpeakSource()
    {
        if (!string.IsNullOrEmpty(SourceText))
        {
            SpeakText(SourceText);
        }
    }

    /// <summary>
    /// 使用 Windows SAPI 朗读文本
    /// </summary>
    private void SpeakText(string text)
    {
        try
        {
            // 异步朗读，不阻塞 UI
            Task.Run(() =>
            {
                try
                {
                    using var synthesizer = new System.Speech.Synthesis.SpeechSynthesizer();
                    synthesizer.SetOutputToDefaultAudioDevice();
                    synthesizer.Speak(text); // 使用同步方法确保音频播放完成
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Speech failed: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Speech initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 执行多引擎对比翻译
    /// </summary>
    private async Task ExecuteCompareEngines()
    {
        if (string.IsNullOrEmpty(SourceText) || !HasResult)
        {
            ResultText = "请先翻译一段文本";
            return;
        }

        IsBusy = true;
        ComparisonResults.Clear();
        ShowComparison = true;

        try
        {
            var engines = new[] { "baidu", "google", "microsoft", "deepl" };
            var tasks = engines.Select(async engineName =>
            {
                try
                {
                    var engine = TranslatorFactory.Create(engineName);
                    var result = await engine.TranslateAsync(
                        SourceText,
                        Settings.SourceLanguage,
                        Settings.TargetLanguage);

                    return new EngineComparisonResult
                    {
                        EngineName = engine.Name,
                        TranslatedText = result.IsSuccess ? result.TranslatedText : $"翻译失败: {result.ErrorMessage}",
                        IsSuccess = result.IsSuccess
                    };
                }
                catch (Exception ex)
                {
                    return new EngineComparisonResult
                    {
                        EngineName = engineName,
                        TranslatedText = $"错误: {ex.Message}",
                        IsSuccess = false
                    };
                }
            });

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                ComparisonResults.Add(result);
            }
        }
        catch (Exception ex)
        {
            ResultText = $"对比翻译出错: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// 引擎对比结果
/// </summary>
public class EngineComparisonResult
{
    public string EngineName { get; set; } = "";
    public string TranslatedText { get; set; } = "";
    public bool IsSuccess { get; set; }
}

