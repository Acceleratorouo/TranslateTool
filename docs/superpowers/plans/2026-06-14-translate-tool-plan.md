# 自动翻译软件实现计划

> **For agentic workers:** Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 构建一个 Windows 桌面翻译工具，通过悬浮窗入口提供文本粘贴翻译、文件翻译、全屏自动翻译、截图框选翻译四种模式。

**Architecture:** WPF + HandyControl 桌面应用，MVVM 模式。悬浮窗为系统托盘常驻入口，右键菜单选择翻译模式。翻译引擎封装统一接口，默认使用谷歌/微软免费网页翻译，支持自定义 API 切换。

**Tech Stack:** C# / .NET 8 WPF, HandyControl, CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, Hardcodet.NotifyIcon.Wpf

---

## 文件结构

```
TranslateTool/
├── src/
│   ├── Models/
│   │   ├── AppSettings.cs              — 应用配置（语言、引擎、快捷键）
│   │   ├── TranslationMode.cs           — 翻译模式枚举
│   │   └── TranslationResult.cs         — 翻译结果模型
│   ├── Services/
│   │   ├── ITranslator.cs               — 翻译引擎接口
│   │   ├── GoogleTranslator.cs          — 谷歌翻译实现
│   │   ├── MicrosoftTranslator.cs       — 微软翻译实现
│   │   ├── TranslatorFactory.cs         — 工厂方法
│   │   ├── ScreenCaptureService.cs      — 屏幕截取
│   │   └── ClipboardHelper.cs           — 剪贴板工具
│   ├── ViewModels/
│   │   ├── MainViewModel.cs             — 主窗口 VM
│   │   └── FloatingWindowViewModel.cs   — 悬浮窗 VM
│   ├── Views/
│   │   ├── App.xaml / App.xaml.cs       — 应用入口 & DI 配置
│   │   ├── MainWindow.xaml/cs           — 主窗口
│   │   └── FloatingWindow.xaml/cs       — 悬浮窗
│   └── Converters/
│       └── BoolToVisibilityConverter.cs — 值转换器
├── TranslateTool.csproj
└── packages/
```

---

## Task 1: 创建项目骨架

**Files:**
- Create: `TranslateTool.csproj`
- Create: `src/App.xaml`
- Create: `src/App.xaml.cs`
- Create: `src/AssemblyInfo.cs`

- [ ] **Step 1: 创建 .NET 8 WPF 项目文件**

在 `e:\vibe coding\3\` 目录下运行：

```powershell
cd "e:\vibe coding\3"
dotnet new wpf -n TranslateTool -f net8.0-windows
```

这会自动创建 `TranslateTool.csproj`、`App.xaml`、`MainWindow.xaml` 等基础文件。

- [ ] **Step 2: 更新 .csproj 添加 NuGet 包**

修改 `TranslateTool.csproj`，添加以下包：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <ApplicationIcon></ApplicationIcon>
    <AssemblyName>TranslateTool</AssemblyName>
    <RootNamespace>TranslateTool</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="HandyControl" Version="3.5.1" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
    <PackageReference Include="Hardcodet.NotifyIcon.Wpf" Version="1.1.0" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: 将文件移到正确目录**

```powershell
cd "e:\vibe coding\3"
mkdir src
mv TranslateTool\*.xsl* src\
mv TranslateTool\AssemblyInfo.cs src\
rm -r TranslateTool
```

- [ ] **Step 4: 更新 .csproj 中的根命名空间**

确保 `src\TranslateTool.csproj`（或重命名为 `src\TranslateTool.csproj`）中的 `<RootNamespace>` 为 `TranslateTool`。

- [ ] **Step 5: 验证项目编译**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
```

预期输出：`Build succeeded. 0 Warning(s), 0 Error(s)`

---

## Task 2: 创建数据模型

**Files:**
- Create: `src\Models\AppSettings.cs`
- Create: `src\Models\TranslationMode.cs`
- Create: `src\Models\TranslationResult.cs`

- [ ] **Step 1: 创建翻译模式枚举**

```csharp
namespace TranslateTool.Models;

public enum TranslationMode
{
    Paste,        // 文本粘贴翻译
    File,         // 文件翻译
    FullScreen,   // 全屏自动翻译
    Region        // 截图框选翻译
}
```

- [ ] **Step 2: 创建翻译结果模型**

```csharp
namespace TranslateTool.Models;

public class TranslationResult
{
    public string SourceText { get; set; } = "";
    public string TranslatedText { get; set; } = "";
    public string SourceLanguage { get; set; } = "";
    public string TargetLanguage { get; set; } = "";
    public string Engine { get; set; } = "";
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
```

- [ ] **Step 3: 创建应用设置模型**

```csharp
namespace TranslateTool.Models;

public class AppSettings
{
    public string SourceLanguage { get; set; } = "auto";
    public string TargetLanguage { get; set; } = "zh-CN";
    public string TranslationEngine { get; set; } = "Google";
    public string? ApiKey { get; set; }
    public string? ApiEndpoint { get; set; }
    public bool ShowFloatingWindow { get; set; } = true;
    public double FloatingWindowTop { get; set; } = 100;
    public double FloatingWindowLeft { get; set; } = 100;
    public bool FloatingWindowAlwaysOnTop { get; set; } = true;
}
```

- [ ] **Step 4: 验证编译**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
```

---

## Task 3: 实现翻译引擎接口与工厂

**Files:**
- Create: `src\Services\ITranslator.cs`
- Create: `src\Services\TranslatorFactory.cs`

- [ ] **Step 1: 创建 ITranslator 接口**

```csharp
using TranslateTool.Models;

namespace TranslateTool.Services;

public interface ITranslator
{
    string Name { get; }
    Task<TranslationResult> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage);
}
```

- [ ] **Step 2: 创建 TranslatorFactory**

```csharp
using TranslateTool.Models;

namespace TranslateTool.Services;

public static class TranslatorFactory
{
    public static ITranslator Create(string engineName)
    {
        return engineName.ToLowerInvariant() switch
        {
            "google" => new GoogleTranslator(),
            "microsoft" => new MicrosoftTranslator(),
            _ => throw new NotSupportedException(
                $"Engine '{engineName}' is not supported.")
        };
    }
}
```

- [ ] **Step 3: 验证编译**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
```

---

## Task 4: 实现谷歌翻译引擎

**Files:**
- Create: `src\Services\GoogleTranslator.cs`

- [ ] **Step 1: 创建谷歌翻译实现（使用免费网页接口）**

```csharp
using System.Text;
using System.Text.Json;
using TranslateTool.Models;

namespace TranslateTool.Services;

public class GoogleTranslator : ITranslator
{
    public string Name => "Google Translate";

    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<TranslationResult> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage)
    {
        var result = new TranslationResult
        {
            SourceText = text,
            SourceLanguage = sourceLanguage,
            TargetLanguage = targetLanguage,
            Engine = Name
        };

        try
        {
            // 使用 Google Translate 免费网页 API (tk 参数)
            var langPair = sourceLanguage == "auto"
                ? $"{targetLanguage}"
                : $"{sourceLanguage}|{targetLanguage}";

            var url = $"https://translate.google.com/translate_a/single?" +
                      $"client=gtx&sl={Uri.EscapeDataString(sourceLanguage)}" +
                      $"&tl={Uri.EscapeDataString(targetLanguage)}" +
                      $"&dt=t&q={Uri.EscapeDataString(text)}";

            var response = await _http.GetStringAsync(url);
            var json = JsonDocument.Parse(response);

            // Google Translate API 返回格式: [[["translated_text",...],...,...,...]]
            var translated = json.RootElement[0]
                .EnumerateArray()
                .First()
                .EnumerateArray()
                .First()[0]
                .GetString() ?? "";

            result.TranslatedText = translated;
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}
```

- [ ] **Step 2: 验证编译**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
```

---

## Task 5: 实现微软翻译引擎

**Files:**
- Create: `src\Services\MicrosoftTranslator.cs`

- [ ] **Step 1: 创建微软翻译实现（使用免费网页接口）**

```csharp
using System.Text;
using System.Text.Json;
using TranslateTool.Models;

namespace TranslateTool.Services;

public class MicrosoftTranslator : ITranslator
{
    public string Name => "Microsoft Translate";

    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<TranslationResult> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage)
    {
        var result = new TranslationResult
        {
            SourceText = text,
            SourceLanguage = sourceLanguage,
            TargetLanguage = targetLanguage,
            Engine = Name
        };

        try
        {
            var body = JsonSerializer.Serialize(new[] {
                new { Text = text }
            });

            var content = new StringContent(body, Encoding.UTF8, "application/json");

            // 使用 Microsoft Translator API V3 (免费层)
            // 注意：实际生产需要 Azure 订阅密钥
            // 这里用网页接口做免费替代
            var url = $"https://api.microsofttranslator.com/v2/http.svc/Translate?" +
                      $"appId={Uri.EscapeDataString("")}" +
                      $"&from={Uri.EscapeDataString(sourceLanguage)}" +
                      $"&to={Uri.EscapeDataString(targetLanguage)}" +
                      $"&text={Uri.EscapeDataString(text)}" +
                      "&contentType=text/plain";

            var response = await _http.GetStringAsync(url);
            // 微软返回 XML，简单提取
            var xmlStart = response.IndexOf(">") + 1;
            var xmlEnd = response.LastIndexOf("<");
            result.TranslatedText = response[xmlStart..xmlEnd];
            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}
```

- [ ] **Step 2: 验证编译**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
```

---

## Task 6: 实现屏幕截取与剪贴板工具

**Files:**
- Create: `src\Services\ScreenCaptureService.cs`
- Create: `src\Services\ClipboardHelper.cs`

- [ ] **Step 1: 创建屏幕截取服务**

```csharp
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace TranslateTool.Services;

public static class ScreenCaptureService
{
    /// <summary>
    /// 截取整个屏幕
    /// </summary>
    public static Bitmap CaptureScreen()
    {
        var bounds = Screen.PrimaryScreen.Bounds;
        return CaptureRectangle(bounds);
    }

    /// <summary>
    /// 截取指定矩形区域
    /// </summary>
    public static Bitmap CaptureRectangle(Rectangle bounds)
    {
        using var screenshot = new Bitmap(
            bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

        using var g = Graphics.FromImage(screenshot);
        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

        return screenshot;
    }

    /// <summary>
    /// 将 Bitmap 转为字节数组（PNG 格式）
    /// </summary>
    public static byte[] BitmapToBytes(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}
```

- [ ] **Step 2: 创建剪贴板工具**

```csharp
using System.Windows.Forms;

namespace TranslateTool.Services;

public static class ClipboardHelper
{
    public static string GetClipboardText()
    {
        try
        {
            return Clipboard.GetText(TextDataFormat.Text);
        }
        catch
        {
            return "";
        }
    }

    public static void SetClipboardText(string text)
    {
        try
        {
            Clipboard.SetText(text, TextDataFormat.Text);
        }
        catch { /* Ignore clipboard errors */ }
    }
}
```

- [ ] **Step 3: 验证编译**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
```

---

## Task 7: 实现 MVVM ViewModel

**Files:**
- Create: `src\ViewModels\MainViewModel.cs`
- Create: `src\ViewModels\FloatingWindowViewModel.cs`

- [ ] **Step 1: 创建主窗口 ViewModel**

```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateTool.Models;
using TranslateTool.Services;

namespace TranslateTool.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    [ObservableProperty] private string _sourceText = "";
    [ObservableProperty] private string _translatedText = "";
    [ObservableProperty] private bool _isTranslating = false;
    [ObservableProperty] private string _sourceLanguage = "auto";
    [ObservableProperty] private string _targetLanguage = "zh-CN";

    public ObservableCollection<string> Languages { get; } = new()
    {
        "auto", "zh-CN", "en-US", "ja-JP", "ko-KR",
        "fr-FR", "de-DE", "es-ES", "ru-RU", "pt-BR"
    };

    public ICommand TranslateCommand { get; }
    public ICommand CopyResultCommand { get; }

    public MainViewModel()
    {
        TranslateCommand = new RelayCommand(ExecuteTranslate, CanExecuteTranslate);
        CopyResultCommand = new RelayCommand(ExecuteCopyResult);
    }

    private bool CanExecuteTranslate() => !string.IsNullOrWhiteSpace(SourceText);

    private async void ExecuteTranslate()
    {
        IsTranslating = true;
        try
        {
            var engine = AppSettings.Current.TranslationEngine;
            var translator = TranslatorFactory.Create(engine);

            var result = await translator.TranslateAsync(
                SourceText, SourceLanguage, TargetLanguage);

            TranslatedText = result.IsSuccess
                ? result.TranslatedText
                : $"翻译失败: {result.ErrorMessage}";
        }
        finally
        {
            IsTranslating = false;
        }
    }

    private void ExecuteCopyResult()
    {
        if (!string.IsNullOrWhiteSpace(TranslatedText))
            ClipboardHelper.SetClipboardText(TranslatedText);
    }
}
```

- [ ] **Step 2: 创建悬浮窗 ViewModel**

```csharp
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TranslateTool.Models;
using TranslateTool.Services;

namespace TranslateTool.ViewModels;

public partial class FloatingWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _resultText = "右键选择翻译模式";
    [ObservableProperty] private bool _isBusy = false;

    public AppSettings Settings { get; } = AppSettings.Current;

    public ICommand PasteTranslateCommand { get; }
    public ICommand FileTranslateCommand { get; }
    public ICommand FullScreenTranslateCommand { get; }
    public ICommand RegionTranslateCommand { get; }

    public FloatingWindowViewModel()
    {
        PasteTranslateCommand = new RelayCommand(ExecutePasteTranslate);
        FileTranslateCommand = new RelayCommand(ExecuteFileTranslate);
        FullScreenTranslateCommand = new RelayCommand(ExecuteFullScreenTranslate);
        RegionTranslateCommand = new RelayCommand(ExecuteRegionTranslate);
    }

    private async void ExecutePasteTranslate()
    {
        var text = ClipboardHelper.GetClipboardText();
        if (string.IsNullOrWhiteSpace(text))
        {
            ResultText = "剪贴板为空";
            return;
        }

        await DoTranslate(text);
    }

    private void ExecuteFileTranslate()
    {
        ResultText = "文件翻译功能待实现 — 拖入 .docx/.pdf/.txt 文件";
    }

    private void ExecuteFullScreenTranslate()
    {
        ResultText = "全屏翻译功能待实现 — 截图 → OCR → 翻译";
    }

    private void ExecuteRegionTranslate()
    {
        ResultText = "框选翻译功能待实现 — 框选区域 → OCR → 翻译";
    }

    private async Task DoTranslate(string text)
    {
        IsBusy = true;
        try
        {
            var engine = TranslatorFactory.Create(Settings.TranslationEngine);
            var result = await engine.TranslateAsync(
                text, Settings.SourceLanguage, Settings.TargetLanguage);

            ResultText = result.IsSuccess
                ? result.TranslatedText
                : $"翻译失败: {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            ResultText = $"翻译出错: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

- [ ] **Step 3: 创建 AppSettings 单例辅助类**

更新 `src\Models\AppSettings.cs`：

```csharp
namespace TranslateTool.Models;

public class AppSettings
{
    public static AppSettings Current { get; } = new();

    public string SourceLanguage { get; set; } = "auto";
    public string TargetLanguage { get; set; } = "zh-CN";
    public string TranslationEngine { get; set; } = "Google";
    public string? ApiKey { get; set; }
    public string? ApiEndpoint { get; set; }
    public bool ShowFloatingWindow { get; set; } = true;
    public double FloatingWindowTop { get; set; } = 100;
    public double FloatingWindowLeft { get; set; } = 100;
    public bool FloatingWindowAlwaysOnTop { get; set; } = true;
}
```

- [ ] **Step 4: 验证编译**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
```

---

## Task 8: 实现悬浮窗 View

**Files:**
- Modify: `src\App.xaml`
- Modify: `src\App.xaml.cs`
- Create: `src\Views\FloatingWindow.xaml`
- Create: `src\Views\FloatingWindow.xaml.cs`
- Modify/Delete: `src\MainWindow.xaml` (暂用不到，先删除)
- Modify/Delete: `src\MainWindow.xaml.cs`

- [ ] **Step 1: 修改 App.xaml 引入 HandyControl 主题**

```xml
<Application x:Class="TranslateTool.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="Views/FloatingWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml"/>
                <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/Theme.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- [ ] **Step 2: 修改 App.xaml.cs 添加 DI 容器**

```csharp
using Microsoft.Extensions.DependencyInjection;
using TranslateTool.ViewModels;
using TranslateTool.Views;

namespace TranslateTool;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        services.AddSingleton<FloatingWindowViewModel>();
        services.AddSingleton<MainViewModel>();
        Services = services.BuildServiceProvider();

        var window = new FloatingWindow
        {
            DataContext = Services.GetRequiredService<FloatingWindowViewModel>()
        };
        window.Show();

        base.OnStartup(e);
    }
}
```

- [ ] **Step 3: 创建悬浮窗 XAML**

```xml
<Window x:Class="TranslateTool.Views.FloatingWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:hc="https://handyorg.github.io/handycontrol"
        xmlns:vm="clr-namespace:TranslateTool.ViewModels"
        Title="翻译工具"
        Width="320" Height="180"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ResizeMode="NoResize"
        MouseLeftButtonDown="Window_MouseLeftButtonDown"
        MouseRightButtonDown="Window_MouseRightButtonDown">

    <Window.DataContext>
        <vm:FloatingWindowViewModel/>
    </Window.DataContext>

    <Border Background="{DynamicResource RegionBrush}"
            BorderBrush="{DynamicResource SecondaryBrush}"
            BorderThickness="1"
            CornerRadius="8"
            Margin="4">
        <StackPanel Margin="12">
            <TextBlock Text="{Binding ResultText}"
                       TextWrapping="Wrap"
                       TextTrimming="CharacterEllipsis"
                       Foreground="{DynamicResource TextBrush}"
                       FontSize="14"/>

            <ProgressBar Value="0"
                         IsIndeterminate="{Binding IsBusy}"
                         Height="3"
                         Margin="0,8,0,0"/>

            <TextBlock Text="右键选择翻译模式"
                       FontSize="10"
                       Foreground="{DynamicResource SecondaryTextBrush}"
                       HorizontalAlignment="Center"
                       Margin="0,4,0,0"/>
        </StackPanel>
    </Border>
</Window>
```

- [ ] **Step 4: 创建悬浮窗代码隐藏**

```csharp
using System.Windows;
using System.Windows.Input;

namespace TranslateTool.Views;

public partial class FloatingWindow : Window
{
    public FloatingWindow()
    {
        InitializeComponent();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var menu = new ContextMenu();

        var pasteItem = new MenuItem { Header = "📋 文本粘贴翻译" };
        pasteItem.Click += (_, _) =>
        {
            if (DataContext is var vm && vm is TranslateTool.ViewModels.FloatingWindowViewModel v)
                v.ExecutePasteTranslate?.Execute(null);
            menu.IsOpen = false;
        };

        var fileItem = new MenuItem { Header = "📁 文件翻译" };
        fileItem.Click += (_, _) =>
        {
            if (DataContext is TranslateTool.ViewModels.FloatingWindowViewModel v)
                v.ExecuteFileTranslate?.Execute(null);
            menu.IsOpen = false;
        };

        var fullItem = new MenuItem { Header = "🖥️ 全屏自动翻译" };
        fullItem.Click += (_, _) =>
        {
            if (DataContext is TranslateTool.ViewModels.FloatingWindowViewModel v)
                v.ExecuteFullScreenTranslate?.Execute(null);
            menu.IsOpen = false;
        };

        var regionItem = new MenuItem { Header = "✂️ 截图框选翻译" };
        regionItem.Click += (_, _) =>
        {
            if (DataContext is TranslateTool.ViewModels.FloatingWindowViewModel v)
                v.ExecuteRegionTranslate?.Execute(null);
            menu.IsOpen = false;
        };

        menu.Items.Add(pasteItem);
        menu.Items.Add(fileItem);
        menu.Items.Add(fullItem);
        menu.Items.Add(regionItem);
        menu.IsOpen = true;
    }
}
```

- [ ] **Step 5: 验证编译并运行**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
dotnet run
```

预期：一个无边框置顶的浮动窗口出现在屏幕左上角区域，显示"右键选择翻译模式"。

---

## Task 9: 实现文本粘贴翻译（完整版）

**Files:**
- Modify: `src\Views\FloatingWindow.xaml` — 添加剪贴板自动检测
- Modify: `src\ViewModels\FloatingWindowViewModel.cs` — 改进粘贴翻译

- [ ] **Step 1: 改进悬浮窗，添加输入框和翻译按钮**

更新悬浮窗 XAML，增加一个更完整的交互界面：

```xml
<Window x:Class="TranslateTool.Views.FloatingWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:hc="https://handyorg.github.io/handycontrol"
        xmlns:vm="clr-namespace:TranslateTool.ViewModels"
        Title="翻译工具"
        Width="400" Height="300"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ResizeMode="NoResize"
        MouseLeftButtonDown="Window_MouseLeftButtonDown">

    <Window.DataContext>
        <vm:FloatingWindowViewModel/>
    </Window.DataContext>

    <Border Background="{DynamicResource RegionBrush}"
            BorderBrush="{DynamicResource SecondaryBrush}"
            BorderThickness="1"
            CornerRadius="8"
            Margin="4">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <!-- 标题栏 -->
            <Border Grid.Row="0" Background="{DynamicResource PrimaryBrush}"
                    CornerRadius="8,8,0,0" Padding="8">
                <TextBlock Text="🔤 翻译工具" Foreground="White" FontSize="13"/>
            </Border>

            <!-- 翻译结果区域 -->
            <Border Grid.Row="1" Margin="8"
                    Background="{DynamicResource RegionBackgroundBrush}"
                    CornerRadius="4" Padding="8">
                <TextBlock Text="{Binding ResultText}"
                           TextWrapping="Wrap"
                           Foreground="{DynamicResource TextBrush}"
                           FontSize="13"/>
            </Border>

            <!-- 底部按钮 -->
            <StackPanel Grid.Row="2" Orientation="Horizontal"
                        HorizontalAlignment="Right" Margin="0,0,8,8">
                <Button Content="复制" Margin="4,0"
                        Command="{Binding CopyResultCommand}"
                        Style="{StaticResource ButtonPrimary}"/>
            </StackPanel>
        </Grid>
    </Border>
</Window>
```

- [ ] **Step 2: 更新 ViewModel 添加 CopyCommand**

更新 `FloatingWindowViewModel.cs` 添加 `CopyResultCommand`。

- [ ] **Step 3: 验证**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
dotnet run
```

先复制一些文本到剪贴板，然后右键 → 文本粘贴翻译。

---

## Task 10: 系统托盘集成

**Files:**
- Modify: `src\App.xaml.cs` — 添加托盘图标

- [ ] **Step 1: 添加托盘图标**

```csharp
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using TranslateTool.ViewModels;
using TranslateTool.Views;

namespace TranslateTool;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private static NotifyIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        services.AddSingleton<FloatingWindowViewModel>();
        services.AddSingleton<MainViewModel>();
        Services = services.BuildServiceProvider();

        var floatingWindow = new FloatingWindow
        {
            DataContext = Services.GetRequiredService<FloatingWindowViewModel>()
        };
        floatingWindow.Show();

        // 创建托盘图标
        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Shield, // 临时使用系统图标
            Text = "翻译工具",
            Visible = true
        };
        _notifyIcon.Click += (s, args) => floatingWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 2: 在 .csproj 中添加 Using 导入**

确保 `src\TranslateTool.csproj` 有：

```xml
<UsingTask TaskName="GenerateResource" />
```

- [ ] **Step 3: 添加 System.Windows.Forms 引用**

在 `.csproj` 的 `<ItemGroup>` 中添加：

```xml
<Reference Include="System.Windows.Forms" />
```

- [ ] **Step 4: 验证**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
dotnet run
```

预期：应用启动后主窗口在后台，系统托盘显示图标。

---

## Task 11: 文件翻译

**Files:**
- Create: `src\Services\FileTranslationService.cs`

- [ ] **Step 1: 创建文件翻译服务**

```csharp
using System.Text;
using TranslateTool.Models;
using TranslateTool.Services;

namespace TranslateTool.Services;

public static class FileTranslationService
{
    /// <summary>
    /// 从文件提取文本
    /// </summary>
    public static string ExtractText(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".txt" => File.ReadAllText(filePath, Encoding.UTF8),
            ".docx" => ExtractDocxText(filePath),
            ".pdf" => ExtractPdfText(filePath),
            _ => throw new NotSupportedException($"不支持的文件格式: {ext}")
        };
    }

    private static string ExtractDocxText(string filePath)
    {
        // 使用 DocumentFormat.OpenXml
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, false);
        var sb = new StringBuilder();
        foreach (var para in doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>())
        {
            sb.AppendLine(para.InnerText);
        }
        return sb.ToString().Trim();
    }

    private static string ExtractPdfText(string filePath)
    {
        // 使用 PdfPig
        using var stream = File.OpenRead(filePath);
        using var doc = UglyToad.PdfPig.PdfDocument.Open(stream);
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString().Trim();
    }
}
```

- [ ] **Step 2: 更新 .csproj 添加文件解析包**

```xml
<ItemGroup>
  <PackageReference Include="DocumentFormat.OpenXml" Version="3.1.1" />
  <PackageReference Include="PdfPig" Version="0.1.9" />
</ItemGroup>
```

- [ ] **Step 3: 验证编译**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
```

---

## Task 12: 全屏翻译与框选翻译

**Files:**
- Create: `src\Views\ScreenCaptureOverlay.xaml/cs` — 全屏遮罩
- Create: `src\Views\RegionSelectorOverlay.xaml/cs` — 框选遮罩

- [ ] **Step 1: 创建全屏截取 + 翻译流程**

在 `FloatingWindowViewModel` 中补充：

```csharp
private async void ExecuteFullScreenTranslate()
{
    IsBusy = true;
    try
    {
        var bitmap = ScreenCaptureService.CaptureScreen();
        var text = await OcrService.RecognizeTextAsync(bitmap);
        bitmap.Dispose();
        await DoTranslate(text);
    }
    catch (Exception ex)
    {
        ResultText = $"全屏翻译出错: {ex.Message}";
    }
    finally
    {
        IsBusy = false;
    }
}
```

- [ ] **Step 2: 创建 OCR 服务（占位实现）**

```csharp
using System.Drawing;
using System.Threading.Tasks;

namespace TranslateTool.Services;

public static class OcrService
{
    public static async Task<string> RecognizeTextAsync(Bitmap image)
    {
        // TODO: 集成 Tesseract 或 ML.NET OCR
        // 当前返回占位文本
        await Task.Delay(100);
        return "[OCR 识别结果将在这里显示 — 需要安装 Tesseract 引擎]";
    }
}
```

- [ ] **Step 3: 创建框选遮罩层**

```xml
<Window x:Class="TranslateTool.Views.RegionSelectorOverlay"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        WindowStyle="None"
        Background="#80000000"
        Topmost="True"
        ResizeMode="NoResize"
        AllowsTransparency="True"
        MouseLeftButtonDown="Window_MouseLeftButtonDown"
        MouseMove="Window_MouseMove"
        MouseLeftButtonUp="Window_MouseLeftButtonUp">
    <Canvas x:Name="SelectionCanvas" Background="Transparent">
        <Rectangle x:Name="SelectionRect"
                   Fill="#400099FF"
                   Stroke="Blue"
                   StrokeThickness="2"
                   Visibility="Hidden"/>
    </Canvas>
</Window>
```

- [ ] **Step 4: 验证编译**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
```

---

## Task 13: 全局快捷键

**Files:**
- Create: `src\Utils\NativeMethods.cs`
- Modify: `src\App.xaml.cs` — 注册快捷键

- [ ] **Step 1: 创建 Win32 P/Invoke 方法**

```csharp
using System.Runtime.InteropServices;

namespace TranslateTool.Utils;

public static class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id,
        UInt32 fsModifiers, UInt32 vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const int ModControl = 0x0002;
    public const int ModShift = 0x0004;
}
```

- [ ] **Step 2: 在 App 启动时注册快捷键**

在 `App.xaml.cs` 中添加快捷键注册逻辑。

- [ ] **Step 3: 验证**

```powershell
cd "e:\vibe coding\3\src"
dotnet build
dotnet run
```

按快捷键应唤醒悬浮窗。

---

## Task 14: 最终整合测试

- [ ] **Step 1: 完整编译**

```powershell
cd "e:\vibe coding\3\src"
dotnet build --configuration Release
```

- [ ] **Step 2: 功能测试清单**

- [ ] 悬浮窗显示且可拖动
- [ ] 右键菜单显示四个选项
- [ ] 文本粘贴翻译（复制文本 → 右键 → 粘贴翻译 → 看到结果）
- [ ] 复制翻译结果按钮
- [ ] 系统托盘图标
- [ ] 快捷键唤出悬浮窗

- [ ] **Step 3: 清理未使用文件**

```powershell
cd "e:\vibe coding\3\src"
rm -f App.xaml  # 如果还存在的话
rm -f MainWindow.xaml MainWindow.xaml.cs  # 如果还存在的话
```

---

## 后续可迭代方向

| 优先级 | 功能 | 说明 |
|--------|------|------|
| P0 | Tesseract OCR 集成 | 替换 OcrService 占位实现 |
| P0 | 文件拖放 UI | 主窗口拖入文件翻译 |
| P1 | 翻译历史记录 | 保存最近翻译到本地 |
| P1 | 更多翻译引擎 | DeepL、百度、有道 |
| P2 | 实时翻译（边读边译） | 针对网页/文档流式翻译 |
| P2 | 多语言界面切换 | 支持中文/英文 UI |
| P3 | 翻译语音朗读 | 集成 Windows SAPI |
