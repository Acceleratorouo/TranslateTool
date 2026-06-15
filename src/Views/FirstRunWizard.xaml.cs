using System.IO;
using System.Windows;
using TranslateTool.Models;

namespace TranslateTool.Views;

public partial class FirstRunWizard : Window
{
    public FirstRunWizard()
    {
        InitializeComponent();
        CheckOcrStatus();
    }

    private void CheckOcrStatus()
    {
        var tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
        var chiSimPath = Path.Combine(tessdataPath, "chi_sim.traineddata");
        var engPath = Path.Combine(tessdataPath, "eng.traineddata");

        if (File.Exists(chiSimPath) && File.Exists(engPath))
        {
            OcrStatusText.Text = "语言包状态：已安装 ✓";
            OcrStatusText.Foreground = (System.Windows.Media.Brush)FindResource("ClaudeSuccess");
            DownloadOcrButton.IsEnabled = false;
            DownloadOcrButton.Content = "已安装";
        }
        else
        {
            OcrStatusText.Text = "语言包状态：未安装（OCR 翻译功能将不可用）";
        }
    }

    private async void DownloadOcr_Click(object sender, RoutedEventArgs e)
    {
        DownloadOcrButton.IsEnabled = false;
        DownloadOcrButton.Content = "正在下载...";
        OcrStatusText.Text = "正在下载 OCR 语言包，请稍候...";

        try
        {
            var progress = new Progress<string>(msg =>
            {
                OcrStatusText.Text = msg;
            });

            await Services.OcrService.InitializeAsync(progress);

            if (Services.OcrService.IsReady)
            {
                OcrStatusText.Text = "语言包状态：已安装 ✓";
                OcrStatusText.Foreground = (System.Windows.Media.Brush)FindResource("ClaudeSuccess");
                DownloadOcrButton.Content = "已安装";
                System.Windows.MessageBox.Show("OCR 语言包下载完成！", "下载成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                OcrStatusText.Text = $"下载失败: {Services.OcrService.InitError}";
                DownloadOcrButton.IsEnabled = true;
                DownloadOcrButton.Content = "重试下载";
            }
        }
        catch (Exception ex)
        {
            OcrStatusText.Text = $"下载失败: {ex.Message}";
            DownloadOcrButton.IsEnabled = true;
            DownloadOcrButton.Content = "重试下载";
        }
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        // 标记已完成首次运行引导（跳过也算完成）
        AppSettings.Current.FirstRunCompleted = true;
        AppSettings.Current.Save();
        Close();
    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        // 保存百度 API 配置
        var appId = BaiduAppIdBox.Text?.Trim();
        var secretKey = BaiduSecretKeyBox.Text?.Trim();

        if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(secretKey))
        {
            AppSettings.Current.BaiduAppId = appId;
            AppSettings.Current.BaiduSecretKey = secretKey;
            AppSettings.Current.TranslationEngine = "baidu";
            AppSettings.Current.Save();

            // 应用到翻译引擎
            Services.BaiduTranslator.SetCredentials(appId, secretKey);

            System.Windows.MessageBox.Show("百度翻译 API 配置已保存！", "配置成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 标记已完成首次运行引导
        AppSettings.Current.FirstRunCompleted = true;
        AppSettings.Current.Save();

        Close();
    }
}
