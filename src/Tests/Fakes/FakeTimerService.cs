using TranslateTool.Services;

namespace TranslateTool.Tests.Fakes;

public sealed class FakeTimerService : ITimerService
{
    public event EventHandler? Tick;

    public TimeSpan Interval { get; set; }

    public bool IsEnabled { get; private set; }

    public void Start() => IsEnabled = true;

    public void Stop() => IsEnabled = false;

    public void Dispose() => Stop();

    public void TriggerTick()
    {
        Tick?.Invoke(this, EventArgs.Empty);
    }
}
