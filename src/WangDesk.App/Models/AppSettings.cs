using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WangDesk.App.Models;

/// <summary>
/// 应用程序设置模型
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    private string _baiduTranslateAppId = string.Empty;
    private string _baiduTranslateSecretKey = string.Empty;
    private int _reminderIntervalMinutes = 45;
    private bool _autoStartEnabled;

    /// <summary>
    /// 百度翻译App ID
    /// </summary>
    public string BaiduTranslateAppId
    {
        get => _baiduTranslateAppId;
        set => SetProperty(ref _baiduTranslateAppId, value);
    }

    /// <summary>
    /// 百度翻译密钥
    /// </summary>
    public string BaiduTranslateSecretKey
    {
        get => _baiduTranslateSecretKey;
        set => SetProperty(ref _baiduTranslateSecretKey, value);
    }

    /// <summary>
    /// 提醒间隔（分钟）
    /// </summary>
    public int ReminderIntervalMinutes
    {
        get => _reminderIntervalMinutes;
        set => SetProperty(ref _reminderIntervalMinutes, value);
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
