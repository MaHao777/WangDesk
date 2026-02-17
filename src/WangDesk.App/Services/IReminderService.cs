namespace WangDesk.App.Services;

/// <summary>
/// 定时提醒服务接口
/// </summary>
public interface IReminderService
{
    /// <summary>
    /// 开始专注计时
    /// </summary>
    void StartFocus();

    /// <summary>
    /// 开始休息计时
    /// </summary>
    void StartBreak();

    /// <summary>
    /// 停止提醒计时
    /// </summary>
    void Stop();

    /// <summary>
    /// 重置计时器
    /// </summary>
    void Reset();

    /// <summary>
    /// 设置专注间隔（分钟）
    /// </summary>
    void SetInterval(int minutes);

    /// <summary>
    /// 设置休息间隔（分钟）
    /// </summary>
    void SetBreakInterval(int minutes);

    /// <summary>
    /// 获取剩余时间（分钟）
    /// </summary>
    int GetRemainingMinutes();

    /// <summary>
    /// 获取剩余时间
    /// </summary>
    TimeSpan GetRemainingTime();

    /// <summary>
    /// 提醒事件
    /// </summary>
    event EventHandler<ReminderTriggeredEventArgs>? ReminderTriggered;

    /// <summary>
    /// 会话结束事件
    /// </summary>
    event EventHandler<PomodoroSessionEndedEventArgs>? SessionEnded;

    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 当前阶段
    /// </summary>
    PomodoroMode CurrentMode { get; }

    /// <summary>
    /// 专注时长（分钟）
    /// </summary>
    int FocusIntervalMinutes { get; }

    /// <summary>
    /// 休息时长（分钟）
    /// </summary>
    int BreakIntervalMinutes { get; }

    /// <summary>
    /// 当前会话开始时间（本地时间）
    /// </summary>
    DateTime? CurrentSessionStartTimeLocal { get; }
}
