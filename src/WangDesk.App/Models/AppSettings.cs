using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WangDesk.App.Models;

/// <summary>
/// 应用程序设置模型
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    public const string DefaultReminderSoundSelectionId = "builtin:asterisk";

    private int _reminderIntervalMinutes = 45;
    private int _breakIntervalMinutes = 5;
    private bool _autoStartEnabled;
    private ReminderSoundType _reminderSound = ReminderSoundType.Asterisk;
    private string _reminderSoundSelectionId = DefaultReminderSoundSelectionId;
    private List<CustomReminderSoundItem> _customReminderSounds = [];
    private DateOnly _focusTodayDate = DateOnly.FromDateTime(DateTime.Now);
    private int _focusTodayCompletedSeconds;

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

    /// <summary>
    /// 番茄钟提醒音效
    /// </summary>
    public ReminderSoundType ReminderSound
    {
        get => _reminderSound;
        set => SetProperty(ref _reminderSound, NormalizeReminderSound(value));
    }

    /// <summary>
    /// 当前提醒音效选择（builtin:* 或 custom:*）
    /// </summary>
    public string ReminderSoundSelectionId
    {
        get => _reminderSoundSelectionId;
        set => SetProperty(ref _reminderSoundSelectionId, NormalizeReminderSoundSelectionId(value));
    }

    /// <summary>
    /// 自定义提醒音效列表
    /// </summary>
    public List<CustomReminderSoundItem> CustomReminderSounds
    {
        get => _customReminderSounds;
        set => SetProperty(ref _customReminderSounds, NormalizeCustomReminderSounds(value));
    }

    /// <summary>
    /// 今日专注统计对应的本地日期
    /// </summary>
    public DateOnly FocusTodayDate
    {
        get => _focusTodayDate;
        set => SetProperty(ref _focusTodayDate, NormalizeFocusTodayDate(value));
    }

    /// <summary>
    /// 今日已完成专注累计秒数
    /// </summary>
    public int FocusTodayCompletedSeconds
    {
        get => _focusTodayCompletedSeconds;
        set => SetProperty(ref _focusTodayCompletedSeconds, NormalizeFocusTodayCompletedSeconds(value));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null!)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private static ReminderSoundType NormalizeReminderSound(ReminderSoundType value)
    {
        return Enum.IsDefined(typeof(ReminderSoundType), value) ? value : ReminderSoundType.Asterisk;
    }

    private static string NormalizeReminderSoundSelectionId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultReminderSoundSelectionId;
        }

        return value.Trim();
    }

    private static List<CustomReminderSoundItem> NormalizeCustomReminderSounds(List<CustomReminderSoundItem>? value)
    {
        return value ?? [];
    }

    private static DateOnly NormalizeFocusTodayDate(DateOnly value)
    {
        return value == default ? DateOnly.FromDateTime(DateTime.Now) : value;
    }

    private static int NormalizeFocusTodayCompletedSeconds(int value)
    {
        return value < 0 ? 0 : value;
    }
}
