using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateTool.Models;
using TranslateTool.Services;

namespace TranslateTool.ViewModels;

public partial class LlmProviderEditViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string _notes = "";

    [ObservableProperty]
    private string _homepageUrl = "";

    [ObservableProperty]
    private string _baseUrl = "";

    [ObservableProperty]
    private string _apiKey = "";

    [ObservableProperty]
    private string _apiFormat = "OpenAiCompatible";

    [ObservableProperty]
    private string _authHeader = "Authorization";

    [ObservableProperty]
    private string _authPrefix = "Bearer";

    [ObservableProperty]
    private string _modelsText = "";

    [ObservableProperty]
    private string _statusMessage = "";

    public LlmProvider? EditingProvider { get; }

    public ObservableCollection<string> ApiFormatOptions { get; } = new() { "OpenAiCompatible", "Ollama", "Gemini" };

    public LlmProviderEditViewModel(LlmProvider? provider = null)
    {
        EditingProvider = provider;
        if (provider is not null)
        {
            DisplayName = provider.DisplayName;
            Notes = provider.Notes ?? "";
            HomepageUrl = provider.HomepageUrl ?? "";
            BaseUrl = provider.BaseUrl;
            ApiKey = provider.ApiKey ?? "";
            ApiFormat = provider.ApiFormat.ToString();
            AuthHeader = provider.AuthHeader;
            AuthPrefix = provider.AuthPrefix;
            ModelsText = string.Join(", ", provider.Models);
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task TestConnectionAsync()
    {
        var provider = BuildProvider();
        try
        {
            await LlmProviderService.TestProviderAsync(provider);
            ModelsText = string.Join(", ", provider.Models);
            StatusMessage = $"✅ 连接成功，获取 {provider.Models.Count} 个模型";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 连接失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Save()
    {
        var provider = BuildProvider();
        if (EditingProvider is not null)
        {
            var index = LlmProviderService.Providers.FindIndex(p => p.Id == EditingProvider.Id);
            if (index >= 0)
            {
                LlmProviderService.Providers[index] = provider;
            }
            else
            {
                // EditingProvider was a template (not yet in Providers) — add as new
                LlmProviderService.Providers.Add(provider);
            }
        }
        else
        {
            LlmProviderService.Providers.Add(provider);
        }

        LlmProviderService.SaveProviders();
        CloseWindow?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke(false);
    }

    public Action<bool>? CloseWindow { get; set; }

    private LlmProvider BuildProvider()
    {
        // Determine if we're editing an existing provider (vs. pre-filling from a template)
        bool isExistingEdit = EditingProvider is not null
            && LlmProviderService.Providers.Any(p => p.Id == EditingProvider.Id);

        var provider = isExistingEdit
            ? new LlmProvider { Id = EditingProvider!.Id }
            : new LlmProvider();  // new Guid generated for new/template-derived providers

        provider.DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? "未命名供应商" : DisplayName.Trim();
        provider.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
        provider.HomepageUrl = string.IsNullOrWhiteSpace(HomepageUrl) ? null : HomepageUrl.Trim();
        provider.BaseUrl = BaseUrl.Trim();
        provider.ApiKey = string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey.Trim();
        provider.ApiFormat = Enum.TryParse<LlmApiFormat>(ApiFormat, ignoreCase: true, out var fmt)
            ? fmt
            : LlmApiFormat.OpenAiCompatible;
        provider.AuthHeader = string.IsNullOrWhiteSpace(AuthHeader) ? "Authorization" : AuthHeader.Trim();
        provider.AuthPrefix = string.IsNullOrWhiteSpace(AuthPrefix) ? "Bearer" : AuthPrefix.Trim();
        provider.Models = ModelsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(m => m.Trim())
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToList();
        provider.IsEnabled = EditingProvider?.IsEnabled ?? true;
        provider.IsDefault = EditingProvider?.IsDefault ?? false;

        return provider;
    }
}
