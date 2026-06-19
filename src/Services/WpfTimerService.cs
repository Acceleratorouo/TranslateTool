using System.Windows.Threading;

namespace TranslateTool.Services;

/// <summary>
/// 基于 WPF DispatcherTimer 的计时器实现。
/// </summary>
public sealed class WpfTimerService : ITimerService
{
    private readonly DispatcherTimer _timer;

    public event EventHandler? Tick;

    public TimeSpan Interval
    {
        get => _timer.Interval;
        set => _timer.Interval = value;
    }

    public bool IsEnabled => _timer.IsEnabled;

    public WpfTimerService()
    {
        _timer = new DispatcherTimer();
        _timer.Tick += OnTick;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        Tick?.Invoke(this, e);
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }
}
