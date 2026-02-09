namespace WangDesk.App.Services;

/// <summary>
/// 定时提醒服务接口
/// </summary>
public interface IReminderService
{
    /// <summary>
    /// 开始提醒计时
    /// </summary>
    void Start();

    /// <summary>
    /// 停止提醒计时
    /// </summary>
    void Stop();

    /// <summary>
    /// 重置计时器
    /// </summary>
    void Reset();

    /// <summary>
    /// 设置提醒间隔（分钟）
    /// </summary>
    void SetInterval(int minutes);

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
    event EventHandler? ReminderTriggered;

    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }
}
