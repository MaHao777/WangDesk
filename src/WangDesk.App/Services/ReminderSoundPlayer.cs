using System.Media;
using WangDesk.App.Models;

namespace WangDesk.App.Services;

/// <summary>
/// 播放番茄钟提醒音效
/// </summary>
public static class ReminderSoundPlayer
{
    public static void Play(ReminderSoundType soundType)
    {
        try
        {
            GetSystemSound(soundType).Play();
        }
        catch
        {
            // 忽略音效播放失败，不影响主流程
        }
    }

    private static SystemSound GetSystemSound(ReminderSoundType soundType)
    {
        return soundType switch
        {
            ReminderSoundType.Exclamation => SystemSounds.Exclamation,
            ReminderSoundType.Beep => SystemSounds.Beep,
            ReminderSoundType.Hand => SystemSounds.Hand,
            ReminderSoundType.Question => SystemSounds.Question,
            _ => SystemSounds.Asterisk
        };
    }
}
