using System.Timers;

namespace WangDesk.App.Services;

/// <summary>
/// 定时提醒服务实现
/// </summary>
public class ReminderService : IReminderService, IDisposable
{
    private System.Timers.Timer? _timer;
    private DateTime _startTime;
    private DateTime? _currentSessionStartTimeLocal;
    private int _focusIntervalMinutes;
    private int _breakIntervalMinutes;
    private PomodoroMode _currentMode = PomodoroMode.Focus;
    private readonly object _lockObject = new();

    public bool IsRunning => _timer?.Enabled ?? false;
    public PomodoroMode CurrentMode => _currentMode;
    public int FocusIntervalMinutes => _focusIntervalMinutes;
    public int BreakIntervalMinutes => _breakIntervalMinutes;
    public DateTime? CurrentSessionStartTimeLocal
    {
        get
        {
            lock (_lockObject)
            {
                return _currentSessionStartTimeLocal;
            }
        }
    }

    public event EventHandler<ReminderTriggeredEventArgs>? ReminderTriggered;
    public event EventHandler<PomodoroSessionEndedEventArgs>? SessionEnded;

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
        _currentSessionStartTimeLocal = _startTime;
        _timer.Start();
    }

    public void Stop()
    {
        PomodoroSessionEndedEventArgs? sessionEndedArgs = null;

        lock (_lockObject)
        {
            if (!IsRunning)
            {
                _startTime = DateTime.Now;
                _currentSessionStartTimeLocal = null;
                return;
            }

            var endedAtLocal = DateTime.Now;
            _timer?.Stop();
            sessionEndedArgs = CreateSessionEndedEventArgs(endedAtLocal);
            _startTime = endedAtLocal;
            _currentSessionStartTimeLocal = null;
        }

        if (sessionEndedArgs != null)
        {
            SessionEnded?.Invoke(this, sessionEndedArgs);
        }
    }

    public void Reset()
    {
        lock (_lockObject)
        {
            _startTime = DateTime.Now;
            if (IsRunning)
            {
                _currentSessionStartTimeLocal = _startTime;
            }
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
                _currentSessionStartTimeLocal = _startTime;
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
                _currentSessionStartTimeLocal = _startTime;
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
        PomodoroSessionEndedEventArgs? sessionEndedArgs = null;

        lock (_lockObject)
        {
            if (!IsRunning) return;

            var elapsed = DateTime.Now - _startTime;
            var intervalMinutes = GetCurrentIntervalMinutes();
            if (elapsed.TotalMinutes >= intervalMinutes)
            {
                var endedAtLocal = DateTime.Now;
                _timer?.Stop();
                completedMode = _currentMode;
                sessionEndedArgs = CreateSessionEndedEventArgs(endedAtLocal);
                _startTime = endedAtLocal;
                _currentSessionStartTimeLocal = null;
                shouldNotify = true;
            }
        }

        if (shouldNotify)
        {
            if (sessionEndedArgs != null)
            {
                SessionEnded?.Invoke(this, sessionEndedArgs);
            }
            ReminderTriggered?.Invoke(this, new ReminderTriggeredEventArgs(completedMode));
        }
    }

    private PomodoroSessionEndedEventArgs CreateSessionEndedEventArgs(DateTime endedAtLocal)
    {
        var mode = _currentMode;
        var startedAtLocal = _currentSessionStartTimeLocal ?? _startTime;
        var maxDuration = TimeSpan.FromMinutes(GetCurrentIntervalMinutes());
        var rawElapsed = endedAtLocal - startedAtLocal;
        if (rawElapsed < TimeSpan.Zero)
        {
            rawElapsed = TimeSpan.Zero;
        }

        var elapsed = rawElapsed > maxDuration ? maxDuration : rawElapsed;
        var normalizedEndedAtLocal = startedAtLocal.Add(elapsed);

        return new PomodoroSessionEndedEventArgs(mode, startedAtLocal, normalizedEndedAtLocal, elapsed);
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
