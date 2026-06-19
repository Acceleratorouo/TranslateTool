namespace TranslateTool.Services;

/// <summary>
/// 计时器服务抽象，用于剪贴板监听等周期性任务。
/// </summary>
public interface ITimerService : IDisposable
{
    /// <summary>
    /// 计时器触发事件。
    /// </summary>
    event EventHandler? Tick;

    /// <summary>
    /// 触发间隔。
    /// </summary>
    TimeSpan Interval { get; set; }

    /// <summary>
    /// 是否正在运行。
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 启动计时器。
    /// </summary>
    void Start();

    /// <summary>
    /// 停止计时器。
    /// </summary>
    void Stop();
}
