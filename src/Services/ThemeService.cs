using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace TranslateTool.Services;

/// <summary>
/// 主题服务 — 支持浅色/深色模式切换
/// 参考 Windows 11 Mica 和 macOS 26 设计语言
/// </summary>
public static class ThemeService
{
    private static bool _isDarkMode;
    private static int _themeDictIndex = -1;
    private static bool _initialized;

    /// <summary>
    /// 当前是否深色模式
    /// </summary>
    public static bool IsDarkMode => _isDarkMode;

    /// <summary>
    /// 切换深色/浅色模式
    /// </summary>
    public static void ToggleDarkMode()
    {
        _isDarkMode = !_isDarkMode;
        ApplyTheme();
    }

    /// <summary>
    /// 设置深色模式
    /// </summary>
    public static void SetDarkMode(bool isDark)
    {
        _isDarkMode = isDark;
        ApplyTheme();
    }

    /// <summary>
    /// 应用主题 - 通过替换主题资源字典实现
    /// </summary>
    private static void ApplyTheme()
    {
        try
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            // 确保在 UI 线程执行
            if (!app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.Invoke(() => ApplyTheme());
                return;
            }

            var mergedDictionaries = app.Resources.MergedDictionaries;
            if (mergedDictionaries == null) return;

            // 首次调用时找到主题字典的位置
            if (!_initialized)
            {
                for (int i = 0; i < mergedDictionaries.Count; i++)
                {
                    var source = mergedDictionaries[i].Source?.OriginalString ?? "";
                    if (source.Contains("LightTheme") || source.Contains("DarkTheme"))
                    {
                        _themeDictIndex = i;
                        _initialized = true;
                        break;
                    }
                }
                
                // 如果没找到，添加到末尾
                if (!_initialized)
                {
                    _themeDictIndex = mergedDictionaries.Count;
                }
            }

            // 创建新的主题字典
            var themePath = _isDarkMode ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
            
            ResourceDictionary newThemeDict;
            
            // 方法1：尝试使用 pack URI
            try
            {
                var themeUri = new Uri($"pack://application:,,,/{themePath}", UriKind.Absolute);
                newThemeDict = new ResourceDictionary { Source = themeUri };
            }
            catch
            {
                // 方法2：尝试从文件系统加载
                try
                {
                    var appDomain = AppDomain.CurrentDomain;
                    var basePath = appDomain.BaseDirectory;
                    var filePath = System.IO.Path.Combine(basePath, themePath);
                    
                    if (System.IO.File.Exists(filePath))
                    {
                        using var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                        newThemeDict = (ResourceDictionary)XamlReader.Load(fileStream);
                    }
                    else
                    {
                        // 方法3：从源目录加载（开发环境）
                        var srcPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(basePath, "..", "..", "..", "..", themePath));
                        if (System.IO.File.Exists(srcPath))
                        {
                            using var fileStream = new System.IO.FileStream(srcPath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                            newThemeDict = (ResourceDictionary)XamlReader.Load(fileStream);
                        }
                        else
                        {
                            throw new Exception($"Theme file not found: {themePath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load theme from file: {ex.Message}");
                    return;
                }
            }

            if (_themeDictIndex >= 0 && _themeDictIndex < mergedDictionaries.Count)
            {
                // 替换现有主题字典
                mergedDictionaries[_themeDictIndex] = newThemeDict;
            }
            else
            {
                // 添加新主题字典
                mergedDictionaries.Add(newThemeDict);
                _themeDictIndex = mergedDictionaries.Count - 1;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Theme switch failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 初始化主题（应用启动时调用）
    /// </summary>
    public static void Initialize(bool isDark)
    {
        _isDarkMode = isDark;
        _initialized = false; // 强制重新查找索引
        ApplyTheme();
    }
}
