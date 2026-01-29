using System.IO;
using System.Text.Json;
using WangDesk.App.Models;

namespace WangDesk.App.Services;

/// <summary>
/// 设置服务接口
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 当前设置
    /// </summary>
    AppSettings CurrentSettings { get; }
    
    /// <summary>
    /// 加载设置
    /// </summary>
    void LoadSettings();
    
    /// <summary>
    /// 保存设置
    /// </summary>
    void SaveSettings();
    
    /// <summary>
    /// 设置变更事件
    /// </summary>
    event EventHandler<AppSettings>? SettingsChanged;
}
