using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateTool.Models;
using TranslateTool.Services;
using TranslateTool.Views;

namespace TranslateTool.ViewModels;

public partial class LlmProviderSettingsViewModel : ObservableObject
{
    public ObservableCollection<LlmProvider> Providers { get; }

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private string _systemPrompt = "";

    [ObservableProperty]
    private double _temperature;

    [ObservableProperty]
    private int _maxTokens;

    [ObservableProperty]
    private int _timeoutSeconds;

    public LlmProviderSettingsViewModel()
    {
        LlmProviderService.EnsureDefaultTemplate();
        Providers = new ObservableCollection<LlmProvider>(LlmProviderService.Providers);
        SystemPrompt = LlmProviderService.Settings.SystemPrompt;
        Temperature = LlmProviderService.Settings.Temperature;
        MaxTokens = LlmProviderService.Settings.MaxTokens;
        TimeoutSeconds = LlmProviderService.Settings.TimeoutSeconds;
    }

    private void RefreshProviders()
    {
        Providers.Clear();
        foreach (var p in LlmProviderService.Providers)
        {
            Providers.Add(p);
        }
    }

    [RelayCommand]
    private void AddFromTemplate()
    {
        var vm = new LlmProviderTemplateViewModel();
        var window = new LlmProviderTemplateWindow(vm);
        if (window.ShowDialog() == true && window.SelectedProvider is not null)
        {
            var editVm = new LlmProviderEditViewModel(window.SelectedProvider);
            var editWindow = new LlmProviderEditWindow(editVm);
            if (editWindow.ShowDialog() == true)
            {
                RefreshProviders();
            }
        }
    }

    [RelayCommand]
    private void AddCustom()
    {
        var editVm = new LlmProviderEditViewModel();
        var editWindow = new LlmProviderEditWindow(editVm);
        if (editWindow.ShowDialog() == true)
        {
            RefreshProviders();
        }
    }

    [RelayCommand]
    private void EditProvider(LlmProvider? provider)
    {
        if (provider is null) return;
        var editVm = new LlmProviderEditViewModel(provider);
        var editWindow = new LlmProviderEditWindow(editVm);
        editWindow.ShowDialog();
        RefreshProviders();
    }

    [RelayCommand]
    private void DeleteProvider(LlmProvider? provider)
    {
        if (provider is null) return;
        LlmProviderService.Providers.Remove(provider);
        LlmProviderService.SaveProviders();
        RefreshProviders();
    }

    [RelayCommand]
    private void SetDefaultProvider(LlmProvider? provider)
    {
        if (provider is null) return;
        LlmProviderService.SetDefaultProvider(provider.Id);
        RefreshProviders();
    }

    [RelayCommand]
    private void DeployLocalGemma()
    {
        var scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "deploy-gemma4-e4b.ps1");
        if (!System.IO.File.Exists(scriptPath))
        {
            StatusMessage = "❌ 部署脚本未找到";
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = false
        };
        Process.Start(psi);
        StatusMessage = "正在部署本地 Gemma 4 E4B...";
    }

    [RelayCommand]
    private void SaveSettings()
    {
        LlmProviderService.Settings.SystemPrompt = SystemPrompt;
        LlmProviderService.Settings.Temperature = Temperature;
        LlmProviderService.Settings.MaxTokens = MaxTokens;
        LlmProviderService.Settings.TimeoutSeconds = TimeoutSeconds;
        LlmProviderService.SaveProviders();
        StatusMessage = "✅ AI 翻译设置已保存";
    }
}
