using System.IO;
using System.Text.Json;
using WangDesk.App.Models;

namespace WangDesk.App.Services;

/// <summary>
/// 设置服务实现
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings;

    public AppSettings CurrentSettings => _settings;

    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WangDesk");
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        _settingsPath = Path.Combine(appDataPath, "settings.json");
        _settings = new AppSettings();
        
        LoadSettings();
    }

    public void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);
                if (loadedSettings != null)
                {
                    _settings = loadedSettings;
                    var hasMigration = NormalizeSettings(_settings);
                    if (hasMigration)
                    {
                        SaveSettings();
                    }
                    else
                    {
                        SettingsChanged?.Invoke(this, _settings);
                    }
                }
            }
        }
        catch (Exception)
        {
            // 使用默认设置
            _settings = new AppSettings();
        }
    }

    private static bool NormalizeSettings(AppSettings settings)
    {
        var hasChanges = false;

        if (settings.CustomReminderSounds == null)
        {
            settings.CustomReminderSounds = [];
            hasChanges = true;
        }

        if (string.IsNullOrWhiteSpace(settings.ReminderSoundSelectionId))
        {
            settings.ReminderSoundSelectionId = ReminderSoundPlayer.MapLegacySoundToSelectionId(settings.ReminderSound);
            hasChanges = true;
        }
        else if (!ReminderSoundPlayer.IsBuiltinSelectionId(settings.ReminderSoundSelectionId) &&
                 !settings.ReminderSoundSelectionId.StartsWith("custom:", StringComparison.OrdinalIgnoreCase))
        {
            settings.ReminderSoundSelectionId = AppSettings.DefaultReminderSoundSelectionId;
            hasChanges = true;
        }

        if (settings.CustomReminderSounds.Count > 0)
        {
            var originalCount = settings.CustomReminderSounds.Count;
            settings.CustomReminderSounds = settings.CustomReminderSounds
                .Where(item =>
                    item != null &&
                    !string.IsNullOrWhiteSpace(item.Id) &&
                    !string.IsNullOrWhiteSpace(item.DisplayName) &&
                    !string.IsNullOrWhiteSpace(item.FileName))
                .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
            if (settings.CustomReminderSounds.Count != originalCount)
            {
                hasChanges = true;
            }
        }

        if (settings.ReminderSoundSelectionId.StartsWith("custom:", StringComparison.OrdinalIgnoreCase))
        {
            var exists = settings.CustomReminderSounds
                .Any(item => string.Equals(item.Id, settings.ReminderSoundSelectionId, StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                settings.ReminderSoundSelectionId = AppSettings.DefaultReminderSoundSelectionId;
                hasChanges = true;
            }
        }

        return hasChanges;
    }

    public void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
            SettingsChanged?.Invoke(this, _settings);
        }
        catch (Exception)
        {
            // 记录日志
        }
    }
}
