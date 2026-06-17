using System.Windows.Controls;
using TranslateTool.ViewModels;

namespace TranslateTool.Views;

public partial class LlmProviderSettingsPage : System.Windows.Controls.UserControl
{
    public LlmProviderSettingsPage()
    {
        InitializeComponent();
        DataContext = new LlmProviderSettingsViewModel();
    }
}
