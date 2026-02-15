using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using WangDesk.App.Models;

namespace WangDesk.App.Services;

/// <summary>
/// 播放番茄钟提醒音效（内置与自定义）
/// </summary>
public static class ReminderSoundPlayer
{
    public const string BuiltinAsteriskId = "builtin:asterisk";
    public const string BuiltinExclamationId = "builtin:exclamation";
    public const string BuiltinBeepId = "builtin:beep";
    public const string BuiltinHandId = "builtin:hand";
    public const string BuiltinQuestionId = "builtin:question";

    private static readonly Dictionary<string, SystemSound> BuiltinSoundMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [BuiltinAsteriskId] = SystemSounds.Asterisk,
        [BuiltinExclamationId] = SystemSounds.Exclamation,
        [BuiltinBeepId] = SystemSounds.Beep,
        [BuiltinHandId] = SystemSounds.Hand,
        [BuiltinQuestionId] = SystemSounds.Question
    };

    private static readonly object _sync = new();
    private static SoundPlayer? _activeCustomPlayer;
    private static string? _activeCustomPath;

    public static string GetSoundsDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WangDesk",
            "sounds");
    }

    public static bool IsBuiltinSelectionId(string? selectionId)
    {
        return !string.IsNullOrWhiteSpace(selectionId) && BuiltinSoundMap.ContainsKey(selectionId);
    }

    public static string MapLegacySoundToSelectionId(ReminderSoundType soundType)
    {
        return soundType switch
        {
            ReminderSoundType.Exclamation => BuiltinExclamationId,
            ReminderSoundType.Beep => BuiltinBeepId,
            ReminderSoundType.Hand => BuiltinHandId,
            ReminderSoundType.Question => BuiltinQuestionId,
            _ => BuiltinAsteriskId
        };
    }

    public static ReminderSoundType MapSelectionIdToLegacySound(string? selectionId)
    {
        if (string.Equals(selectionId, BuiltinExclamationId, StringComparison.OrdinalIgnoreCase))
        {
            return ReminderSoundType.Exclamation;
        }

        if (string.Equals(selectionId, BuiltinBeepId, StringComparison.OrdinalIgnoreCase))
        {
            return ReminderSoundType.Beep;
        }

        if (string.Equals(selectionId, BuiltinHandId, StringComparison.OrdinalIgnoreCase))
        {
            return ReminderSoundType.Hand;
        }

        if (string.Equals(selectionId, BuiltinQuestionId, StringComparison.OrdinalIgnoreCase))
        {
            return ReminderSoundType.Question;
        }

        return ReminderSoundType.Asterisk;
    }

    public static void Play(AppSettings settings)
    {
        if (settings == null)
        {
            PlayBuiltin(BuiltinAsteriskId);
            return;
        }

        var selectionId = string.IsNullOrWhiteSpace(settings.ReminderSoundSelectionId)
            ? AppSettings.DefaultReminderSoundSelectionId
            : settings.ReminderSoundSelectionId;

        if (selectionId.StartsWith("custom:", StringComparison.OrdinalIgnoreCase))
        {
            if (TryPlayCustomSound(settings, selectionId))
            {
                return;
            }

            Debug.WriteLine($"[ReminderSound] Failed to play custom sound: {selectionId}. Fallback to builtin.");
            PlayBuiltin(BuiltinAsteriskId);
            return;
        }

        PlayBuiltin(selectionId);
    }

    public static void Play(ReminderSoundType soundType)
    {
        PlayBuiltin(MapLegacySoundToSelectionId(soundType));
    }

    public static void Stop()
    {
        lock (_sync)
        {
            StopActiveCustomPlayer_NoLock();
        }
    }

    private static bool TryPlayCustomSound(AppSettings settings, string selectionId)
    {
        var customItem = settings.CustomReminderSounds
            .FirstOrDefault(item => string.Equals(item.Id, selectionId, StringComparison.OrdinalIgnoreCase));

        if (customItem == null || string.IsNullOrWhiteSpace(customItem.FileName))
        {
            return false;
        }

        var customPath = Path.Combine(GetSoundsDirectory(), customItem.FileName);
        if (!File.Exists(customPath))
        {
            return false;
        }

        try
        {
            var player = new SoundPlayer(customPath);
            lock (_sync)
            {
                StopActiveCustomPlayer_NoLock();
                _activeCustomPlayer = player;
                _activeCustomPath = customPath;
                _activeCustomPlayer.Play();
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReminderSound] Custom sound playback error: {ex}");
            lock (_sync)
            {
                StopActiveCustomPlayer_NoLock();
            }
            return false;
        }
    }

    private static void PlayBuiltin(string? selectionId)
    {
        // 内置系统音效不可中断；这里先停止正在播放的自定义 WAV，防止叠音。
        Stop();

        var resolvedId = IsBuiltinSelectionId(selectionId)
            ? selectionId!
            : BuiltinAsteriskId;

        try
        {
            BuiltinSoundMap[resolvedId].Play();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReminderSound] Builtin sound playback error: {ex}");
            SystemSounds.Asterisk.Play();
        }
    }

    private static void StopActiveCustomPlayer_NoLock()
    {
        if (_activeCustomPlayer == null)
        {
            _activeCustomPath = null;
            return;
        }

        try
        {
            _activeCustomPlayer.Stop();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReminderSound] Stop custom sound failed: {_activeCustomPath}. {ex}");
        }
        finally
        {
            _activeCustomPlayer.Dispose();
            _activeCustomPlayer = null;
            _activeCustomPath = null;
        }
    }
}
