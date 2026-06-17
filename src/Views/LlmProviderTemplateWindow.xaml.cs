using System.Windows;
using TranslateTool.Models;
using TranslateTool.ViewModels;

namespace TranslateTool.Views;

public partial class LlmProviderTemplateWindow : Window
{
    public LlmProviderTemplateWindow(LlmProviderTemplateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseWindow = result =>
        {
            SelectedProvider = result;
            DialogResult = result is not null;
        };
    }

    public LlmProvider? SelectedProvider { get; private set; }
}
