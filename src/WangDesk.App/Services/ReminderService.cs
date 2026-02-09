using System.Timers;

namespace WangDesk.App.Services;

/// <summary>
/// 定时提醒服务实现
/// </summary>
public class ReminderService : IReminderService, IDisposable
{
    private System.Timers.Timer? _timer;
    private DateTime _startTime;
    private int _intervalMinutes;
    private readonly object _lockObject = new();

    public bool IsRunning => _timer?.Enabled ?? false;

    public event EventHandler? ReminderTriggered;

    public ReminderService(int defaultIntervalMinutes = 45)
    {
        _intervalMinutes = defaultIntervalMinutes;
    }

    public void Start()
    {
        lock (_lockObject)
        {
            if (_timer == null)
            {
                _timer = new System.Timers.Timer(1000); // 每秒更新一次
                _timer.Elapsed += OnTimerElapsed;
            }

            _startTime = DateTime.Now;
            _timer.Start();
        }
    }

    public void Stop()
    {
        lock (_lockObject)
        {
            _timer?.Stop();
            _startTime = DateTime.Now;
        }
    }

    public void Reset()
    {
        lock (_lockObject)
        {
            _startTime = DateTime.Now;
        }
    }

    public void SetInterval(int minutes)
    {
        if (minutes < 1) minutes = 1;
        if (minutes > 180) minutes = 180;

        lock (_lockObject)
        {
            _intervalMinutes = minutes;
            if (IsRunning)
            {
                _startTime = DateTime.Now;
            }
        }
    }

    public int GetRemainingMinutes()
    {
        return (int)Math.Ceiling(GetRemainingTime().TotalMinutes);
    }

    public TimeSpan GetRemainingTime()
    {
        lock (_lockObject)
        {
            if (!IsRunning)
            {
                return TimeSpan.FromMinutes(_intervalMinutes);
            }

            var elapsed = DateTime.Now - _startTime;
            var remaining = TimeSpan.FromMinutes(_intervalMinutes) - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_lockObject)
        {
            if (!IsRunning) return;

            var elapsed = DateTime.Now - _startTime;
            if (elapsed.TotalMinutes >= _intervalMinutes)
            {
                _timer?.Stop();
                ReminderTriggered?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }
}
