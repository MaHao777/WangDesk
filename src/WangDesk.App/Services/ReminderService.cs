using System.Timers;

namespace WangDesk.App.Services;

/// <summary>
/// 定时提醒服务实现
/// </summary>
public class ReminderService : IReminderService, IDisposable
{
    private System.Timers.Timer? _timer;
    private DateTime _startTime;
    private int _focusIntervalMinutes;
    private int _breakIntervalMinutes;
    private PomodoroMode _currentMode = PomodoroMode.Focus;
    private readonly object _lockObject = new();

    public bool IsRunning => _timer?.Enabled ?? false;
    public PomodoroMode CurrentMode => _currentMode;
    public int FocusIntervalMinutes => _focusIntervalMinutes;
    public int BreakIntervalMinutes => _breakIntervalMinutes;

    public event EventHandler<ReminderTriggeredEventArgs>? ReminderTriggered;

    public ReminderService(int defaultFocusIntervalMinutes = 45, int defaultBreakIntervalMinutes = 5)
    {
        _focusIntervalMinutes = ClampMinutes(defaultFocusIntervalMinutes);
        _breakIntervalMinutes = ClampMinutes(defaultBreakIntervalMinutes);
    }

    public void StartFocus()
    {
        lock (_lockObject)
        {
            _currentMode = PomodoroMode.Focus;
            StartTimer();
        }
    }

    public void StartBreak()
    {
        lock (_lockObject)
        {
            _currentMode = PomodoroMode.Break;
            StartTimer();
        }
    }

    private void StartTimer()
    {
        if (_timer == null)
        {
            _timer = new System.Timers.Timer(1000); // 每秒更新一次
            _timer.Elapsed += OnTimerElapsed;
        }

        _startTime = DateTime.Now;
        _timer.Start();
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
        var normalized = ClampMinutes(minutes);

        lock (_lockObject)
        {
            _focusIntervalMinutes = normalized;
            if (IsRunning && _currentMode == PomodoroMode.Focus)
            {
                _startTime = DateTime.Now;
            }
        }
    }

    public void SetBreakInterval(int minutes)
    {
        var normalized = ClampMinutes(minutes);

        lock (_lockObject)
        {
            _breakIntervalMinutes = normalized;
            if (IsRunning && _currentMode == PomodoroMode.Break)
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
                return TimeSpan.FromMinutes(GetCurrentIntervalMinutes());
            }

            var elapsed = DateTime.Now - _startTime;
            var remaining = TimeSpan.FromMinutes(GetCurrentIntervalMinutes()) - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        bool shouldNotify = false;
        PomodoroMode completedMode = PomodoroMode.Focus;

        lock (_lockObject)
        {
            if (!IsRunning) return;

            var elapsed = DateTime.Now - _startTime;
            var intervalMinutes = GetCurrentIntervalMinutes();
            if (elapsed.TotalMinutes >= intervalMinutes)
            {
                _timer?.Stop();
                completedMode = _currentMode;
                shouldNotify = true;
            }
        }

        if (shouldNotify)
        {
            ReminderTriggered?.Invoke(this, new ReminderTriggeredEventArgs(completedMode));
        }
    }

    private int GetCurrentIntervalMinutes()
    {
        return _currentMode == PomodoroMode.Break ? _breakIntervalMinutes : _focusIntervalMinutes;
    }

    private static int ClampMinutes(int minutes)
    {
        if (minutes < 1) return 1;
        if (minutes > 180) return 180;
        return minutes;
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }
}
