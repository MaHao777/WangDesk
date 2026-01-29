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
                _timer = new System.Timers.Timer(60000); // 每分钟检查一次
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
            // 如果正在运行，重置计时
            if (IsRunning)
            {
                _startTime = DateTime.Now;
            }
        }
    }

    public int GetRemainingMinutes()
    {
        lock (_lockObject)
        {
            if (!IsRunning) return _intervalMinutes;
            
            var elapsed = DateTime.Now - _startTime;
            var remaining = _intervalMinutes - (int)elapsed.TotalMinutes;
            return remaining > 0 ? remaining : 0;
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_lockObject)
        {
            var elapsed = DateTime.Now - _startTime;
            if (elapsed.TotalMinutes >= _intervalMinutes)
            {
                ReminderTriggered?.Invoke(this, EventArgs.Empty);
                // 重置计时器
                _startTime = DateTime.Now;
            }
        }
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }
}
