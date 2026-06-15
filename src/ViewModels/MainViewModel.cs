using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateTool.Models;
using TranslateTool.Services;

namespace TranslateTool.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    [ObservableProperty] private string _sourceText = "";
    [ObservableProperty] private string _translatedText = "";
    [ObservableProperty] private bool _isTranslating = false;
    [ObservableProperty] private string _sourceLanguage = "auto";
    [ObservableProperty] private string _targetLanguage = "zh-CN";

    public ObservableCollection<string> Languages { get; } = new()
    {
        "auto", "zh-CN", "en-US", "ja-JP", "ko-KR",
        "fr-FR", "de-DE", "es-ES", "ru-RU", "pt-BR"
    };

    public ICommand TranslateCommand { get; }
    public ICommand CopyResultCommand { get; }

    public MainViewModel()
    {
        TranslateCommand = new RelayCommand(ExecuteTranslate, CanExecuteTranslate);
        CopyResultCommand = new RelayCommand(ExecuteCopyResult);
    }

    private bool CanExecuteTranslate() => !string.IsNullOrWhiteSpace(SourceText);

    private async void ExecuteTranslate()
    {
        IsTranslating = true;
        try
        {
            var engine = AppSettings.Current.TranslationEngine;
            var translator = TranslatorFactory.Create(engine);

            var result = await translator.TranslateAsync(
                SourceText, SourceLanguage, TargetLanguage);

            TranslatedText = result.IsSuccess
                ? result.TranslatedText
                : $"翻译失败: {result.ErrorMessage}";
        }
        finally
        {
            IsTranslating = false;
        }
    }

    private void ExecuteCopyResult()
    {
        if (!string.IsNullOrWhiteSpace(TranslatedText))
            ClipboardHelper.SetClipboardText(TranslatedText);
    }
}
