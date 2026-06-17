using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TranslateTool.Views;

public partial class TranslationToastWindow : Window
{
    private static TranslationToastWindow? _currentToast;
    private static readonly object _lock = new();
    private DispatcherTimer? _autoCloseTimer;
    private bool _isClosing;

    public TranslationToastWindow()
    {
        InitializeComponent();
        PositionWindow();
    }

    private void PositionWindow()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 20;
        Top = workArea.Bottom - ActualHeight - 20;
    }

    /// <summary>
    /// 显示翻译结果 Toast
    /// </summary>
    /// <param name="sourceText">原文</param>
    /// <param name="translatedText">译文</param>
    /// <param name="engineName">翻译引擎名称</param>
    public static void Show(string sourceText, string translatedText, string engineName)
    {
        lock (_lock)
        {
            // 若已有 Toast 在显示，先关闭旧的
            if (_currentToast != null && _currentToast.IsLoaded)
            {
                _currentToast.ForceClose();
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _currentToast = new TranslationToastWindow();
                _currentToast.SourceTextBlock.Text = sourceText;
                _currentToast.TranslatedTextBlock.Text = translatedText;
                _currentToast.EngineText.Text = engineName;
                _currentToast.Show();

                // 确保窗口位置正确（因为 ActualWidth/Height 在 Show 后才可用）
                _currentToast.PositionWindow();

                // 启动自动关闭计时器（4秒后淡出）
                _currentToast._autoCloseTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(4)
                };
                _currentToast._autoCloseTimer.Tick += (s, e) =>
                {
                    _currentToast._autoCloseTimer?.Stop();
                    _currentToast.StartFadeOut();
                };
                _currentToast._autoCloseTimer.Start();
            });
        }
    }

    private void ForceClose()
    {
        _isClosing = true;
        _autoCloseTimer?.Stop();
        Close();
    }

    private void StartFadeOut()
    {
        if (_isClosing) return;

        var storyboard = (Storyboard)FindResource("FadeOutStoryboard");
        storyboard?.Begin(this);
    }

    private void FadeOutStoryboard_Completed(object? sender, EventArgs e)
    {
        ForceClose();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ForceClose();
            e.Handled = true;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 点击 Toast 任意区域：复制译文到剪贴板 + 关闭
        try
        {
            if (!string.IsNullOrEmpty(TranslatedTextBlock.Text))
            {
                System.Windows.Clipboard.SetText(TranslatedTextBlock.Text);
            }
        }
        catch { }

        ForceClose();
        e.Handled = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        _autoCloseTimer?.Stop();
        _autoCloseTimer = null;
        if (_currentToast == this)
        {
            _currentToast = null;
        }
        base.OnClosed(e);
    }
}
