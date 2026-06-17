using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateTool.Models;
using TranslateTool.Services;

namespace TranslateTool.ViewModels;

public partial class LlmProviderTemplateViewModel : ObservableObject
{
    public ObservableCollection<LlmProvider> Templates { get; } = new();

    [ObservableProperty]
    private LlmProvider? _selectedTemplate;

    public LlmProviderTemplateViewModel()
    {
        foreach (var template in LlmProviderService.GetBuiltInTemplates())
        {
            Templates.Add(template);
        }
    }

    [RelayCommand]
    private void Select()
    {
        CloseWindow?.Invoke(SelectedTemplate);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke(null);
    }

    public Action<LlmProvider?>? CloseWindow { get; set; }
}
