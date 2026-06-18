using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using TranslateTool.Models;
using TranslateTool.ViewModels;

namespace TranslateTool.Views;

public partial class LlmProviderTemplateWindow : Window
{
    public LlmProviderTemplateWindow(LlmProviderTemplateViewModel viewModel)
    {
        InitializeComponent();
        LoadIcon();
        DataContext = viewModel;
        viewModel.CloseWindow = result =>
        {
            SelectedProvider = result;
            DialogResult = result is not null;
        };
    }

    public LlmProvider? SelectedProvider { get; private set; }

    private void LoadIcon()
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "TranslateTool.ico");
        if (File.Exists(iconPath))
        {
            Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
        }
    }
}
