using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using TranslateTool.ViewModels;

namespace TranslateTool.Views;

public partial class LlmProviderEditWindow : Window
{
    public LlmProviderEditWindow(LlmProviderEditViewModel viewModel)
    {
        InitializeComponent();
        LoadIcon();
        DataContext = viewModel;
        viewModel.CloseWindow = succeeded => DialogResult = succeeded;
    }

    private void LoadIcon()
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "TranslateTool.ico");
        if (File.Exists(iconPath))
        {
            Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
        }
    }
}
