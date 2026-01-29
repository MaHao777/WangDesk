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
                    SettingsChanged?.Invoke(this, _settings);
                }
            }
        }
        catch (Exception)
        {
            // 使用默认设置
            _settings = new AppSettings();
        }
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
