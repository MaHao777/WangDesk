using Microsoft.Win32;
using System.Reflection;

namespace WangDesk.App.Services;

/// <summary>
/// 开机自启服务实现
/// </summary>
public class AutoStartService : IAutoStartService
{
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private readonly string _appName;
    private readonly string _appPath;

    public AutoStartService()
    {
        _appName = "WangDesk";
        _appPath = Assembly.GetExecutingAssembly().Location;
        
        // 如果是dll，获取exe路径
        if (_appPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            _appPath = _appPath.Replace(".dll", ".exe");
        }
    }

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
            if (key != null)
            {
                var value = key.GetValue(_appName);
                return value != null;
            }
        }
        catch (Exception)
        {
            // 记录日志
        }
        return false;
    }

    public void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                key.SetValue(_appName, $"\"{_appPath}\"");
            }
            else
            {
                key.DeleteValue(_appName, false);
            }
        }
        catch (Exception)
        {
            // 记录日志
        }
    }
}
