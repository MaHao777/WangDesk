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
}
