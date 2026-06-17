using System.Windows;
using TranslateTool.ViewModels;

namespace TranslateTool.Views;

public partial class LlmProviderEditWindow : Window
{
    public LlmProviderEditWindow(LlmProviderEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseWindow = succeeded => DialogResult = succeeded;
    }
}
