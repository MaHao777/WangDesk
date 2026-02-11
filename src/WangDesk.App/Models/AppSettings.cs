using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WangDesk.App.Models;

/// <summary>
/// 应用程序设置模型
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    private int _reminderIntervalMinutes = 45;
    private int _breakIntervalMinutes = 5;
    private bool _autoStartEnabled;

    /// <summary>
    /// 提醒间隔（分钟）
    /// </summary>
    public int ReminderIntervalMinutes
    {
        get => _reminderIntervalMinutes;
        set => SetProperty(ref _reminderIntervalMinutes, value);
    }

    /// <summary>
    /// 休息间隔（分钟）
    /// </summary>
    public int BreakIntervalMinutes
    {
        get => _breakIntervalMinutes;
        set => SetProperty(ref _breakIntervalMinutes, value);
    }

    /// <summary>
    /// 开机自启
    /// </summary>
    public bool AutoStartEnabled
    {
        get => _autoStartEnabled;
        set => SetProperty(ref _autoStartEnabled, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null!)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
