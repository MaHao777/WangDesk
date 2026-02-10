using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WangDesk.App.Animations;
using WangDesk.App.Models;
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
    private System.Windows.Forms.Timer? _alertFlashTimer;
    private readonly ISystemMonitorService _systemMonitor;
    private readonly ISettingsService _settingsService;
    private readonly IReminderService _reminderService;
    private readonly IAutoStartService _autoStartService;
    private SettingsWindow? _settingsWindow;
    private PomodoroPopupWindow? _pomodoroPopupWindow;
    private ReminderPopupWindow? _reminderPopupWindow;
    private bool _isFlashing;
    private bool _showRedIcon;
    private Icon? _normalIcon;
    private Icon? _redIcon;

    public TrayIconManager(
        ISystemMonitorService systemMonitor,
        ISettingsService settingsService,
        IReminderService reminderService,
        IAutoStartService autoStartService)
    {
        _systemMonitor = systemMonitor;
        _settingsService = settingsService;
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

        _animationTimer = new System.Windows.Forms.Timer();
        _animationTimer.Interval = 100;
        _animationTimer.Tick += (s, e) => UpdateAnimation();
        _animationTimer.Start();

        _tooltipUpdateTimer = new System.Windows.Forms.Timer();
        _tooltipUpdateTimer.Interval = 1000;
        _tooltipUpdateTimer.Tick += (s, e) => UpdateTooltip();
        _tooltipUpdateTimer.Start();

        _alertFlashTimer = new System.Windows.Forms.Timer();
        _alertFlashTimer.Interval = 500;
        _alertFlashTimer.Tick += (s, e) => FlashAlertIcon();

        _systemMonitor.MetricsUpdated += OnMetricsUpdated;
        _systemMonitor.StartMonitoring();

        _notifyIcon.MouseClick += OnTrayIconClick;
        _notifyIcon.MouseMove += OnTrayIconMouseMove;
        _notifyIcon.BalloonTipClicked += OnBalloonTipClicked;

        _settingsService.SettingsChanged += OnSettingsChanged;
        _reminderService.ReminderTriggered += OnReminderTriggered;
    }

    /// <summary>
    /// 更新动画
    /// </summary>
    private void OnReminderTriggered(object? sender, EventArgs e)
    {
        try
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ShowReminderPopup();
            });
        }
        catch
        {
        }
    }

    private void ShowReminderPopup()
    {
        _reminderPopupWindow?.Close();
        _reminderPopupWindow = new ReminderPopupWindow();
        var screenPoint = System.Windows.Forms.Cursor.Position;
        _reminderPopupWindow.ShowNearScreenPoint(screenPoint);
    }

    private void StartFlashing()
    {
        if (_isFlashing) return;
        
        _isFlashing = true;
        _showRedIcon = true;
        
        if (_normalIcon == null)
        {
            _normalIcon = _notifyIcon?.Icon;
            _redIcon = CreateRedIcon();
        }
        
        _alertFlashTimer?.Start();
    }

    public void StopFlashing()
    {
        _isFlashing = false;
        _alertFlashTimer?.Stop();
        if (_normalIcon != null && _notifyIcon != null)
        {
            _notifyIcon.Icon = _normalIcon;
        }
    }

    private void FlashAlertIcon()
    {
        if (!_isFlashing || _notifyIcon == null) return;
        
        _showRedIcon = !_showRedIcon;
        _notifyIcon.Icon = _showRedIcon ? _redIcon : _normalIcon;
    }

    private Icon CreateRedIcon()
    {
        var size = 256;
        var bitmap = new System.Drawing.Bitmap(size, size);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);
        
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(239, 89, 80));
        g.FillEllipse(brush, 20, 20, size - 40, size - 40);
        
        using var whiteBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
        var fontSize = size / 3;
        using var font = new System.Drawing.Font("Segoe UI", fontSize, System.Drawing.FontStyle.Bold);
        var text = "!";
        var textSize = g.MeasureString(text, font);
        var x = (size - textSize.Width) / 2;
        var y = (size - textSize.Height) / 2;
        g.DrawString(text, font, whiteBrush, x, y);
        
        var hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    private void OnBalloonTipClicked(object? sender, EventArgs e)
    {
        StopFlashing();
    }

    private void UpdateAnimation()
    {
        if (_isFlashing) return;
        
        var frame = _animationGenerator.GenerateNextFrame();
        if (frame != null)
        {
            var newIcon = ConvertDrawingImageToIcon(frame);
            var oldIcon = _notifyIcon!.Icon;
            _notifyIcon.Icon = newIcon;
            
            if (_normalIcon == null)
            {
                _normalIcon = newIcon;
            }
            
            // 释放旧图标的 GDI 资源（但不释放 _normalIcon）
            if (oldIcon != null && oldIcon != _normalIcon)
            {
                oldIcon.Dispose();
            }
        }
    }

    /// <summary>
    /// 更新托盘提示
    /// </summary>
    private void UpdateTooltip()
    {
        var metrics = _systemMonitor.GetMetrics();
        var storage = _systemMonitor.GetStorageInfo();
        var remaining = _reminderService.GetRemainingTime();

        var tooltip = $"旺旺桌宠\n" +
                      $"CPU: {metrics.CpuUsage}%\n" +
                      $"内存: {metrics.MemoryUsagePercent}% ({metrics.MemoryUsedGB:F1}/{metrics.MemoryTotalGB:F1} GB)\n" +
                      $"上传: {metrics.NetworkSent}\n" +
                      $"下载: {metrics.NetworkReceived}\n" +
                      $"剩余: {(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";

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
        StopFlashing();
        
        if (e.Button == MouseButtons.Left)
        {
            ShowPomodoroPopup(e.Location);
        }
        else if (e.Button == MouseButtons.Right)
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
    /// 显示番茄钟弹窗（切换显示/隐藏）
    /// </summary>
    private void ShowPomodoroPopup(System.Drawing.Point point)
    {
        // 如果番茄钟弹窗已经显示，则关闭它
        if (_pomodoroPopupWindow != null && _pomodoroPopupWindow.IsOpen)
        {
            _pomodoroPopupWindow.Close();
            return;
        }

        _pomodoroPopupWindow?.Dispose();
        _pomodoroPopupWindow = new PomodoroPopupWindow(_settingsService, _reminderService);
        var screenPoint = System.Windows.Forms.Cursor.Position;
        _pomodoroPopupWindow.ShowNearScreenPoint(screenPoint);
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
                _autoStartService);
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        _reminderService.SetInterval(settings.ReminderIntervalMinutes);
        _autoStartService.SetAutoStart(settings.AutoStartEnabled);
    }

    private void ShutdownApplication()
    {
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// 将DrawingImage转换为Icon
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private Icon ConvertDrawingImageToIcon(DrawingImage drawingImage)
    {
        // 使用适中分辨率渲染，减少 GDI+ 负担
        int size = 64;
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
        IntPtr hIcon = bmp.GetHicon();
        try
        {
            // 用 Icon 构造函数克隆一份，然后立即释放原生 GDI 句柄
            return (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    public void Dispose()
    {
        _animationTimer?.Stop();
        _animationTimer?.Dispose();
        _tooltipUpdateTimer?.Stop();
        _tooltipUpdateTimer?.Dispose();
        _alertFlashTimer?.Stop();
        _alertFlashTimer?.Dispose();
        _redIcon?.Dispose();
        _normalIcon?.Dispose();
        _notifyIcon?.Dispose();
        _settingsWindow?.Close();
        _pomodoroPopupWindow?.Close();
        _reminderPopupWindow?.Close();
    }
}
