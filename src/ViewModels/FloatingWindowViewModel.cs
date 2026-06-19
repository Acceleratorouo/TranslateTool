using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
// OpenFileDialog
using TranslateTool.Localization;
using TranslateTool.Models;
using TranslateTool.Services;
using TranslateTool.Utils;
using TranslateTool.Views;

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
    private static readonly string HistoryFilePath = UserDataPaths.HistoryFile;

    // 剪贴板监听
    private string _lastClipboardText = "";
    private bool _isAutoTranslateEnabled = true;
    private readonly IClipboardService _clipboardService;
    private readonly IFilePickerService _filePickerService;
    private readonly IRegionSelectionService _regionSelectionService;
    private readonly ITextToSpeechService _textToSpeechService;
    private readonly INotificationService _notificationService;
    private readonly ITimerService? _timerService;

    // 划词翻译防抖
    private DateTime _lastSelectionTranslateTime = DateTime.MinValue;
    private const int SelectionTranslateDebounceMs = 500;

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

    [ObservableProperty]
    private bool _isWindowVisible;

    // 保存翻译后的原始文本（用于 Toast 显示）
    private string _translatedSourceText = "";
    private string _translatedResultText = "";
    private string _translatedEngineName = "";

    // 保存最近一次用于评分的源文本与完整源文本
    private string _lastFullSourceText = "";
    private string _lastComparisonSource = "";

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
    public IRelayCommand<string> VoteResultCommand { get; }

    public FloatingWindowViewModel()
        : this(new WpfClipboardService(), new WpfFilePickerService(), new WpfRegionSelectionService(), new WpfTextToSpeechService(), new WpfNotificationService(), new WpfTimerService())
    {
    }

    public FloatingWindowViewModel(
        IClipboardService clipboardService,
        IFilePickerService? filePickerService = null,
        IRegionSelectionService? regionSelectionService = null,
        ITextToSpeechService? textToSpeechService = null,
        INotificationService? notificationService = null,
        ITimerService? timerService = null,
        bool startClipboardMonitor = true)
    {
        _clipboardService = clipboardService;
        _filePickerService = filePickerService ?? new WpfFilePickerService();
        _regionSelectionService = regionSelectionService ?? new WpfRegionSelectionService();
        _textToSpeechService = textToSpeechService ?? new WpfTextToSpeechService();
        _notificationService = notificationService ?? new WpfNotificationService();
        _timerService = timerService;

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
        VoteResultCommand = new RelayCommand<string>(ExecuteVoteResult);

        LoadHistory();
        if (startClipboardMonitor)
        {
            StartClipboardMonitor();
        }

        _sourceLanguage = MapCodeToLanguage(Settings.SourceLanguage);
        _targetLanguage = MapCodeToLanguage(Settings.TargetLanguage);
    }

    private void StartClipboardMonitor()
    {
        if (_timerService is null)
        {
            return;
        }

        _timerService.Interval = TimeSpan.FromMilliseconds(500);
        _timerService.Tick += ClipboardTimer_Tick;
        _timerService.Start();
    }

    private void ClipboardTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isAutoTranslateEnabled || IsBusy) return;

        try
        {
            var currentText = _clipboardService.GetText();
            if (string.IsNullOrWhiteSpace(currentText)) return;
            if (currentText == _lastClipboardText) return;
            if (currentText.Length < 2 || currentText.Length > 5000) return;

            _lastClipboardText = currentText;
            _ = DoTranslate(currentText);
        }
        catch
        {
        }
    }

    public void Cleanup()
    {
        _timerService?.Stop();
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

    private static string MapCodeToLanguage(string code)
    {
        return code switch
        {
            "auto" => "自动",
            "zh-CN" => "中文",
            "en" => "英文",
            "ja" => "日文",
            "ko" => "韩文",
            "fr" => "法文",
            "de" => "德文",
            "es" => "西班牙文",
            "ru" => "俄文",
            _ => "自动"
        };
    }

    private void ExecutePasteTranslate()
    {
        var text = _clipboardService.GetText();
        if (string.IsNullOrWhiteSpace(text))
        {
            ResultText = Localization["ErrorClipboardEmpty"];
            return;
        }

        _ = DoTranslate(text);
    }

    public void SelectionTranslate()
    {
        if (!Settings.EnableSelectionTranslate) return;

        var now = DateTime.Now;
        if ((now - _lastSelectionTranslateTime).TotalMilliseconds < SelectionTranslateDebounceMs)
        {
            return;
        }
        _lastSelectionTranslateTime = now;

        if (IsTranslateToolInForeground())
        {
            return;
        }

        var previousClipboard = _clipboardService.GetText();
        string? previousClipboardData = null;
        if (!string.IsNullOrEmpty(previousClipboard))
        {
            previousClipboardData = previousClipboard;
        }

        try
        {
            System.Windows.Forms.SendKeys.SendWait("^c");
            System.Threading.Thread.Sleep(100);

            var selectedText = _clipboardService.GetText();

            if (previousClipboardData != null)
            {
                _clipboardService.SetText(previousClipboardData);
            }

            if (string.IsNullOrWhiteSpace(selectedText) || selectedText == previousClipboardData)
            {
                return;
            }

            if (selectedText.Length < 2 || selectedText.Length > 5000)
            {
                return;
            }

            _ = DoTranslate(selectedText);
        }
        catch
        {
            if (previousClipboardData != null)
            {
                try
                {
                    _clipboardService.SetText(previousClipboardData);
                }
                catch { }
            }
        }
    }

    private static bool IsTranslateToolInForeground()
    {
        try
        {
            var foregroundWindow = NativeMethods.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return false;

            var sb = new System.Text.StringBuilder(256);
            NativeMethods.GetWindowText(foregroundWindow, sb, 256);
            var windowTitle = sb.ToString();

            if (windowTitle.Contains("TranslateTool") || windowTitle.Contains("翻译工具"))
            {
                return true;
            }

            NativeMethods.GetWindowThreadProcessId(foregroundWindow, out uint foregroundPid);
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            return foregroundPid == currentProcess.Id;
        }
        catch
        {
            return false;
        }
    }

    private async Task ExecuteFileTranslate(string? _)
    {
        var filePath = _filePickerService.PickFileForTranslation();
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            await ExecuteTranslateFile(filePath);
        }
    }

    private async Task ExecuteTranslateFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            ResultText = Localization["ErrorFileNotFound"];
            return;
        }

        IsBusy = true;
        try
        {
            var text = await Task.Run(() => FileTranslationService.ExtractText(filePath));
            if (string.IsNullOrWhiteSpace(text))
            {
                ResultText = Localization["ErrorFileEmpty"];
                return;
            }

            ResultText = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Localization["TranslatingFile"],
                Path.GetFileName(filePath));
            await DoTranslate(text);
        }
        catch (Exception ex)
        {
            ResultText = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Localization["ErrorFileTranslate"],
                ex.Message);
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
            var selectedRegion = _regionSelectionService.SelectRegion();
            if (selectedRegion == null)
            {
                IsBusy = false;
                return;
            }

            var region = selectedRegion.Value;
            var progress = new Progress<string>(msg => ResultText = msg);

            if (!OcrService.IsReady)
            {
                ResultText = Localization["PreparingOcr"];
                await OcrService.InitializeAsync(progress);
            }

            var rect = new System.Drawing.Rectangle(
                (int)region.X,
                (int)region.Y,
                (int)region.Width,
                (int)region.Height);

            using var bitmap = ScreenCaptureService.CaptureRectangle(rect);
            ResultText = Localization["RecognizingText"];
            var text = await OcrService.RecognizeTextAsync(bitmap, progress: progress);
            await DoTranslate(text);
        }
        catch (Exception ex)
        {
            ResultText = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Localization["ErrorRegion"],
                ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
    private async Task DoTranslate(string text)
    {
        _lastFullSourceText = text;
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
                    try { _clipboardService.SetText(cachedText!); } catch { }
                }

                AddToHistory(text, cachedText!, Settings.TranslationEngine + " (缓存)");

                // 保存 Toast 显示用的值
                _translatedSourceText = sourcePreview;
                _translatedResultText = cachedText!;
                _translatedEngineName = Settings.TranslationEngine;

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
                        _clipboardService.SetText(result.TranslatedText);
                    }
                    catch { }
                }

                AddToHistory(text, result.TranslatedText, result.Engine);

                // 保存 Toast 显示用的值
                _translatedSourceText = sourcePreview;
                _translatedResultText = result.TranslatedText;
                _translatedEngineName = result.Engine;
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

            // 翻译完成后，若悬浮窗未显示且开启了 Toast 提示，则显示 Toast
            if (!IsWindowVisible && Settings.ShowToastOnTranslate && !string.IsNullOrEmpty(_translatedResultText))
            {
                _notificationService.ShowTranslation(_translatedSourceText, _translatedResultText, _translatedEngineName);
            }
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
        _ = LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            UserDataPaths.EnsureDirectoryExists(UserDataPaths.HistoryDirectory);
            if (File.Exists(HistoryFilePath))
            {
                var json = await File.ReadAllTextAsync(HistoryFilePath);
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
        _ = SaveHistoryAsync();
    }

    private async Task SaveHistoryAsync()
    {
        try
        {
            UserDataPaths.EnsureDirectoryExists(UserDataPaths.HistoryDirectory);
            var json = JsonSerializer.Serialize(History.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(HistoryFilePath, json);
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
            _clipboardService.SetText(textToCopy);
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
    /// 使用文本转语音服务朗读文本。
    /// </summary>
    private void SpeakText(string text)
    {
        _textToSpeechService.Speak(text);
    }

    /// <summary>
    /// 记录用户对某个引擎结果的赞/踩反馈
    /// </summary>
    private void ExecuteVoteResult(string? parameter)
    {
        if (string.IsNullOrWhiteSpace(parameter))
        {
            return;
        }

        var parts = parameter.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out int vote))
        {
            return;
        }

        var engine = parts[0];
        var result = ComparisonResults.FirstOrDefault(r =>
            r.EngineName.Equals(engine, StringComparison.OrdinalIgnoreCase));
        if (result is null)
        {
            return;
        }

        var source = !string.IsNullOrEmpty(_lastComparisonSource)
            ? _lastComparisonSource
            : SourceText;

        var entry = new TranslationFeedbackEntry
        {
            SourceHash = TranslationFeedbackService.ComputeSourceHash(source),
            Engine = result.EngineName,
            Translation = result.TranslatedText,
            Vote = vote,
            Timestamp = DateTime.UtcNow
        };

        TranslationFeedbackService.SaveFeedback(entry);

        // 反馈可能改变引擎声誉，重新计算所有对比结果的分数
        if (!string.IsNullOrEmpty(source))
        {
            var allTranslations = ComparisonResults
                .Where(r => r.IsSuccess)
                .Select(r => r.TranslatedText)
                .ToList();

            foreach (var item in ComparisonResults)
            {
                if (item.IsSuccess)
                {
                    var scoringResult = TranslationScoringService.ScoreResult(
                        source,
                        item.TranslatedText,
                        allTranslations,
                        Settings.ScoreWeightLength,
                        Settings.ScoreWeightDiversity,
                        Settings.ScoreWeightFormat,
                        Settings.ScoreWeightSmoothness,
                        Settings.ScoreWeightRejection,
                        item.EngineName);
                    item.Score = scoringResult.Score;
                    item.ScoreReason = string.Join("；", scoringResult.Reasons);
                }
            }
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

            // 将源文本传递给评分服务（用于获取原文预览）
            var sourceForScoring = !string.IsNullOrEmpty(_lastFullSourceText)
                ? _lastFullSourceText
                : SourceText;
            _lastComparisonSource = sourceForScoring;

            // 计算所有翻译文本用于多样性评分
            var allTranslations = results
                .Where(r => r.IsSuccess)
                .Select(r => r.TranslatedText)
                .ToList();

            // 为每个结果计算评分
            foreach (var result in results)
            {
                if (result.IsSuccess)
                {
                    var scoringResult = TranslationScoringService.ScoreResult(
                        sourceForScoring,
                        result.TranslatedText,
                        allTranslations,
                        Settings.ScoreWeightLength,
                        Settings.ScoreWeightDiversity,
                        Settings.ScoreWeightFormat,
                        Settings.ScoreWeightSmoothness,
                        Settings.ScoreWeightRejection,
                        result.EngineName);
                    result.Score = scoringResult.Score;
                    result.ScoreReason = string.Join("；", scoringResult.Reasons);
                }
                else
                {
                    result.Score = 0;
                    result.ScoreReason = "翻译失败";
                }
            }

            // 按评分降序排列
            var sortedResults = results.OrderByDescending(r => r.Score).ToList();

            foreach (var result in sortedResults)
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

    // 评分相关属性
    public double Score { get; set; }
    public bool IsRecommended => Score >= 80;
    public string ScoreReason { get; set; } = "";
}

