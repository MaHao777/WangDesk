using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using WangDesk.App.Models;
using WangDesk.App.Services;

using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;

namespace WangDesk.App.Views;

/// <summary>
/// 右键设置弹窗 - 点击外部区域时渐渐向下消失
/// </summary>
public class SettingsPopupWindow : IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IAutoStartService _autoStartService;
    private readonly ISystemMonitorService _systemMonitor;
    private readonly Action _shutdownAction;
    private readonly Popup _popup;
    private DispatcherTimer? _closeTimer;
    private DispatcherTimer? _refreshTimer;
    private Border? _rootBorder;
    private bool _isClosing;
    private bool _mouseHasEntered;
    private System.Windows.Controls.CheckBox? _autoStartCheckBox;

    // 系统状态 UI 元素
    private TextBlock? _cpuValueText;
    private Border? _cpuBar;
    private TextBlock? _memValueText;
    private Border? _memBar;
    private TextBlock? _gpuValueText;
    private Border? _gpuBar;
    private TextBlock? _netUpText;
    private TextBlock? _netDownText;
    private StackPanel? _drivePanel;

    private static readonly SolidColorBrush AccentColor = new(Color.FromRgb(239, 89, 80));
    private static readonly SolidColorBrush AccentHoverColor = new(Color.FromRgb(255, 110, 100));
    private static readonly SolidColorBrush AccentPressedColor = new(Color.FromRgb(200, 70, 60));
    private static readonly SolidColorBrush BgColor = new(Color.FromRgb(40, 40, 45));
    private static readonly SolidColorBrush BgLightColor = new(Color.FromRgb(55, 55, 60));
    private static readonly SolidColorBrush TextColor = new(Color.FromRgb(220, 220, 225));
    private static readonly SolidColorBrush SubTextColor = new(Color.FromRgb(150, 150, 155));

    public bool IsOpen => _popup.IsOpen && !_isClosing;

    public SettingsPopupWindow(
        ISettingsService settingsService,
        IAutoStartService autoStartService,
        ISystemMonitorService systemMonitor,
        Action shutdownAction)
    {
        _settingsService = settingsService;
        _autoStartService = autoStartService;
        _systemMonitor = systemMonitor;
        _shutdownAction = shutdownAction;

        _popup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.AbsolutePoint,
            StaysOpen = true
        };

        InitializeComponent();
        LoadSettings();
        StartCloseTimer();
        StartRefreshTimer();

        _popup.Closed += OnPopupClosed;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _closeTimer?.Stop();
        _refreshTimer?.Stop();
    }

    public void ShowNearScreenPoint(System.Drawing.Point screenPoint)
    {
        _mouseHasEntered = false;
        _isClosing = false;
        _popup.PlacementRectangle = new Rect(
            screenPoint.X - 110,
            screenPoint.Y - 10,
            220, 0);
        _popup.IsOpen = true;
        _closeTimer?.Start();
        _refreshTimer?.Start();
        LoadSettings();
        RefreshSystemStatus();
        PlayOpenAnimation();
    }

    private void PlayOpenAnimation()
    {
        if (_rootBorder == null) return;

        _rootBorder.Opacity = 0;
        _rootBorder.RenderTransform = new TranslateTransform(0, 12);

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var slideUp = new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        _rootBorder.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        ((TranslateTransform)_rootBorder.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slideUp);
    }

    public void Close()
    {
        if (_isClosing || !_popup.IsOpen) return;
        _isClosing = true;
        PlayCloseAnimation();
    }

    /// <summary>
    /// 关闭动画 - 渐渐向下消失
    /// </summary>
    private void PlayCloseAnimation()
    {
        if (_rootBorder == null)
        {
            _popup.IsOpen = false;
            return;
        }

        _rootBorder.RenderTransform ??= new TranslateTransform(0, 0);

        // 透明度渐隐
        var fadeOut = new DoubleAnimation(_rootBorder.Opacity, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        // 向下滑出
        var slideDown = new DoubleAnimation(0, 15, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (s, e) =>
        {
            _popup.IsOpen = false;
            _isClosing = false;
        };

        _rootBorder.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        ((TranslateTransform)_rootBorder.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slideDown);
    }

    private void StartCloseTimer()
    {
        _closeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _closeTimer.Tick += (s, e) => CheckMousePosition();
    }

    /// <summary>
    /// 检测鼠标是否在弹窗外部，如果鼠标曾进入过弹窗后移出则关闭
    /// </summary>
    private void CheckMousePosition()
    {
        if (!_popup.IsOpen || _rootBorder == null) return;

        var mousePos = Mouse.GetPosition(_rootBorder);
        var width = _rootBorder.ActualWidth;
        var height = _rootBorder.ActualHeight;

        var isInside = mousePos.X >= -5 && mousePos.X <= width + 5 &&
                       mousePos.Y >= -5 && mousePos.Y <= height + 5;

        if (isInside)
        {
            _mouseHasEntered = true;
        }
        else if (_mouseHasEntered)
        {
            Close();
        }
    }

    private void InitializeComponent()
    {
        _rootBorder = new Border
        {
            Background = BgColor,
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16, 14, 16, 14),
            BorderBrush = BgLightColor,
            BorderThickness = new Thickness(1),
            Width = 250,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 25,
                ShadowDepth = 4,
                Opacity = 0.55
            }
        };

        var stack = new StackPanel();

        // ===== 系统状态标题 =====
        var statusTitle = new TextBlock
        {
            Text = "📊  系统状态",
            Foreground = TextColor,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(2, 0, 0, 10)
        };
        stack.Children.Add(statusTitle);

        // CPU
        stack.Children.Add(CreateMetricRow("CPU", out _cpuValueText, out _cpuBar, AccentColor));

        // 内存
        stack.Children.Add(CreateMetricRow("内存", out _memValueText, out _memBar,
            new SolidColorBrush(Color.FromRgb(80, 180, 239))));

        // GPU
        stack.Children.Add(CreateMetricRow("GPU", out _gpuValueText, out _gpuBar,
            new SolidColorBrush(Color.FromRgb(160, 120, 240))));

        // 网络
        var netPanel = new StackPanel
        {
            Margin = new Thickness(0, 6, 0, 2)
        };
        var netLabel = new TextBlock
        {
            Text = "🌐  网络",
            Foreground = SubTextColor,
            FontSize = 11,
            Margin = new Thickness(2, 0, 0, 4)
        };
        netPanel.Children.Add(netLabel);

        var netRow = new Grid { Margin = new Thickness(2, 0, 2, 0) };
        netRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        netRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _netUpText = new TextBlock
        {
            Text = "↑ 0 B/s",
            Foreground = new SolidColorBrush(Color.FromRgb(130, 200, 130)),
            FontSize = 11.5,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        Grid.SetColumn(_netUpText, 0);
        netRow.Children.Add(_netUpText);

        _netDownText = new TextBlock
        {
            Text = "↓ 0 B/s",
            Foreground = new SolidColorBrush(Color.FromRgb(130, 170, 230)),
            FontSize = 11.5,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(_netDownText, 1);
        netRow.Children.Add(_netDownText);

        netPanel.Children.Add(netRow);
        stack.Children.Add(netPanel);

        // 磁盘
        _drivePanel = new StackPanel
        {
            Margin = new Thickness(0, 6, 0, 0)
        };
        var driveLabel = new TextBlock
        {
            Text = "💾  磁盘",
            Foreground = SubTextColor,
            FontSize = 11,
            Margin = new Thickness(2, 0, 0, 4)
        };
        _drivePanel.Children.Add(driveLabel);
        stack.Children.Add(_drivePanel);

        // 分隔线
        stack.Children.Add(CreateSeparator(8));

        // ===== 设置标题 =====
        var settingsTitle = new TextBlock
        {
            Text = "⚙  设置",
            Foreground = TextColor,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(2, 0, 0, 8)
        };
        stack.Children.Add(settingsTitle);

        // 开机自启
        var autoStartPanel = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 4),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand
        };

        var autoStartGrid = new Grid();
        autoStartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        autoStartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var autoStartLabel = new TextBlock
        {
            Text = "🚀  开机自启",
            Foreground = TextColor,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(autoStartLabel, 0);
        autoStartGrid.Children.Add(autoStartLabel);

        _autoStartCheckBox = new System.Windows.Controls.CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        _autoStartCheckBox.Checked += OnAutoStartChanged;
        _autoStartCheckBox.Unchecked += OnAutoStartChanged;
        Grid.SetColumn(_autoStartCheckBox, 1);
        autoStartGrid.Children.Add(_autoStartCheckBox);

        autoStartPanel.Child = autoStartGrid;

        // 悬停效果
        autoStartPanel.MouseEnter += (s, e) => autoStartPanel.Background = BgLightColor;
        autoStartPanel.MouseLeave += (s, e) => autoStartPanel.Background = Brushes.Transparent;
        autoStartPanel.MouseLeftButtonUp += (s, e) =>
        {
            _autoStartCheckBox.IsChecked = !_autoStartCheckBox.IsChecked;
        };

        stack.Children.Add(autoStartPanel);

        // 分隔线
        stack.Children.Add(CreateSeparator(2));

        // 退出按钮
        var exitPanel = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 4, 0, 0),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand
        };

        var exitText = new TextBlock
        {
            Text = "🚪  退出",
            Foreground = AccentColor,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };
        exitPanel.Child = exitText;

        exitPanel.MouseEnter += (s, e) =>
        {
            exitPanel.Background = new SolidColorBrush(Color.FromRgb(60, 45, 45));
        };
        exitPanel.MouseLeave += (s, e) => exitPanel.Background = Brushes.Transparent;
        exitPanel.MouseLeftButtonUp += (s, e) =>
        {
            Close();
            _shutdownAction();
        };

        stack.Children.Add(exitPanel);

        _rootBorder.Child = stack;
        _popup.Child = _rootBorder;
    }

    /// <summary>
    /// 创建带进度条的指标行（CPU / 内存）
    /// </summary>
    private static StackPanel CreateMetricRow(string label, out TextBlock valueText, out Border progressBar, SolidColorBrush barColor)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 2) };

        var headerRow = new Grid { Margin = new Thickness(2, 0, 2, 3) };
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var labelText = new TextBlock
        {
            Text = label,
            Foreground = SubTextColor,
            FontSize = 11
        };
        Grid.SetColumn(labelText, 0);
        headerRow.Children.Add(labelText);

        valueText = new TextBlock
        {
            Text = "0%",
            Foreground = TextColor,
            FontSize = 11,
            FontWeight = FontWeights.Medium
        };
        Grid.SetColumn(valueText, 1);
        headerRow.Children.Add(valueText);
        panel.Children.Add(headerRow);

        // 进度条背景
        var barBg = new Border
        {
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 65)),
            Margin = new Thickness(2, 0, 2, 0)
        };

        // 进度条填充
        progressBar = new Border
        {
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = barColor,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 0
        };

        var barGrid = new Grid { Margin = new Thickness(2, 0, 2, 0) };
        barGrid.Children.Add(barBg);
        barGrid.Children.Add(progressBar);
        panel.Children.Add(barGrid);

        return panel;
    }

    /// <summary>
    /// 创建磁盘信息行
    /// </summary>
    private static Grid CreateDriveRow(StorageInfo drive)
    {
        var row = new Grid { Margin = new Thickness(2, 2, 2, 2) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var driveLabel = new TextBlock
        {
            Text = drive.DriveLetter,
            Foreground = SubTextColor,
            FontSize = 11,
            Width = 22
        };
        Grid.SetColumn(driveLabel, 0);
        row.Children.Add(driveLabel);

        // 迷你进度条
        var miniBarBg = new Border
        {
            Height = 3,
            CornerRadius = new CornerRadius(1.5),
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 65)),
            Margin = new Thickness(4, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var miniBar = new Border
        {
            Height = 3,
            CornerRadius = new CornerRadius(1.5),
            Background = drive.UsagePercent > 85
                ? new SolidColorBrush(Color.FromRgb(239, 89, 80))
                : new SolidColorBrush(Color.FromRgb(180, 140, 80)),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var miniBarGrid = new Grid
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 8, 0)
        };
        miniBarGrid.Children.Add(miniBarBg);
        miniBarGrid.Children.Add(miniBar);
        Grid.SetColumn(miniBarGrid, 1);
        row.Children.Add(miniBarGrid);

        // 布局完成后设置进度条宽度
        miniBarGrid.SizeChanged += (s, e) =>
        {
            miniBar.Width = miniBarGrid.ActualWidth * Math.Min(drive.UsagePercent, 100) / 100.0;
        };

        var infoText = new TextBlock
        {
            Text = $"{drive.UsagePercent:F0}%  {drive.AvailableGB:F0}G可用",
            Foreground = SubTextColor,
            FontSize = 10
        };
        Grid.SetColumn(infoText, 2);
        row.Children.Add(infoText);

        return row;
    }

    private static Border CreateSeparator(double verticalMargin = 2)
    {
        return new Border
        {
            Height = 1,
            Background = BgLightColor,
            Margin = new Thickness(0, verticalMargin, 0, verticalMargin)
        };
    }

    /// <summary>
    /// 启动系统状态刷新定时器
    /// </summary>
    private void StartRefreshTimer()
    {
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _refreshTimer.Tick += (s, e) => RefreshSystemStatus();
    }

    /// <summary>
    /// 刷新系统状态显示
    /// </summary>
    private void RefreshSystemStatus()
    {
        if (!_popup.IsOpen) return;

        var metrics = _systemMonitor.GetMetrics();
        var storage = _systemMonitor.GetStorageInfo();

        // CPU
        if (_cpuValueText != null && _cpuBar != null)
        {
            _cpuValueText.Text = $"{metrics.CpuUsage:F0}%";
            // 进度条宽度 = 父容器可用宽度 * 百分比
            var parent = _cpuBar.Parent as Grid;
            if (parent != null && parent.ActualWidth > 0)
            {
                _cpuBar.Width = parent.ActualWidth * Math.Min(metrics.CpuUsage, 100) / 100.0;
            }
        }

        // 内存
        if (_memValueText != null && _memBar != null)
        {
            _memValueText.Text = $"{metrics.MemoryUsagePercent:F0}%  ({metrics.MemoryUsedGB:F1}/{metrics.MemoryTotalGB:F1}GB)";
            var parent = _memBar.Parent as Grid;
            if (parent != null && parent.ActualWidth > 0)
            {
                _memBar.Width = parent.ActualWidth * Math.Min(metrics.MemoryUsagePercent, 100) / 100.0;
            }
        }

        // GPU
        if (_gpuValueText != null && _gpuBar != null)
        {
            _gpuValueText.Text = $"{metrics.GpuUsage:F0}%";
            var parent = _gpuBar.Parent as Grid;
            if (parent != null && parent.ActualWidth > 0)
            {
                _gpuBar.Width = parent.ActualWidth * Math.Min(metrics.GpuUsage, 100) / 100.0;
            }
        }

        // 网络
        if (_netUpText != null) _netUpText.Text = $"↑ {metrics.NetworkSent}";
        if (_netDownText != null) _netDownText.Text = $"↓ {metrics.NetworkReceived}";

        // 磁盘（保留第一个子元素标签，更新后面的）
        if (_drivePanel != null && storage.Count > 0)
        {
            while (_drivePanel.Children.Count > 1)
                _drivePanel.Children.RemoveAt(_drivePanel.Children.Count - 1);

            foreach (var drive in storage)
            {
                _drivePanel.Children.Add(CreateDriveRow(drive));
            }
        }
    }

    private void LoadSettings()
    {
        if (_autoStartCheckBox == null) return;
        var settings = _settingsService.CurrentSettings;
        // 临时取消事件避免触发保存
        _autoStartCheckBox.Checked -= OnAutoStartChanged;
        _autoStartCheckBox.Unchecked -= OnAutoStartChanged;
        _autoStartCheckBox.IsChecked = settings.AutoStartEnabled;
        _autoStartCheckBox.Checked += OnAutoStartChanged;
        _autoStartCheckBox.Unchecked += OnAutoStartChanged;
    }

    private void OnAutoStartChanged(object sender, RoutedEventArgs e)
    {
        var settings = _settingsService.CurrentSettings;
        settings.AutoStartEnabled = _autoStartCheckBox?.IsChecked ?? false;
        _settingsService.SaveSettings();
        _autoStartService.SetAutoStart(settings.AutoStartEnabled);
    }

    public void Dispose()
    {
        _closeTimer?.Stop();
        _refreshTimer?.Stop();
        _popup.IsOpen = false;
    }
}
