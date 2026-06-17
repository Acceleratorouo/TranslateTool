using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
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

    [ObservableProperty]
    private string _selectedModel = "";

    public ObservableCollection<string> AvailableModels { get; } = new();

    [ObservableProperty]
    private bool _isDeploying;

    [ObservableProperty]
    private int _deployProgress;

    [ObservableProperty]
    private string _deployStatusText = "";

    public bool HasDeployStatus => !string.IsNullOrEmpty(DeployStatusText);

    partial void OnDeployStatusTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasDeployStatus));
    }

    public LlmProviderSettingsViewModel()
    {
        LlmProviderService.EnsureDefaultTemplate();
        Providers = new ObservableCollection<LlmProvider>(LlmProviderService.Providers);
        SystemPrompt = LlmProviderService.Settings.SystemPrompt;
        Temperature = LlmProviderService.Settings.Temperature;
        MaxTokens = LlmProviderService.Settings.MaxTokens;
        TimeoutSeconds = LlmProviderService.Settings.TimeoutSeconds;
        RefreshAvailableModels();
    }

    /// <summary>
    /// 从默认供应商加载可用模型列表，并保持当前选中的模型（若仍存在）。
    /// </summary>
    private void RefreshAvailableModels()
    {
        var previousModel = LlmProviderService.Settings.DefaultModel ?? "";
        AvailableModels.Clear();

        var provider = LlmProviderService.GetDefaultProvider();
        if (provider is not null)
        {
            foreach (var m in provider.Models)
            {
                AvailableModels.Add(m);
            }
        }

        // 选中之前保存的模型；若不存在则选第一个（若有）
        if (AvailableModels.Contains(previousModel))
        {
            SelectedModel = previousModel;
        }
        else if (AvailableModels.Count > 0)
        {
            SelectedModel = AvailableModels[0];
        }
        else
        {
            SelectedModel = "";
        }
    }

    /// <summary>
    /// 当供应商列表刷新后（增删改/设默认），同步刷新模型列表。
    /// </summary>
    partial void OnSelectedModelChanged(string value)
    {
        // 即时更新到 Settings，便于后续翻译直接使用
        LlmProviderService.Settings.DefaultModel = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void RefreshProviders()
    {
        Providers.Clear();
        foreach (var p in LlmProviderService.Providers)
        {
            Providers.Add(p);
        }
        RefreshAvailableModels();
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

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task DeployLocalGemmaAsync()
    {
        // 检查 Ollama 是否安装
        if (!IsOllamaInstalled())
        {
            StatusMessage = "❌ 未检测到 Ollama，请先安装 Ollama";
            DeployStatusText = "❌ 未检测到 Ollama，请先安装 Ollama（点击查看操作文档）";
            DeployProgress = 0;
            try
            {
                Process.Start(new ProcessStartInfo("https://ollama.com") { UseShellExecute = true });
            }
            catch { }
            return;
        }

        IsDeploying = true;
        DeployProgress = 0;
        DeployStatusText = "正在拉取 Gemma 4 E4B 模型...";
        StatusMessage = "正在部署本地 Gemma 4 E4B...";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "pull gemma4:e4b",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = Process.Start(psi)!;
            var readTask = ReadProgressAsync(process);

            await process.WaitForExitAsync();
            await readTask;

            if (process.ExitCode == 0)
            {
                DeployProgress = 100;
                DeployStatusText = "✅ Gemma 4 E4B 部署完成";
                StatusMessage = "✅ Gemma 4 E4B 部署完成，请将 Ollama 供应商设为默认";
            }
            else
            {
                DeployStatusText = $"❌ 部署失败（退出码 {process.ExitCode}）";
                StatusMessage = "❌ 部署失败";
            }
        }
        catch (Exception ex)
        {
            DeployStatusText = $"❌ 部署异常: {ex.Message}";
            StatusMessage = "❌ 部署异常";
        }
        finally
        {
            IsDeploying = false;
        }
    }

    private async Task ReadProgressAsync(Process process)
    {
        var stdoutTask = ReadStreamAsync(process.StandardOutput);
        var stderrTask = ReadStreamAsync(process.StandardError);
        await Task.WhenAll(stdoutTask, stderrTask);
    }

    private async Task ReadStreamAsync(StreamReader reader)
    {
        var sb = new StringBuilder();
        var buffer = new char[4096];

        while (true)
        {
            int read = await reader.ReadAsync(buffer, 0, buffer.Length);
            if (read == 0) break;

            for (int i = 0; i < read; i++)
            {
                char c = buffer[i];
                if (c == '\r' || c == '\n')
                {
                    if (sb.Length > 0)
                    {
                        ProcessProgressLine(sb.ToString());
                        sb.Clear();
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        if (sb.Length > 0)
        {
            ProcessProgressLine(sb.ToString());
        }
    }

    private void ProcessProgressLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        // 移除 ANSI 转义码
        var clean = Regex.Replace(line, @"\x1b\[[0-9;]*[a-zA-Z]", "");
        var match = Regex.Match(clean, @"(\d+)%");

        if (match.Success && int.TryParse(match.Groups[1].Value, out var pct))
        {
            var clamped = Math.Clamp(pct, 0, 100);
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                DeployProgress = clamped;
                DeployStatusText = $"拉取模型中... {clamped}%";
            });
        }
    }

    private static bool IsOllamaInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "list",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(5000);
            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        LlmProviderService.Settings.SystemPrompt = SystemPrompt;
        LlmProviderService.Settings.Temperature = Temperature;
        LlmProviderService.Settings.MaxTokens = MaxTokens;
        LlmProviderService.Settings.TimeoutSeconds = TimeoutSeconds;
        LlmProviderService.Settings.DefaultModel = string.IsNullOrWhiteSpace(SelectedModel) ? null : SelectedModel;
        LlmProviderService.SaveProviders();
        StatusMessage = "✅ AI 翻译设置已保存";
    }
}
