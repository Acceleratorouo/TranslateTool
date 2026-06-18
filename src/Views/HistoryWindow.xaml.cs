using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace TranslateTool.Views;

public partial class HistoryWindow : Window
{
    private CollectionViewSource? _viewSource;

    public HistoryWindow()
    {
        InitializeComponent();
        LoadIcon();
        Loaded += HistoryWindow_Loaded;
    }

    private void LoadIcon()
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "TranslateTool.ico");
        if (File.Exists(iconPath))
        {
            Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
        }
    }

    private void HistoryWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 设置 CollectionViewSource 用于筛选
        _viewSource = new CollectionViewSource
        {
            Source = HistoryList.ItemsSource
        };
        _viewSource.Filter += ViewSource_Filter;
        HistoryList.ItemsSource = _viewSource.View;
    }

    private void ViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (string.IsNullOrEmpty(SearchBox.Text))
        {
            e.Accepted = true;
            return;
        }

        if (e.Item is ViewModels.HistoryEntry entry)
        {
            var searchText = SearchBox.Text.ToLower();
            e.Accepted = entry.SourceText.ToLower().Contains(searchText) ||
                         entry.TranslatedText.ToLower().Contains(searchText) ||
                         entry.Engine.ToLower().Contains(searchText);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewSource?.View.Refresh();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (_viewSource?.View != null)
        {
            var count = _viewSource.View.Cast<object>().Count();
            StatusText.Text = $"共 {count} 条记录";
        }
    }

    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryList.SelectedItem is ViewModels.HistoryEntry entry)
        {
            StatusText.Text = $"来源: {entry.SourceLang} → 目标: {entry.TargetLang} | 引擎: {entry.Engine}";
        }
    }

    private void CopySource_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string text)
        {
            System.Windows.Clipboard.SetText(text);
        }
    }

    private void CopyTranslated_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string text)
        {
            System.Windows.Clipboard.SetText(text);
        }
    }
}
