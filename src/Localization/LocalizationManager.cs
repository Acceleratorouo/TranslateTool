using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace TranslateTool.Localization;

/// <summary>
/// 本地化管理器，支持中英文切换
/// </summary>
public class LocalizationManager : INotifyPropertyChanged
{
    private static LocalizationManager? _instance;
    public static LocalizationManager Instance => _instance ??= new LocalizationManager();

    private ResourceManager _resourceManager;
    private CultureInfo _currentCulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LocalizationManager()
    {
        _resourceManager = new ResourceManager("TranslateTool.Localization.Strings", typeof(LocalizationManager).Assembly);
        _currentCulture = new CultureInfo("zh-CN");
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    public string this[string key]
    {
        get
        {
            try
            {
                return _resourceManager.GetString(key, _currentCulture) ?? key;
            }
            catch
            {
                return key;
            }
        }
    }

    /// <summary>
    /// 当前语言
    /// </summary>
    public string CurrentLanguage
    {
        get => _currentCulture.Name;
        set
        {
            if (_currentCulture.Name != value)
            {
                _currentCulture = new CultureInfo(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            }
        }
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    public void SwitchLanguage(string cultureName)
    {
        CurrentLanguage = cultureName;
    }
}
