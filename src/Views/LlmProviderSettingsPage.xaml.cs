using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using TranslateTool.ViewModels;

namespace TranslateTool.Views;

public partial class LlmProviderSettingsPage : System.Windows.Controls.UserControl
{
    public LlmProviderSettingsPage()
    {
        InitializeComponent();
        DataContext = new LlmProviderSettingsViewModel();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch { }
        e.Handled = true;
    }
}
