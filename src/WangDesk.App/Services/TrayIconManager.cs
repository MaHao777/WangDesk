using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WangDesk.App.Animations;
using WangDesk.App.Models;
using WangDesk.App.Services;
using WangDesk.App.Views;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace WangDesk.App.Services;

/// <summary>
/// 托盘图标管理服务
/// </summary>
public class TrayIconManager : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private readonly PetAnimationGenerator _animationGenerator;
    private System.Windows.Forms.Timer? _animationTimer;
    private System.Windows.Forms.Timer? _tooltipUpdateTimer;
    private readonly ISystemMonitorService _systemMonitor;
    private readonly ISettingsService _settingsService;
    private readonly ITranslationService _translationService;
    private readonly IReminderService _reminderService;
    private readonly IAutoStartService _autoStartService;
    private SettingsWindow? _settingsWindow;
    private TranslationWindow? _translationWindow;

    public TrayIconManager(
        ISystemMonitorService systemMonitor,
        ISettingsService settingsService,
        ITranslationService translationService,
        IReminderService reminderService,
        IAutoStartService autoStartService)
    {
        _systemMonitor = systemMonitor;
        _settingsService = settingsService;
        _translationService = translationService;
        _reminderService = reminderService;
        _autoStartService = autoStartService;
        _animationGenerator = new PetAnimationGenerator();
    }

    /// <summary>
    /// 初始化托盘图标
    /// </summary>
    public void Initialize()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "旺旺桌宠 - CPU: 0%",
            Visible = true
        };

        // 创建动画定时器
        _animationTimer = new System.Windows.Forms.Timer();
        _animationTimer.Interval = 100; // 100ms 更新一帧
        _animationTimer.Tick += (s, e) => UpdateAnimation();
        _animationTimer.Start();

        // 创建提示更新定时器
        _tooltipUpdateTimer = new System.Windows.Forms.Timer();
        _tooltipUpdateTimer.Interval = 1000; // 1秒更新一次提示
        _tooltipUpdateTimer.Tick += (s, e) => UpdateTooltip();
        _tooltipUpdateTimer.Start();

        // 绑定系统监控事件
        _systemMonitor.MetricsUpdated += OnMetricsUpdated;
        _systemMonitor.StartMonitoring();

        // 设置鼠标事件
        _notifyIcon.MouseClick += OnTrayIconClick;
        _notifyIcon.MouseMove += OnTrayIconMouseMove;

        // 启动提醒服务
        _reminderService.ReminderTriggered += OnReminderTriggered;
        _reminderService.SetInterval(_settingsService.CurrentSettings.ReminderIntervalMinutes);
        _reminderService.Start();

        // 配置翻译服务
        _translationService.Configure(
            _settingsService.CurrentSettings.BaiduTranslateAppId,
            _settingsService.CurrentSettings.BaiduTranslateSecretKey);

        // 监听设置变更
        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    /// <summary>
    /// 更新动画
    /// </summary>
    private void UpdateAnimation()
    {
        var frame = _animationGenerator.GenerateNextFrame();
        if (frame != null)
        {
            _notifyIcon!.Icon = ConvertDrawingImageToIcon(frame);
        }
    }

    /// <summary>
    /// 更新托盘提示
    /// </summary>
    private void UpdateTooltip()
    {
        var metrics = _systemMonitor.GetMetrics();
        var storage = _systemMonitor.GetStorageInfo();
        var remainingMinutes = _reminderService.GetRemainingMinutes();
        
        var tooltip = $"旺旺桌宠\n" +
                      $"CPU: {metrics.CpuUsage}%\n" +
                      $"内存: {metrics.MemoryUsagePercent}% ({metrics.MemoryUsedGB:F1}/{metrics.MemoryTotalGB:F1} GB)\n" +
                      $"上传: {metrics.NetworkSent}\n" +
                      $"下载: {metrics.NetworkReceived}\n" +
                      $"下次休息: {remainingMinutes} 分钟后";

        _notifyIcon!.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
    }

    /// <summary>
    /// 系统监控数据更新事件
    /// </summary>
    private void OnMetricsUpdated(object? sender, SystemMetrics metrics)
    {
        _animationGenerator.CpuUsage = metrics.CpuUsage;
    }

    /// <summary>
    /// 托盘图标点击事件
    /// </summary>
    private void OnTrayIconClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ShowContextMenu();
        }
    }

    /// <summary>
    /// 托盘图标鼠标移动事件
    /// </summary>
    private void OnTrayIconMouseMove(object? sender, MouseEventArgs e)
    {
        // 鼠标悬停时显示系统状态提示（由UpdateTooltip处理）
    }

    /// <summary>
    /// 显示上下文菜单
    /// </summary>
    private void ShowContextMenu()
    {
        var contextMenu = new System.Windows.Controls.ContextMenu();

        // 系统状态菜单项
        var statusItem = new System.Windows.Controls.MenuItem { Header = "系统状态" };
        var metrics = _systemMonitor.GetMetrics();
        var storage = _systemMonitor.GetStorageInfo();

        statusItem.Items.Add(new System.Windows.Controls.MenuItem 
        { 
            Header = $"CPU: {metrics.CpuUsage}%", 
            IsEnabled = false 
        });
        statusItem.Items.Add(new System.Windows.Controls.MenuItem 
        { 
            Header = $"User: {metrics.CpuUserUsage}%", 
            IsEnabled = false 
        });
        statusItem.Items.Add(new System.Windows.Controls.MenuItem 
        { 
            Header = $"Kernel: {metrics.CpuKernelUsage}%", 
            IsEnabled = false 
        });
        statusItem.Items.Add(new System.Windows.Controls.MenuItem 
        { 
            Header = $"Available: {metrics.CpuAvailable}%", 
            IsEnabled = false 
        });
        statusItem.Items.Add(new System.Windows.Controls.Separator());
        statusItem.Items.Add(new System.Windows.Controls.MenuItem 
        { 
            Header = $"Memory: {metrics.MemoryUsagePercent}%", 
            IsEnabled = false 
        });
        statusItem.Items.Add(new System.Windows.Controls.MenuItem 
        { 
            Header = $"Total: {metrics.MemoryTotalGB:F2} GB", 
            IsEnabled = false 
        });
        statusItem.Items.Add(new System.Windows.Controls.MenuItem 
        { 
            Header = $"Used: {metrics.MemoryUsedGB:F2} GB", 
            IsEnabled = false 
        });
        statusItem.Items.Add(new System.Windows.Controls.MenuItem 
        { 
            Header = $"Available: {metrics.MemoryAvailableGB:F2} GB", 
            IsEnabled = false 
        });
        
        // 存储信息
        statusItem.Items.Add(new System.Windows.Controls.Separator());
        foreach (var drive in storage)
        {
            var driveItem = new System.Windows.Controls.MenuItem { Header = drive.DriveName };
            driveItem.Items.Add(new System.Windows.Controls.MenuItem 
            { 
                Header = $"{drive.DriveLetter} Drive: {drive.UsagePercent}%", 
                IsEnabled = false 
            });
            driveItem.Items.Add(new System.Windows.Controls.MenuItem 
            { 
                Header = $"Used: {drive.UsedGB:F2} GB", 
                IsEnabled = false 
            });
            driveItem.Items.Add(new System.Windows.Controls.MenuItem 
            { 
                Header = $"Available: {drive.AvailableGB:F2} GB", 
                IsEnabled = false 
            });
            statusItem.Items.Add(driveItem);
        }
        
        // 网络信息
        statusItem.Items.Add(new System.Windows.Controls.Separator());
        statusItem.Items.Add(new System.Windows.Controls.MenuItem 
        { 
            Header = $"Sent: {metrics.NetworkSent}", 
            IsEnabled = false 
        });
        statusItem.Items.Add(new System.Windows.Controls.MenuItem 
        { 
            Header = $"Received: {metrics.NetworkReceived}", 
            IsEnabled = false 
        });

        contextMenu.Items.Add(statusItem);
        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        // 翻译菜单项
        var translateItem = new System.Windows.Controls.MenuItem { Header = "翻译" };
        translateItem.Click += (s, e) => ShowTranslationWindow();
        contextMenu.Items.Add(translateItem);

        // 设置菜单项
        var settingsItem = new System.Windows.Controls.MenuItem { Header = "设置" };
        settingsItem.Click += (s, e) => ShowSettingsWindow();
        contextMenu.Items.Add(settingsItem);

        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        // 退出菜单项
        var exitItem = new System.Windows.Controls.MenuItem { Header = "退出" };
        exitItem.Click += (s, e) => ShutdownApplication();
        contextMenu.Items.Add(exitItem);

        // 显示菜单
        contextMenu.IsOpen = true;
    }

    /// <summary>
    /// 显示翻译窗口
    /// </summary>
    private void ShowTranslationWindow()
    {
        if (_translationWindow == null)
        {
            _translationWindow = new TranslationWindow(_translationService);
            _translationWindow.Closed += (s, e) => _translationWindow = null;
        }
        
        _translationWindow.Show();
        _translationWindow.Activate();
    }

    /// <summary>
    /// 显示设置窗口
    /// </summary>
    private void ShowSettingsWindow()
    {
        if (_settingsWindow == null)
        {
            _settingsWindow = new SettingsWindow(
                _settingsService,
                _autoStartService,
                _reminderService);
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
        }
        
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    /// <summary>
    /// 提醒触发事件
    /// </summary>
    private void OnReminderTriggered(object? sender, EventArgs e)
    {
        // 显示气泡提示
        _notifyIcon?.ShowBalloonTip(
            3000,
            "旺旺桌宠提醒",
            "该休息啦！起来活动一下吧~",
            ToolTipIcon.Info);
    }

    /// <summary>
    /// 设置变更事件
    /// </summary>
    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        // 更新翻译服务配置
        _translationService.Configure(settings.BaiduTranslateAppId, settings.BaiduTranslateSecretKey);
        
        // 更新提醒间隔
        _reminderService.SetInterval(settings.ReminderIntervalMinutes);
        
        // 更新开机自启
        _autoStartService.SetAutoStart(settings.AutoStartEnabled);
    }

    /// <summary>
    /// 关闭应用程序
    /// </summary>
    private void ShutdownApplication()
    {
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// 将DrawingImage转换为Icon
    /// </summary>
    private Icon ConvertDrawingImageToIcon(DrawingImage drawingImage)
    {
        // 使用高分辨率渲染 (256x256) 以确保清晰度
        int size = 256;
        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawImage(drawingImage, new Rect(0, 0, size, size));
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(drawingVisual);

        // 转换为Icon
        using var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
        stream.Position = 0;

        using var bmp = new Bitmap(stream);
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _animationTimer?.Stop();
        _animationTimer?.Dispose();
        _tooltipUpdateTimer?.Stop();
        _tooltipUpdateTimer?.Dispose();
        _notifyIcon?.Dispose();
        _settingsWindow?.Close();
        _translationWindow?.Close();
    }
}
