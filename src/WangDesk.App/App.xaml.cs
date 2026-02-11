using System.Windows;
using WangDesk.App.Services;

namespace WangDesk.App;

/// <summary>
/// 旺旺桌宠应用程序
/// </summary>
public partial class App : global::System.Windows.Application
{
    private TrayIconManager? _trayIconManager;
    private ISystemMonitorService? _systemMonitorService;
    private ISettingsService? _settingsService;
    private IReminderService? _reminderService;
    private IAutoStartService? _autoStartService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 初始化服务
        InitializeServices();

        // 初始化托盘图标管理器
        _trayIconManager = new TrayIconManager(
            _systemMonitorService!,
            _settingsService!,
            _reminderService!,
            _autoStartService!);

        _trayIconManager.Initialize();
    }

    /// <summary>
    /// 初始化所有服务
    /// </summary>
    private void InitializeServices()
    {
        // 设置服务
        _settingsService = new SettingsService();

        // 系统监控服务
        _systemMonitorService = new SystemMonitorService();

        // 定时提醒服务（使用设置中的默认间隔）
        _reminderService = new ReminderService(
            _settingsService.CurrentSettings.ReminderIntervalMinutes,
            _settingsService.CurrentSettings.BreakIntervalMinutes);

        // 开机自启服务
        _autoStartService = new AutoStartService();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 释放资源
        _trayIconManager?.Dispose();
        (_systemMonitorService as IDisposable)?.Dispose();
        (_reminderService as IDisposable)?.Dispose();

        base.OnExit(e);
    }
}
